using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Utils;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Server.Connections;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server
{
    public class WitServer : IDisposable
    {
        #region Fields

        private readonly ConcurrentDictionary<Guid, ConnectionInfo> m_connections = new ();

        private readonly SemaphoreSlim m_processingLimit;

        private bool m_isDisposed;

        #endregion

        #region Constructors

        public WitServer(ITransportServerFactory transportFactory, IEncryptorServerFactory encryptorFactory,
            IAccessTokenValidator tokenValidator, IMessageSerializer parametersSerializer, IMessageSerializer messageSerializer,
            IRequestProcessor requestProcessor, IDiscoveryServer? discoveryServer,
            ILogger? logger, TimeSpan? timeout, string? name, string? description, int maxConcurrentRequests = int.MaxValue,
            TimeSpan? handshakeTimeout = null)
        {
            TransportFactory = transportFactory;
            EncryptorFactory = encryptorFactory;
            ParametersSerializer = parametersSerializer;
            MessageSerializer = messageSerializer;
            TokenValidator = tokenValidator;
            RequestProcessor = requestProcessor;
            DiscoveryServer = discoveryServer;
            Logger = logger;
            Timeout = timeout;
            Name = name;
            Description = description;

            RequestProcessor.ResetSerializer(ParametersSerializer);

            HandshakeTimeout = handshakeTimeout;

            Id = Guid.NewGuid();
            m_processingLimit = new SemaphoreSlim(Math.Max(1, maxConcurrentRequests));

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            TransportFactory.NewClientConnected += OnNewClientConnected;
            RequestProcessor.Callback += OnCallback;

            if(DiscoveryServer != null)
                DiscoveryServer.DiscoveryMessageRequested += OnDiscoveryMessageRequested;
        }

        private bool TryGetConnection(Guid client, out ConnectionInfo? connection)
        {
            if (m_connections.TryGetValue(client, out connection))
                return true;

            Logger?.LogWarning("Ignoring message for disconnected or unknown client {ClientId}", client);
            return false;
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection()
        {
            TransportFactory.StartWaitingForConnection(Logger);
            if(DiscoveryServer == null)
                return;

            DiscoveryServer.Start();
            SendDiscoveryMessage(DiscoveryMessageType.Hello);

        }

        public void StopWaitingForConnection()
        {
            TransportFactory.StopWaitingForConnection();
            if (DiscoveryServer == null)
                return;

            SendDiscoveryMessage(DiscoveryMessageType.Goodbye);
            DiscoveryServer.Stop();
        }

        #endregion

        #region Handshake

        private WitMessage ProcessInitialization(ConnectionInfo connection, WitMessage message, out bool refused)
        {
            refused = false;

            if (message.Data == null)
                return message.With(x => x.Data = null);

            WitRequestInitialization? request;
            try
            {
                request = MessageSerializer.Deserialize<WitRequestInitialization>(message.Data);
            }
            catch (Exception)
            {
                request = null;
            }

            if (request == null)
            {
                // An unreadable initialization payload is, in practice, a pre-3.0
                // client whose layout this build no longer parses. Refuse in the
                // open so a newer client would at least see why; an old one fails
                // fast instead of hanging on a silent close.
                Logger?.LogWarning(
                    "Unreadable initialization from client {ClientId}: most likely a pre-protocol-{Version} client; refusing",
                    connection.Id, WitProtocol.VERSION);

                refused = true;
                return message.With(x => x.Data = MessageSerializer.Serialize(
                    RefuseInitialization($"Cannot read the initialization request; the server speaks protocol {WitProtocol.VERSION}")));
            }

            if (request.ProtocolVersion != WitProtocol.VERSION)
            {
                Logger?.LogWarning(
                    "Protocol mismatch for client {ClientId}: client speaks {ClientVersion}, server speaks {ServerVersion}; refusing",
                    connection.Id, request.ProtocolVersion, WitProtocol.VERSION);

                refused = true;

                var refusal = MessageSerializer.Serialize(
                    RefuseInitialization($"Protocol mismatch: client {request.ProtocolVersion}, server {WitProtocol.VERSION}"));

                // Encrypted for the client when it offered a key, so the same
                // decrypt path every accepted handshake uses can read the reason.
                if (request.PublicKey != null)
                    refusal = connection.Encryptor.EncryptForClient(refusal, request.PublicKey).GetAwaiter().GetResult();

                return message.With(x => x.Data = refusal);
            }

            if (request.PublicKey == null)
                return message.With(x => x.Data = null);

            try
            {
                var response = new WitResponseInitialization
                {
                    SymmetricKey = connection.Encryptor.GetSymmetricKey(),
                    Vector = connection.Encryptor.GetVector(),
                    ProtocolVersion = WitProtocol.VERSION
                };

                byte[] responseBytes = connection.Encryptor
                    .EncryptForClient(MessageSerializer.Serialize(response), request.PublicKey)
                    .GetAwaiter().GetResult();

                connection.State = ConnectionState.Initialized;

                return message.With(x => x.Data = responseBytes);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error during initialization");
                return message.With(x => x.Data = null);
            }
        }

        private static WitResponseInitialization RefuseInitialization(string reason)
        {
            return new WitResponseInitialization
            {
                ProtocolVersion = WitProtocol.VERSION,
                ErrorMessage = reason
            };
        }

        private WitMessage ProcessAuthorization(ConnectionInfo connection, WitMessage message)
        {
            if (message.Data == null)
                return message.With(x => x.Data = null);

            WitRequestAuthorization? request =
                MessageSerializer.Deserialize<WitRequestAuthorization>(message.Data);

            if (request == null || request.Token == null)
                return message.With(x => x.Data = null);

            try
            {
                bool authorized = TokenValidator.IsAuthorizationTokenValid(request.Token);
                if (authorized)
                    connection.State = ConnectionState.Authorized;

                var response = new WitResponseAuthorization
                {
                    IsAuthorized = authorized,
                    Message = authorized ? "Authorized" : "Forbidden"
                };

                byte[] responseBytes = MessageSerializer.Serialize(response);

                return message.With(x => x.Data = responseBytes);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error during authorization");
                return message.With(x => x.Data = null);
            }
        }

        #endregion

        #region Processing

        private async Task<WitMessage> ProcessMessage(ConnectionInfo connection, WitMessage message)
        {
            var request = message.Data.GetRequest(MessageSerializer);

            if (request != null && request.InvocationId != Guid.Empty &&
                connection.TryGetCachedResponse(request.InvocationId, out byte[]? cachedResponse))
            {
                // A retry of an invocation this connection already executed:
                // answer with the recorded result instead of running it again.
                Logger?.LogDebug("Answering duplicate invocation {InvocationId} from cache", request.InvocationId);
                return message.With(x => x.Data = cachedResponse);
            }

            WitResponse? response;
            if (request == null)
            {
                Logger?.LogError($"Request is empty");
                response = WitResponse.BadRequest("Request is empty");
            }

            else if (!TokenValidator.IsRequestTokenValid(request.Token))
            {
                Logger?.LogError($"Token is not valid");
                response = WitResponse.UnauthorizedRequest("Token is not valid");
            }
            else
            {
                await m_processingLimit.WaitAsync().ConfigureAwait(false);
                try
                {
                    response = await RequestProcessor.Process(request).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // A service method that throws is a fault of that one call, not
                    // of the connection. Turn it into an error response so the caller
                    // gets an answer and the connection stays open for the next
                    // request, instead of the exception unwinding to the connection
                    // loop and closing it.
                    Logger?.LogError(e, "Request processor threw for method {MethodName}", request.MethodName);
                    response = WitResponse.InternalServerError($"Request processing failed: {e.Message}", e);
                }
                finally
                {
                    m_processingLimit.Release();
                }
            }

            byte[] responseBytes = MessageSerializer.Serialize(response!);

            if (request != null && request.InvocationId != Guid.Empty)
                connection.CacheResponse(request.InvocationId, responseBytes);

            return message.With(x => x.Data = responseBytes);
        }

        private async Task<WitMessage> Encrypt(ConnectionInfo connection, WitMessage message)
        {
            if (message.Type == WitMessageType.Initialization || message.Data == null)
                return message;

            var data = await connection.Encryptor.Encrypt(message.Data);

            return message.With(x => x.Data = data);
        }

        private async Task<WitMessage> Decrypt(ConnectionInfo connection, WitMessage message)
        {
            if (message.Type == WitMessageType.Initialization || message.Data == null)
                return message;

            var data = await connection.Encryptor.Decrypt(message.Data);

            return message.With(x => x.Data = data);
        }

        #endregion

        #region Send

        private async Task SendMessageAsync(ConnectionInfo connection, WitMessage message)
        {
            await connection.SendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var encryptedMessage = await Encrypt(connection, message);
                var data = MessageSerializer.Serialize(encryptedMessage);
                await connection.Transport.SendBytesAsync(data);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to send message to client {ClientId}", connection.Id);
            }
            finally
            {
                connection.SendLock.Release();
            }
        }

        private void SendDiscoveryMessage(DiscoveryMessageType type)
        {
            DiscoveryServer?.SendDiscoveryMessage(ParametersSerializer.Serialize(GetMessage(type), Logger));
        }

        private DiscoveryMessage GetMessage(DiscoveryMessageType type)
        {
            return new DiscoveryMessage
            {
                ServiceId = Id,
                Timestamp = DateTimeOffset.UtcNow,
                Type = type,
                ServiceName = Name,
                ServiceDescription = Description,
                Transport = TransportFactory.Options.Transport,
                Data = TransportFactory.Options.Data
            };
        }

        private void CloseConnection(ConnectionInfo connection)
        {
            // Disposing the transport raises Disconnected, which removes and
            // disposes the connection. Stop taking inbound frames first.
            connection.CompleteInbound();
            connection.Transport.Dispose();
        }

        #endregion

        #region Connection Loop

        private async Task ProcessConnectionAsync(ConnectionInfo connection)
        {
            try
            {
                while (await connection.Inbound.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (connection.Inbound.Reader.TryRead(out byte[]? data))
                    {
                        if (!await ProcessFrameAsync(connection, data).ConfigureAwait(false))
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Connection loop failed for client {ClientId}", connection.Id);
                CloseConnection(connection);
            }
        }

        /// <summary>
        /// Handles one inbound frame in the connection's order. Returns
        /// <c>false</c> when the connection must stop being processed (it was
        /// closed for an out-of-order or rejected message).
        /// </summary>
        private async Task<bool> ProcessFrameAsync(ConnectionInfo connection, byte[] data)
        {
            WitMessage? message;
            try
            {
                message = MessageSerializer.Deserialize<WitMessage>(data);
            }
            catch (Exception e)
            {
                Logger?.LogWarning(e, "Failed to deserialize a frame from client {ClientId}", connection.Id);
                return true;
            }

            if (message == null || message.Type == WitMessageType.Unknown)
                return true;

            var decrypted = await Decrypt(connection, message);

            switch (message.Type)
            {
                case WitMessageType.Initialization:
                    if (connection.IsInitialized && connection.CanReinitialize)
                        connection.Reinitialize();

                    if (connection.IsInitialized)
                    {
                        Logger?.LogWarning("Out-of-order initialization from client {ClientId}; closing", connection.Id);
                        CloseConnection(connection);
                        return false;
                    }

                    var initReply = ProcessInitialization(connection, decrypted, out bool refusedInit);
                    await SendMessageAsync(connection, initReply);

                    if (refusedInit)
                    {
                        CloseConnection(connection);
                        return false;
                    }

                    return true;

                case WitMessageType.Authorization:
                    if (connection.State != ConnectionState.Initialized)
                    {
                        Logger?.LogWarning("Out-of-order authorization from client {ClientId}; closing", connection.Id);
                        CloseConnection(connection);
                        return false;
                    }

                    await SendMessageAsync(connection, ProcessAuthorization(connection, decrypted));

                    if (!connection.IsAuthorized)
                    {
                        Logger?.LogWarning("Authorization failed for client {ClientId}; closing", connection.Id);
                        CloseConnection(connection);
                        return false;
                    }

                    return true;

                case WitMessageType.Request:
                    if (!connection.IsAuthorized)
                    {
                        Logger?.LogWarning("Request before authorization from client {ClientId}; closing", connection.Id);
                        CloseConnection(connection);
                        return false;
                    }

                    var tag = connection.Id.ToString().Substring(0, 4);
                    var responseMessage = await ProcessMessage(connection, decrypted);
                    await SendMessageAsync(connection, responseMessage);
                    return true;

                default:
                    // Clients do not send callbacks; ignore anything else.
                    return true;
            }
        }

        #endregion

        #region Callbacks

        private void OnCallback(WitRequest? request)
        {
            if (request == null || m_isDisposed)
                return;

            byte[] callback;
            try
            {
                callback = MessageSerializer.Serialize(request);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to serialize callback");
                return;
            }

            foreach (var connection in m_connections.Values)
            {
                // Only clients that finished the handshake receive events, and the
                // send goes through the connection's send lock so it never
                // interleaves with a response on the same transport.
                if (!connection.IsAuthorized)
                    continue;

                _ = SendCallbackAsync(connection, callback);
            }
        }

        private async Task SendCallbackAsync(ConnectionInfo connection, byte[] callback)
        {
            var message = new WitMessage
            {
                Id = connection.Id,
                Type = WitMessageType.Callback,
                Data = callback
            };

            var send = SendMessageAsync(connection, message);

            if (Timeout != null && Timeout != TimeSpan.Zero)
            {
                try
                {
                    await send.WaitAsync(Timeout.Value).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    Logger?.LogWarning("Callback to client {ClientId} timed out", connection.Id);
                }
            }
            else
            {
                await send.ConfigureAwait(false);
            }
        }

        #endregion

        #region Event Handlers

        private void OnDataReceived(Guid sender, byte[] data)
        {
            if (!TryGetConnection(sender, out ConnectionInfo? connection) || connection == null)
                return;

            connection.Inbound.Writer.TryWrite(data);
        }

        private void OnDiscoveryMessageRequested(IDiscoveryServer sender)
        {
            SendDiscoveryMessage(DiscoveryMessageType.Heartbeat);
        }

        private void OnNewClientConnected(ITransportServer transport)
        {
            // Keep this fast and subscribe promptly: the transport is already
            // reading, and any work done before the Callback subscription is a
            // window in which a fast client's first frame is delivered to nobody.
            // The encryptor is built lazily on the processing loop for that reason.
            var connection = new ConnectionInfo(transport, EncryptorFactory);

            if (!m_connections.TryAdd(transport.Id, connection))
            {
                connection.Dispose();
                return;
            }

            transport.Callback += OnDataReceived;
            transport.Disconnected += OnClientDisconnected;

            _ = ProcessConnectionAsync(connection);
            _ = EnforceHandshakeTimeoutAsync(connection);
        }

        private async Task EnforceHandshakeTimeoutAsync(ConnectionInfo connection)
        {
            if (HandshakeTimeout == null || HandshakeTimeout == TimeSpan.Zero)
                return;

            try
            {
                await Task.Delay(HandshakeTimeout.Value).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            if (m_isDisposed || connection.IsAuthorized)
                return;

            // Still connected but never authorized within the window: close it so
            // it cannot hold a slot open indefinitely.
            if (m_connections.TryGetValue(connection.Id, out var current) && ReferenceEquals(current, connection))
            {
                Logger?.LogWarning("Client {ClientId} did not finish the handshake in time; closing", connection.Id);
                CloseConnection(connection);
            }
        }

        private void OnClientDisconnected(Guid sender)
        {
            if (m_connections.TryRemove(sender, out ConnectionInfo? info) && info != null)
                info.Dispose();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_isDisposed)
                return;

            m_isDisposed = true;

            TransportFactory.NewClientConnected -= OnNewClientConnected;
            RequestProcessor.Callback -= OnCallback;

            if (DiscoveryServer != null)
                DiscoveryServer.DiscoveryMessageRequested -= OnDiscoveryMessageRequested;

            StopWaitingForConnection();

            foreach (var info in m_connections.Values)
            {
                info.Transport.Dispose();
                info.Dispose();
            }

            m_connections.Clear();

            TransportFactory.Dispose();

            if (DiscoveryServer != null)
            {
                DiscoveryServer.Dispose();
            }

            m_processingLimit.Dispose();
        }

        #endregion

        #region Services

        private IRequestProcessor RequestProcessor { get; }

        private ITransportServerFactory TransportFactory { get; }

        private IEncryptorServerFactory EncryptorFactory { get; }

        private IMessageSerializer ParametersSerializer { get; }

        private IMessageSerializer MessageSerializer { get; }

        private IAccessTokenValidator TokenValidator { get; }

        private IDiscoveryServer? DiscoveryServer { get; }

        private ILogger? Logger { get; }

        private TimeSpan? Timeout { get; }

        private TimeSpan? HandshakeTimeout { get; }

        public string? Name { get; }

        public string? Description { get; }

        public Guid Id { get; }

        public IServerOptions Options => TransportFactory.Options;

        #endregion
    }
}

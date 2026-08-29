using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Utils;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Resilience;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client
{
    public class WitClient : IClient, IDisposable
    {
        #region Events

        public event ClientEventHandler CallbackReceived = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        /// <summary>
        /// Raised when a reconnection attempt is starting.
        /// </summary>
        public event ReconnectingEventHandler Reconnecting = delegate { };

        /// <summary>
        /// Raised when reconnection succeeds.
        /// </summary>
        public event ReconnectedEventHandler Reconnected = delegate { };

        /// <summary>
        /// Raised when all reconnection attempts have failed.
        /// </summary>
        public event ReconnectionFailedEventHandler ReconnectionFailed = delegate { };

        #endregion

        #region Fields

        private readonly Channel<byte[]> m_inbound = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions { SingleReader = true });

        #endregion

        #region Constructors

        public WitClient(ITransportClient transport, IEncryptorClient encryptor, IAccessTokenProvider tokenProvider,
            IMessageSerializer parametersSerializer, IMessageSerializer messageSerializer, ILogger? logger, TimeSpan? timeout)
            : this(transport, encryptor, tokenProvider, parametersSerializer, messageSerializer, 
                new ReconnectionOptions(), new RetryOptions(), logger, timeout)
        {
        }

        public WitClient(ITransportClient transport, IEncryptorClient encryptor, IAccessTokenProvider tokenProvider,
            IMessageSerializer parametersSerializer, IMessageSerializer messageSerializer, 
            ReconnectionOptions reconnectionOptions, ILogger? logger, TimeSpan? timeout)
            : this(transport, encryptor, tokenProvider, parametersSerializer, messageSerializer, 
                reconnectionOptions, new RetryOptions(), logger, timeout)
        {
        }

        public WitClient(ITransportClient transport, IEncryptorClient encryptor, IAccessTokenProvider tokenProvider,
            IMessageSerializer parametersSerializer, IMessageSerializer messageSerializer, 
            ReconnectionOptions reconnectionOptions, RetryOptions retryOptions, ILogger? logger, TimeSpan? timeout)
        {
            Transport = transport;
            ParametersSerializer = parametersSerializer;
            MessageSerializer = messageSerializer;
            Encryptor = encryptor;
            TokenProvider = tokenProvider;
            ReconnectionOptions = reconnectionOptions;
            RetryOptions = retryOptions;
            Logger = logger;

            Timeout = timeout;

            SendLock = new SemaphoreSlim(1, 1);
            ReconnectionCts = new CancellationTokenSource();
            RetryPolicy = new RetryPolicy(retryOptions, logger);

            IsInitialized = false;
            IsAuthorized = false;
            ConnectionState = ReconnectionState.Disconnected;
            
            InitDefaults();
            InitEvents();

            _ = Task.Run(ProcessInboundAsync);
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            
        }

        private void InitEvents()
        {
            Transport.Callback += OnDataReceived;
            Transport.Disconnected += OnServerDisconnected;
        }

        private async Task<bool> ProcessInitialization(TimeSpan? timeout = null)
        {
            if (IsInitialized)
                return true;

            Logger?.LogDebug("Starting initialization");

            WitMessage requestMessage = new()
            {
                Id = Guid.NewGuid(),
                Type = WitMessageType.Initialization,
                Data = MessageSerializer.Serialize(new WitRequestInitialization
                {
                    PublicKey = Encryptor.GetPublicKey(),
                    ProtocolVersion = WitProtocol.VERSION
                })
            };

            WitMessage? responseMessage;
            try
            {
                responseMessage = await SendMessageAsync(requestMessage, timeout);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Initialization request failed");
                return false;
            }

            if (responseMessage == null || responseMessage.Data == null)
                return false;

            byte[] dataDecrypted = await Encryptor.DecryptRsa(responseMessage.Data);

            Logger?.LogDebug(
                "Initialization response: {EncryptedLength} bytes on the wire, {PlainLength} bytes decrypted",
                responseMessage.Data.Length, dataDecrypted.Length);

            WitResponseInitialization? response =
                MessageSerializer.Deserialize<WitResponseInitialization>(dataDecrypted, Logger);

            if (response?.ErrorMessage != null)
            {
                Logger?.LogError("Server refused initialization: {Reason}", response.ErrorMessage);
                return false;
            }

            if (response == null || response.SymmetricKey == null || response.Vector == null)
            {
                Logger?.LogError(
                    "Failed to initialize: response parsed: {HasResponse}, key present: {HasKey}, vector present: {HasVector}",
                    response != null, response?.SymmetricKey != null, response?.Vector != null);
                return false;
            }

            IsInitialized = Encryptor.ResetAes(response.SymmetricKey, response.Vector);

            if (IsInitialized)
                Logger?.LogDebug("Initialization completed");
            else
                Logger?.LogError("Failed to initialize");

            return IsInitialized;
        }

        private async Task<bool> ProcessAuthorization(TimeSpan? timeout = null)
        {
            if (IsAuthorized)
                return true;

            Logger?.LogDebug("Starting authorization");

            WitMessage requestMessage = new()
            {
                Id = Guid.NewGuid(),
                Type = WitMessageType.Authorization,
                Data = MessageSerializer.Serialize(new WitRequestAuthorization
                {
                    Token = await TokenProvider.GetToken()
                })
            };

            WitMessage? responseMessage;
            try
            {
                responseMessage = await SendMessageAsync(requestMessage, timeout);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Authorization request failed");
                return false;
            }

            if (responseMessage == null || responseMessage.Data == null)
            {
                Logger?.LogError("Failed to authorize");
                return false;
            }

            WitResponseAuthorization? response =
                MessageSerializer.Deserialize<WitResponseAuthorization>(responseMessage.Data);

            if (response == null)
            {
                Logger?.LogError("Failed to authorize");
                return false;
            }

            IsAuthorized = response.IsAuthorized;

            if (IsAuthorized)
                Logger?.LogDebug($"Authorization completed");
            else
                Logger?.LogError($"Failed to authorize, {response.Message}");

            return IsAuthorized;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"IsInitialized: {IsInitialized}, IsAuthorized: {IsAuthorized}, State: {ConnectionState}";
        }

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConnectionTimeout = timeout;

            if (!await Transport.ConnectAsync(timeout, cancellationToken))
                return false;

            if (!await ProcessInitialization(timeout))
                return false;

            if (!await ProcessAuthorization(timeout))
                return false;

            ConnectionState = ReconnectionState.Connected;

            return true;
        }

        public async Task<bool> ReconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            await Disconnect();

            await Task.Delay(500, cancellationToken);

            return await ConnectAsync(timeout, cancellationToken);
        }

        public async Task Disconnect()
        {
            // Cancel any ongoing reconnection
            await StopReconnectionAsync();

            await Transport.Disconnect();

            IsInitialized = false;
            IsAuthorized = false;
            ConnectionState = ReconnectionState.Disconnected;
        }

        /// <summary>
        /// Stops any ongoing reconnection attempts.
        /// </summary>
        public async Task StopReconnectionAsync()
        {
            if (ConnectionState == ReconnectionState.Reconnecting)
            {
                ReconnectionCts.Cancel();
                ReconnectionCts = new CancellationTokenSource();
            }
        }

        public async Task<WitResponse> SendRequest(WitRequest? request)
        {
            if (request == null)
            {
                Logger?.LogError("Failed to send request: empty request");
                return WitResponse.BadRequest($"Empty request");
            }

            // One id for the logical call: every retry attempt below carries it,
            // so the server can recognise a duplicate and answer from its cache.
            if (request.InvocationId == Guid.Empty)
                request.InvocationId = Guid.NewGuid();

            async Task<WitResponse> SendOnceAsync()
            {
                request.Token = await TokenProvider.GetToken();

                var messageRequest = new WitMessage
                {
                    Id = Guid.NewGuid(),
                    Type = WitMessageType.Request,
                    Data = MessageSerializer.Serialize(request)
                };

                WitMessage? messageResponse = null;

                try
                {
                    messageResponse = await SendMessageAsync(messageRequest);
                }
                catch (TimeoutException)
                {
                    Logger?.LogError("Request {MessageId} timed out", messageRequest.Id);
                    return WitResponse.Timeout("No response arrived within the configured timeout");
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Failed to receive response");
                    return WitResponse.TransportError("Failed to receive response", e);
                }

                try
                {
                    return (messageResponse?.Data).GetResponse(MessageSerializer);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Failed to parse response");
                    return WitResponse.TransportError("Failed to parse response", e);
                }
            }

            // Retry re-executes the method, so it is reserved for methods the
            // consumer has declared idempotent (or an explicit retry-everything
            // opt-in). Anything else gets exactly one attempt.
            if (RetryOptions.Enabled &&
                (RetryOptions.RetryAllMethods || RetryOptions.IdempotentMethods.Contains(request.MethodName)))
            {
                return await RetryPolicy.ExecuteAsync(SendOnceAsync);
            }

            return await SendOnceAsync();
        }

        private async Task<WitMessage?> SendMessageAsync(WitMessage message, TimeSpan? timeout = null)
        {
            timeout ??= Timeout;

            // Register the wait BEFORE sending, keyed by this message's id, so a
            // response that comes back before the send call even returns — a real
            // possibility on the in-process and MMF transports — is matched, not
            // dropped. Several requests may be in flight at once; each is matched
            // to its own response by id.
            var pending = new TaskCompletionSource<WitMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!PendingRequests.TryAdd(message.Id, pending))
            {
                Logger?.LogError("Duplicate message id {MessageId}", message.Id);
                return null;
            }

            try
            {
                // Sealing and sending stay under one lock: the AEAD counter
                // demands that messages hit the wire in the order they were
                // sealed, so encrypt-then-send must be atomic per message. The
                // response wait below is outside the lock -- requests still
                // overlap on the wire's turnaround.
                await SendLock.WaitAsync();
                try
                {
                    var encryptedMessage = await Encrypt(message);
                    byte[] data = MessageSerializer.Serialize(encryptedMessage);
                    await Transport.SendBytesAsync(data);
                }
                finally
                {
                    SendLock.Release();
                }


                if (timeout != null && timeout != TimeSpan.Zero)
                    return await pending.Task.WaitAsync(timeout.Value);

                return await pending.Task;
            }
            finally
            {
                PendingRequests.TryRemove(message.Id, out _);
            }
        }

        private async Task<WitMessage> Encrypt(WitMessage message)
        {
            if (message.Type == WitMessageType.Initialization || message.Data == null)
                return message;

            var data = await Encryptor.Encrypt(message.Data);

            return message.With(x => x.Data = data);
        }

        private async Task<WitMessage> Decrypt(WitMessage message)
        {
            if (message.Type == WitMessageType.Initialization || message.Data == null)
                return message;

            var data = await Encryptor.Decrypt(message.Data);

            return message.With(x => x.Data = data);
        }

        #endregion

        #region Reconnection

        private async Task StartReconnectionAsync()
        {
            if (!ReconnectionOptions.Enabled || !ReconnectionOptions.ReconnectOnDisconnect)
            {
                ConnectionState = ReconnectionState.Disconnected;
                return;
            }

            if (ConnectionState == ReconnectionState.Reconnecting)
                return;

            ConnectionState = ReconnectionState.Reconnecting;
            Logger?.LogInformation("Starting automatic reconnection");

            Exception? lastException = null;
            var attempt = 0;
            var token = ReconnectionCts.Token;

            while (!token.IsCancellationRequested)
            {
                attempt++;

                if (ReconnectionOptions.MaxAttempts > 0 && attempt > ReconnectionOptions.MaxAttempts)
                {
                    Logger?.LogError($"Reconnection failed after {ReconnectionOptions.MaxAttempts} attempts");
                    ConnectionState = ReconnectionState.Failed;
                    ReconnectionOptions.OnReconnectionFailed?.Invoke(lastException);
                    ReconnectionFailed(this, lastException);
                    return;
                }

                var delay = ReconnectionOptions.GetDelayForAttempt(attempt);
                Logger?.LogDebug($"Reconnection attempt {attempt}, waiting {delay}");

                ReconnectionOptions.OnReconnecting?.Invoke(attempt, delay);
                Reconnecting(this, attempt, delay);

                try
                {
                    await Task.Delay(delay, token);

                    // Reset state before reconnecting
                    IsInitialized = false;
                    IsAuthorized = false;

                    var timeout = ConnectionTimeout ?? TimeSpan.FromSeconds(30);
                    
                    if (await Transport.ConnectAsync(timeout, token) &&
                        await ProcessInitialization(timeout) &&
                        await ProcessAuthorization(timeout))
                    {
                        ConnectionState = ReconnectionState.Connected;
                        Logger?.LogInformation($"Reconnection successful after {attempt} attempts");
                        ReconnectionOptions.OnReconnected?.Invoke();
                        Reconnected(this);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger?.LogDebug("Reconnection cancelled");
                    ConnectionState = ReconnectionState.Disconnected;
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Logger?.LogWarning(ex, $"Reconnection attempt {attempt} failed");
                }
            }

            ConnectionState = ReconnectionState.Disconnected;
        }

        #endregion

        #region Event Handlers

        private async Task OnMessageReceived(WitMessage? message)
        {
            if (message == null)
            {
                Logger?.LogWarning("Ignoring empty incoming message");
                return;
            }

            if (message.Type == WitMessageType.Unknown)
                return;

            if (message.Type == WitMessageType.Initialization && IsInitialized)
            {
                Logger?.LogError("Wrong initialization request");
                throw new WitException($"Wrong initialization request");
            }

            if (message.Type == WitMessageType.Authorization && IsAuthorized)
            {
                Logger?.LogError("Wrong authorization request");
                throw new WitException($"Wrong authorization request");
            }

            var decryptedMessage = await Decrypt(message);

            if (message.Type == WitMessageType.Callback)
            {
                var callbackRequest = decryptedMessage.Data.GetRequest(MessageSerializer);

                // Decryption stayed on the loop (ordered); the user's handlers
                // run off it, so an event handler that calls back into this
                // client cannot deadlock against its own inbound processing.
                _ = Task.Run(() => CallbackReceived(callbackRequest));
            }

            else if (PendingRequests.TryGetValue(decryptedMessage.Id, out var pending))
                pending.TrySetResult(decryptedMessage);

            else
                Logger?.LogWarning("Dropping a response with no waiting request: {MessageId}", decryptedMessage.Id);
        }

        private void OnDataReceived(Guid sender, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Logger?.LogWarning("Ignoring empty incoming payload from transport {TransportId}", sender);
                return;
            }

            // Inbound frames are processed by one loop, in arrival order: the
            // AEAD counter demands it, and it is what keeps a response from
            // being decrypted before the callback sent just ahead of it.
            m_inbound.Writer.TryWrite(data);
        }

        private async Task ProcessInboundAsync()
        {
            while (await m_inbound.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (m_inbound.Reader.TryRead(out byte[]? data))
                    await ProcessInboundFrameAsync(data);
            }
        }

        private async Task ProcessInboundFrameAsync(byte[] data)
        {
            try
            {
                var message = MessageSerializer.Deserialize<WitMessage>(data);
                if (message == null)
                {
                    Logger?.LogWarning("Ignoring incoming payload that could not be deserialized into a message");
                    return;
                }

                await OnMessageReceived(message);
            }
            catch (Exception e)
            {
                Logger?.LogWarning(e, "Failed to process incoming payload");
            }
        }

        private async void OnServerDisconnected(Guid sender)
        {
            var wasConnected = ConnectionState == ReconnectionState.Connected;

            // The responses to anything still in flight died with the connection;
            // fail those calls now rather than letting them wait forever.
            foreach (var pending in PendingRequests.Values)
                pending.TrySetException(new WitExceptionTransport("Connection was lost before a response arrived"));
            PendingRequests.Clear();
            
            IsInitialized = false;
            IsAuthorized = false;

            Disconnected(sender);

            // Start reconnection if was connected and auto-reconnect is enabled
            if (wasConnected && ReconnectionOptions.Enabled && ReconnectionOptions.ReconnectOnDisconnect)
            {
                await StartReconnectionAsync();
            }
            else
            {
                ConnectionState = ReconnectionState.Disconnected;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            ReconnectionCts?.Cancel();
            ReconnectionCts?.Dispose();

            m_inbound.Writer.TryComplete();

            foreach (var pending in PendingRequests.Values)
                pending.TrySetException(new WitExceptionTransport("The client was disposed before a response arrived"));
            PendingRequests.Clear();

            SendLock?.Dispose();
            Encryptor?.Dispose();
            Transport?.Dispose();
        }

        #endregion


        #region Properties

        private TimeSpan? Timeout { get; }

        private TimeSpan? ConnectionTimeout { get; set; }

        private ConcurrentDictionary<Guid, TaskCompletionSource<WitMessage?>> PendingRequests { get; } = new();

        private SemaphoreSlim SendLock { get; }

        private CancellationTokenSource ReconnectionCts { get; set; }

        public bool IsInitialized { get; private set; }

        public bool IsAuthorized { get; private set; }

        /// <summary>
        /// Gets the current connection/reconnection state.
        /// </summary>
        public ReconnectionState ConnectionState { get; private set; }

        #endregion

        #region Services

        private ITransportClient Transport { get; }

        public IMessageSerializer ParametersSerializer { get; }
        
        public IMessageSerializer MessageSerializer { get; }

        private IEncryptorClient Encryptor { get; }

        private IAccessTokenProvider TokenProvider { get; }

        private ReconnectionOptions ReconnectionOptions { get; }

        private RetryOptions RetryOptions { get; }

        private RetryPolicy RetryPolicy { get; }

        private ILogger? Logger { get; }

        #endregion
    }

    #region Delegates

    public delegate void ReconnectingEventHandler(WitClient sender, int attempt, TimeSpan delay);
    public delegate void ReconnectedEventHandler(WitClient sender);
    public delegate void ReconnectionFailedEventHandler(WitClient sender, Exception? lastException);

    #endregion
}

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.Extensions.Logging;
using OutWit.Common.Utils;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
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

        #endregion

        #region Constructors

        public WitServer(ITransportServerFactory transportFactory, IEncryptorServerFactory encryptorFactory,
            IAccessTokenValidator tokenValidator, IMessageSerializer parametersSerializer, IMessageSerializer messageSerializer,
            IRequestProcessor requestProcessor, IDiscoveryServer? discoveryServer,
            ILogger? logger, TimeSpan? timeout, string? name, string? description)
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
            
            Id = Guid.NewGuid();
            WaitForCallback = new SemaphoreSlim(1, 1);

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

        private WitMessage ProcessInitialization(Guid client, WitMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitException($"Unexpected recipient id: {client}");
            }

            if(connection.IsInitialized && connection.CanReinitialize)
                connection.Reinitialize();

            if (connection.IsInitialized)
            {
                Logger?.LogError($"Wrong initialization request");
                throw new WitException($"Wrong initialization request");
            }

            if (message.Data == null)
                return message.With(x=>x.Data = null);

            WitRequestInitialization? request = 
                MessageSerializer.Deserialize<WitRequestInitialization>(message.Data);

            if(request == null || request.PublicKey == null)
                return message.With(x => x.Data = null);

            try
            {
                var response = new WitResponseInitialization
                {
                    SymmetricKey = connection.Encryptor.GetSymmetricKey(),
                    Vector = connection.Encryptor.GetVector()
                };

                byte[] responseBytes = MessageSerializer
                    .Serialize(response)
                    .EncryptRsa(request.PublicKey.ToRsaParameters());

                connection.IsInitialized = true;

                return message.With(x => x.Data = responseBytes);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error during initialization");
                return message.With(x => x.Data = null);
            }
        }

        private WitMessage ProcessAuthorization(Guid client, WitMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitException($"Unexpected recipient id: {client}");
            }

            if (connection.IsAuthorized)
            {
                Logger?.LogError($"Wrong authorization request");
                throw new WitException($"Wrong authorization request");
            }

            if (message.Data == null)
                return message.With(x => x.Data = null);

            WitRequestAuthorization? request =
                MessageSerializer.Deserialize<WitRequestAuthorization>(message.Data);

            if (request == null || request.Token == null)
                return message.With(x => x.Data = null);

            try
            {
                connection.IsAuthorized = TokenValidator.IsAuthorizationTokenValid(request.Token);

                var response = new WitResponseAuthorization
                {
                    IsAuthorized = connection.IsAuthorized,
                    Message = connection.IsAuthorized ? "Authorized" : "Forbidden"
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

        protected async Task<WitMessage> ProcessMessage(Guid client, WitMessage message)
        {
            var request = message.Data.GetRequest(MessageSerializer);

            WitResponse? response = null;
            if (request == null)
            {
                Logger?.LogError($"Request is empty");
                response = WitResponse.BadRequest("Request is empty");
            }

            else if (!TokenValidator.IsRequestTokenValid(request.Token))
            {
                Logger?.LogError($"Tokes is not valid");
                response = WitResponse.UnauthorizedRequest("Tokes is not valid");
            }
            else 
                response = await RequestProcessor.Process(request);

            return message.With(x => x.Data = MessageSerializer.Serialize(response!));
        }

        private async Task<WitMessage> Encrypt(Guid client, WitMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitException($"Unexpected recipient id: {client}");
            }

            if (message.Type == WitMessageType.Initialization || message.Data == null)
                return message;

            var data = await connection.Encryptor.Encrypt(message.Data);

            return message.With(x => x.Data = data);
        }

        private async Task<WitMessage> Decrypt(Guid client, WitMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitException($"Unexpected recipient id: {client}");
            }

            if (message.Type == WitMessageType.Initialization || message.Data == null)
                return message;

            var data = await connection.Encryptor.Decrypt(message.Data);

            return message.With(x => x.Data = data);
        }

        #endregion

        #region Tools

        private async Task SendMessageAsync(Guid client, WitMessage message)
        {
            if(!m_connections.TryGetValue(client, out ConnectionInfo? connection))
                return;

            var encryptedMessage = await Encrypt(client, message);
            var data = MessageSerializer.Serialize(encryptedMessage);
            await connection.Transport.SendBytesAsync(data);
        }

        private async Task SendCallbackAsync(byte[] callback)
        {
            foreach (var connection in m_connections.Values)
            {
                try
                {
                    var message = new WitMessage
                    {
                        Id = connection.Id,
                        Type = WitMessageType.Callback,
                        Data = callback
                    };

                    var encryptedMessage = await Encrypt(connection.Id, message);
                    var data = MessageSerializer.Serialize(encryptedMessage);
                    await connection.Transport.SendBytesAsync(data);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Failed to send callback");
                }
        
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

        #endregion

        #region Event Handlers

        private async Task OnMessageReceived(Guid client, WitMessage? message)
        {
            if(message == null || message.Type == WitMessageType.Unknown)
                return;

            await WaitForCallback.WaitAsync();

            var decryptedMessage = await Decrypt(client, message);

            if (message.Type == WitMessageType.Initialization)
                await SendMessageAsync(client, ProcessInitialization(client, decryptedMessage));

            if (message.Type == WitMessageType.Authorization)
                await SendMessageAsync(client, ProcessAuthorization(client, decryptedMessage));

            else if (message.Type == WitMessageType.Request)
            {
                var responseMessage = await ProcessMessage(client, decryptedMessage);
                await SendMessageAsync(client, responseMessage);
            }

            WaitForCallback.Release();
        }

        private async void OnDataReceived(Guid sender, byte[] data)
        {
            await OnMessageReceived(sender, MessageSerializer.Deserialize<WitMessage>(data));
        }

        private void OnCallback(WitRequest? request)
        {
            if (request == null)
                return;

            Task.Run(async () =>
            {
                await WaitForCallback.WaitAsync();

                if(Timeout != null && Timeout != TimeSpan.Zero)
                    SendCallbackAsync(MessageSerializer.Serialize(request)).Wait(Timeout.Value);
                else
                    SendCallbackAsync(MessageSerializer.Serialize(request)).Wait();

                WaitForCallback.Release();
            });
        }

        private void OnDiscoveryMessageRequested(IDiscoveryServer sender)
        {
            SendDiscoveryMessage(DiscoveryMessageType.Heartbeat);
        }

        private void OnNewClientConnected(ITransportServer transport)
        {
            transport.Callback += OnDataReceived;
            transport.Disconnected += OnClientDisconnected;

            m_connections.TryAdd(transport.Id, new ConnectionInfo(transport, EncryptorFactory.CreateEncryptor()));
        }

        private void OnClientDisconnected(Guid sender)
        {
            if (m_connections.ContainsKey(sender))
                m_connections.TryRemove(sender, out ConnectionInfo? info);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            foreach (var info in m_connections.Values)
            {
                info.Transport.Dispose();
            }
            
            m_connections.Clear();
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

        private SemaphoreSlim WaitForCallback { get; }

        private ILogger? Logger { get; }

        private TimeSpan? Timeout { get; }

        public string? Name { get; }

        public string? Description { get; }

        public Guid Id { get; }

        public IServerOptions Options => TransportFactory.Options;

        #endregion
    }
}

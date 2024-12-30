using System;
using System.Collections.Concurrent;
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
    public class WitComServer
    {
        #region Fields

        private readonly ConcurrentDictionary<Guid, ConnectionInfo> m_connections = new ();

        #endregion

        #region Constructors

        public WitComServer(ITransportServerFactory transportFactory, IEncryptorServerFactory encryptorFactory,
            IAccessTokenValidator tokenValidator, IMessageSerializer serializer, IValueConverter valueConverter, IRequestProcessor requestProcessor,
            ILogger? logger, TimeSpan? timeout)
        {
            TransportFactory = transportFactory;
            EncryptorFactory = encryptorFactory;
            Serializer = serializer;
            Converter = valueConverter;
            TokenValidator = tokenValidator;
            RequestProcessor = requestProcessor;
            Logger = logger;
            Timeout = timeout;

            WaitForCallback = new AutoResetEvent(true);

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            TransportFactory.NewClientConnected += OnNewClientConnected;
            RequestProcessor.Callback += OnCallback;
        }


        private WitComMessage ProcessInitialization(Guid client, WitComMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitComException($"Unexpected recipient id: {client}");
            }

            if(connection.IsInitialized && connection.CanReinitialize)
                connection.Reinitialize();

            if (connection.IsInitialized)
            {
                Logger?.LogError($"Wrong initialization request");
                throw new WitComException($"Wrong initialization request");
            }

            if (message.Data == null)
                return message.With(x=>x.Data = null);

            WitComRequestInitialization? request = 
                Serializer.Deserialize<WitComRequestInitialization>(message.Data);

            if(request == null || request.PublicKey == null)
                return message.With(x => x.Data = null);

            try
            {
                var response = new WitComResponseInitialization
                {
                    SymmetricKey = connection.Encryptor.GetSymmetricKey(),
                    Vector = connection.Encryptor.GetVector()
                };

                byte[] responseBytes = Serializer
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

        private WitComMessage ProcessAuthorization(Guid client, WitComMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitComException($"Unexpected recipient id: {client}");
            }

            if (connection.IsAuthorized)
            {
                Logger?.LogError($"Wrong authorization request");
                throw new WitComException($"Wrong authorization request");
            }

            if (message.Data == null)
                return message.With(x => x.Data = null);

            WitComRequestAuthorization? request =
                Serializer.Deserialize<WitComRequestAuthorization>(message.Data);

            if (request == null || request.Token == null)
                return message.With(x => x.Data = null);

            try
            {
                connection.IsAuthorized = TokenValidator.IsAuthorizationTokenValid(request.Token);

                var response = new WitComResponseAuthorization
                {
                    IsAuthorized = connection.IsAuthorized,
                    Message = connection.IsAuthorized ? "Authorized" : "Forbidden"
                };

                byte[] responseBytes = Serializer.Serialize(response);

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
            TransportFactory.StartWaitingForConnection();
        }

        public void StopWaitingForConnection()
        {
            TransportFactory.StopWaitingForConnection();
        }

        protected WitComMessage ProcessMessage(Guid client, WitComMessage message)
        {
            var request = message.Data.GetRequest(Serializer, Converter);

            WitComResponse? response = null;
            if (request == null)
            {
                Logger?.LogError($"Request is empty");
                response = WitComResponse.BadRequest("Request is empty");
            }

            else if (!TokenValidator.IsRequestTokenValid(request.Token))
            {
                Logger?.LogError($"Tokes is not valid");
                response = WitComResponse.UnauthorizedRequest("Tokes is not valid");
            }
            else 
                response = RequestProcessor.Process(request);

            return message.With(x => x.Data = Serializer.Serialize(response!));
        }

        private WitComMessage Encrypt(Guid client, WitComMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitComException($"Unexpected recipient id: {client}");
            }

            if (message.Type == WitComMessageType.Initialization || message.Data == null)
                return message;

            return message.With(x => x.Data = connection.Encryptor.Encrypt(message.Data));
        }

        private WitComMessage Decrypt(Guid client, WitComMessage message)
        {
            if (!m_connections.TryGetValue(client, out ConnectionInfo? connection))
            {
                Logger?.LogError($"Unexpected recipient id");
                throw new WitComException($"Unexpected recipient id: {client}");
            }

            if (message.Type == WitComMessageType.Initialization || message.Data == null)
                return message;

            return message.With(x => x.Data = connection.Encryptor.Decrypt(message.Data));
        }

        #endregion

        #region Tools

        private async Task SendMessageAsync(Guid client, WitComMessage message)
        {
            if(!m_connections.TryGetValue(client, out ConnectionInfo? connection))
                return;

            var data = Serializer.Serialize(Encrypt(client, message));
            await connection.Transport.SendBytesAsync(data);
        }

        private async Task SendCallbackAsync(byte[] callback)
        {
            foreach (var connection in m_connections.Values)
            {
                var message = new WitComMessage
                {
                    Id = connection.Id,
                    Type = WitComMessageType.Callback,
                    Data = callback
                };

                var data = Serializer.Serialize(Encrypt(connection.Id, message));
                await  connection.Transport.SendBytesAsync(data);
            }
        }

        #endregion

        #region Event Handlers

        private async Task OnMessageReceived(Guid client, WitComMessage? message)
        {
            if(message == null || message.Type == WitComMessageType.Unknown)
                return;

            WaitForCallback.Reset();

            if (message.Type == WitComMessageType.Initialization)
                await SendMessageAsync(client, ProcessInitialization(client, Decrypt(client, message)));

            if (message.Type == WitComMessageType.Authorization)
                await SendMessageAsync(client, ProcessAuthorization(client, Decrypt(client, message)));

            else if(message.Type == WitComMessageType.Request)
                await SendMessageAsync(client, ProcessMessage(client,Decrypt(client, message)));

            WaitForCallback.Set();
        }

        private async void OnDataReceived(Guid sender, byte[] data)
        {
            await OnMessageReceived(sender, Serializer.Deserialize<WitComMessage>(data));
        }

        private void OnCallback(WitComRequest? request)
        {
            if (request == null)
                return;

            Task.Run(() =>
            {
                WaitForCallback.WaitOne();

                if(Timeout != null && Timeout != TimeSpan.Zero)
                    SendCallbackAsync(Serializer.Serialize(request)).Wait(Timeout.Value);
                else
                    SendCallbackAsync(Serializer.Serialize(request)).Wait();

                WaitForCallback.Set();
            });
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

        #region Services

        private IRequestProcessor RequestProcessor { get; }

        private ITransportServerFactory TransportFactory { get; }

        private IEncryptorServerFactory EncryptorFactory { get; }

        private IMessageSerializer Serializer { get; }

        private IValueConverter Converter { get; }

        private IAccessTokenValidator TokenValidator { get; }

        private AutoResetEvent WaitForCallback{ get; }

        private ILogger? Logger { get; }

        private TimeSpan? Timeout { get; }

        #endregion
    }
}

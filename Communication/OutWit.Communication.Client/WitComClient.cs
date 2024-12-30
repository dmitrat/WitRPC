using Microsoft.Extensions.Logging;
using OutWit.Common.Utils;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client
{
    public class WitComClient : IClient
    {
        #region Events

        public event ClientEventHandler CallbackReceived = delegate { };

        #endregion

        #region Constructors

        public WitComClient(ITransportClient transport, IEncryptorClient encryptor, IAccessTokenProvider tokenProvider, 
            IMessageSerializer serializer, IValueConverter valueConverter, ILogger? logger, TimeSpan? timeout)
        {
            Transport = transport;
            Serializer = serializer;
            Encryptor = encryptor;
            TokenProvider = tokenProvider;
            Converter = valueConverter;
            Logger = logger;

            Timeout = timeout;

            WaitForResponse = new AutoResetEvent(true);
            WaitForRequest = new AutoResetEvent(true);

            IsInitialized = false;
            IsAuthorized = false;

            InitEvents();
        }

        #endregion

        #region Initialization

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

            WitComMessage requestMessage = new ()
            {
                Id = Guid.NewGuid(),
                Type = WitComMessageType.Initialization,
                Data = Serializer.Serialize(new WitComRequestInitialization
                {
                    PublicKey = Encryptor.GetPublicKey()
                })
            };

            WitComMessage? responseMessage = await SendMessageAsync(requestMessage, timeout);
            if (responseMessage == null || responseMessage.Data == null)
                return false;

            byte[] dataDecrypted = responseMessage.Data.DecryptRsa(Encryptor.GetPrivateKey().ToRsaParameters());
            WitComResponseInitialization? response =
                Serializer.Deserialize<WitComResponseInitialization>(dataDecrypted);

            if (response == null || response.SymmetricKey == null || response.Vector == null)
            {
                Logger?.LogError("Failed to initialize");
                return false;
            }

            IsInitialized = Encryptor.ResetAes(response.SymmetricKey, response.Vector);

            if(IsInitialized)
                Logger?.LogDebug("Initialization completed");
            else
                Logger?.LogError("Failed to initialize");

            return IsInitialized;
        }

        private async Task<bool> ProcessAuthorization()
        {
            if (IsAuthorized)
                return true;

            Logger?.LogDebug("Starting authorization");

            WitComMessage requestMessage = new()
            {
                Id = Guid.NewGuid(),
                Type = WitComMessageType.Authorization,
                Data = Serializer.Serialize(new WitComRequestAuthorization
                {
                    Token = TokenProvider.GetToken()
                })
            };

            WitComMessage? responseMessage = await SendMessageAsync(requestMessage);
            if (responseMessage == null || responseMessage.Data == null)
            {
                Logger?.LogError("Failed to authorize");
                return false;
            }

            WitComResponseAuthorization? response =
                Serializer.Deserialize<WitComResponseAuthorization>(responseMessage.Data);

            if (response == null)
            {
                Logger?.LogError("Failed to authorize");
                return false;
            }

            IsAuthorized = response.IsAuthorized;

            if(IsAuthorized)
                Logger?.LogDebug($"Authorization completed");
            else
                Logger?.LogError($"Failed to authorize, {response.Message}");

            return IsAuthorized;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"IsInitialized: {IsInitialized}, IsAuthorized: {IsAuthorized}";
        }

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!await Transport.ConnectAsync(timeout, cancellationToken))
                return false;

            if (!await ProcessInitialization(timeout))
                return false;

            if (!await ProcessAuthorization())
                return false;
            

            return true;
        }

        public async Task<bool> ReconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            await Disconnect();

            Thread.Sleep(500);

            return await ConnectAsync(timeout, cancellationToken);
        }

        public async Task Disconnect()
        {
            await Transport.Disconnect();

            IsInitialized = false;
            IsAuthorized = false;
        }

        public async Task<WitComResponse> SendRequest(WitComRequest? request)
        {
            if (request == null)
            {
                Logger?.LogError("Failed to send request: empty request");
                return WitComResponse.BadRequest($"Empty request");
            }

            request.Token = TokenProvider.GetToken();

            var messageRequest = new WitComMessage
            {
                Id = Guid.NewGuid(),
                Type = WitComMessageType.Request,
                Data = Serializer.Serialize(request)
            };

            WitComMessage? messageResponse = null;

            try
            {
                messageResponse = await SendMessageAsync(messageRequest);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to receive response");
                return WitComResponse.InternalServerError("Failed to receive response", e);
            }

            try
            {
                return (messageResponse?.Data).GetResponse(Serializer);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to parse response");
                return WitComResponse.InternalServerError("Failed to parse response", e);
            }
        }

        private async Task<WitComMessage?> SendMessageAsync(WitComMessage message, TimeSpan? timeout = null)
        {
            timeout ??= Timeout;

            WaitForRequest.WaitOne();
            WaitForRequest.Reset();

            byte[] data = Serializer.Serialize(Encrypt(message));
            await Transport.SendBytesAsync(data);

            WaitForResponse.Reset();

            var result = (timeout != null && timeout != TimeSpan.Zero)
                ? WaitForResponse.WaitOne(timeout.Value) 
                : WaitForResponse.WaitOne();

            if (!result || Response == null)
            {
                Logger?.LogError("Response timeout");
                WaitForRequest.Set();
                return null;
            }

            if (Response.Id != message.Id)
            {
                Logger?.LogError("Received response inconsistent");
                throw new WitComException($"Received response inconsistent");
            }

            return Response;
        }

        private WitComMessage Encrypt(WitComMessage message)
        {
            if (message.Type == WitComMessageType.Initialization || message.Data == null)
                return message;

            return message.With(x => x.Data = Encryptor.Encrypt(message.Data));
        }

        private WitComMessage Decrypt(WitComMessage message)
        {
            if (message.Type == WitComMessageType.Initialization || message.Data == null)
                return message;

            return message.With(x => x.Data = Encryptor.Decrypt(message.Data));
        }


        #endregion

        #region Event Handlers

        private void OnMessageReceived(WitComMessage? message)
        {
            if(message == null)
                return;

            if(message.Type == WitComMessageType.Unknown)
                return;

            if (message.Type == WitComMessageType.Initialization && IsInitialized)
            {
                Logger?.LogError("Wrong initialization request");
                throw new WitComException($"Wrong initialization request");
            }

            if (message.Type == WitComMessageType.Authorization && IsAuthorized)
            {
                Logger?.LogError("Wrong authorization request");
                throw new WitComException($"Wrong authorization request");
            }

            if (message.Type == WitComMessageType.Callback)
                CallbackReceived(Decrypt(message).Data.GetRequest(Serializer, Converter));

            else
            {
                Response = Decrypt(message);
                WaitForResponse.Set();
                WaitForRequest.Set();
            }
        }

        private void OnDataReceived(Guid sender, byte[] data)
        {
            OnMessageReceived(Serializer.Deserialize<WitComMessage>(data));
        }

        private void OnServerDisconnected(Guid sender)
        {
        }

        #endregion

        #region Properties

        private TimeSpan? Timeout { get; }


        private AutoResetEvent WaitForResponse { get; }

        private AutoResetEvent WaitForRequest { get; }


        private WitComMessage? Response { get; set; }


        public bool IsInitialized { get; private set; }

        public bool IsAuthorized{ get; private set; }

        #endregion

        #region Services

        private ITransportClient Transport { get; }

        public IMessageSerializer Serializer { get; }

        public IValueConverter Converter { get; }

        private IEncryptorClient Encryptor { get; }

        private IAccessTokenProvider TokenProvider { get; }

        private ILogger? Logger { get; }

        #endregion
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class WitClient : IClient, IDisposable
    {
        #region Events

        public event ClientEventHandler CallbackReceived = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public WitClient(ITransportClient transport, IEncryptorClient encryptor, IAccessTokenProvider tokenProvider,
            IMessageSerializer parametersSerializer, IMessageSerializer messageSerializer, ILogger? logger, TimeSpan? timeout)
        {
            Transport = transport;
            ParametersSerializer = parametersSerializer;
            MessageSerializer = messageSerializer;
            Encryptor = encryptor;
            TokenProvider = tokenProvider;
            Logger = logger;

            Timeout = timeout;

            WaitForRequest = new SemaphoreSlim(1, 1);

            IsInitialized = false;
            IsAuthorized = false;
            
            InitDefaults();
            InitEvents();
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
                    PublicKey = Encryptor.GetPublicKey()
                })
            };

            WitMessage? responseMessage = await SendMessageAsync(requestMessage, timeout);
            if (responseMessage == null || responseMessage.Data == null)
                return false;

            byte[] dataDecrypted = await Encryptor.DecryptRsa(responseMessage.Data);
            WitResponseInitialization? response =
                MessageSerializer.Deserialize<WitResponseInitialization>(dataDecrypted);

            if (response == null || response.SymmetricKey == null || response.Vector == null)
            {
                Logger?.LogError("Failed to initialize");
                return false;
            }

            IsInitialized = Encryptor.ResetAes(response.SymmetricKey, response.Vector);

            if (IsInitialized)
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

            WitMessage requestMessage = new()
            {
                Id = Guid.NewGuid(),
                Type = WitMessageType.Authorization,
                Data = MessageSerializer.Serialize(new WitRequestAuthorization
                {
                    Token = await TokenProvider.GetToken()
                })
            };

            WitMessage? responseMessage = await SendMessageAsync(requestMessage);
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

            await Task.Delay(500, cancellationToken);

            return await ConnectAsync(timeout, cancellationToken);
        }

        public async Task Disconnect()
        {
            await Transport.Disconnect();

            IsInitialized = false;
            IsAuthorized = false;
        }

        public async Task<WitResponse> SendRequest(WitRequest? request)
        {
            if (request == null)
            {
                Logger?.LogError("Failed to send request: empty request");
                return WitResponse.BadRequest($"Empty request");
            }

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
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to receive response");
                return WitResponse.InternalServerError("Failed to receive response", e);
            }

            try
            {
                return (messageResponse?.Data).GetResponse(MessageSerializer);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to parse response");
                return WitResponse.InternalServerError("Failed to parse response", e);
            }
        }

        private async Task<WitMessage?> SendMessageAsync(WitMessage message, TimeSpan? timeout = null)
        {
            timeout ??= Timeout;

            if (timeout == null || timeout == TimeSpan.Zero)
                await WaitForRequest.WaitAsync();

            else if (!await WaitForRequest.WaitAsync(timeout.Value))
            {
                Logger?.LogError("Response timeout");
                return null;
            }

            try
            {
                var encryptedMessage = await Encrypt(message);
                byte[] data = MessageSerializer.Serialize(encryptedMessage);

                await Transport.SendBytesAsync(data);

                WaitForResponse = new TaskCompletionSource<WitMessage?>();

                var result = (timeout != null && timeout != TimeSpan.Zero)
                    ? await WaitForResponse.Task.WaitAsync(timeout.Value)
                    : await WaitForResponse.Task.WaitAsync(CancellationToken.None);

                if (result == null)
                {
                    Logger?.LogError("Response timeout");
                    return null;
                }

                if (result.Id != message.Id)
                {
                    Logger?.LogError("Received response inconsistent");
                    throw new WitException($"Received response inconsistent");
                }
                return result;
            }
            catch (Exception e)
            {
                //Logger?.LogError(e, "Failed to send message");
                return null;
            }
            finally
            {
                WaitForRequest.Release();
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

        #region Event Handlers

        private async Task OnMessageReceived(WitMessage? message)
        {
            if (message == null)
                throw new WitException("Received empty message");

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
                CallbackReceived(decryptedMessage.Data.GetRequest(MessageSerializer));

            else
            {
                WaitForResponse?.TrySetResult(decryptedMessage);
            }
        }

        private async void OnDataReceived(Guid sender, byte[] data)
        {
            await OnMessageReceived(MessageSerializer.Deserialize<WitMessage>(data));
        }

        private void OnServerDisconnected(Guid sender)
        {
            Disconnected(sender);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            WaitForRequest?.Dispose();
            Encryptor?.Dispose();
            Transport?.Dispose();
        }

        #endregion


        #region Properties

        private TimeSpan? Timeout { get; }


        private TaskCompletionSource<WitMessage?>? WaitForResponse { get; set; }

        private SemaphoreSlim WaitForRequest { get; }


        public bool IsInitialized { get; private set; }

        public bool IsAuthorized { get; private set; }

        #endregion

        #region Services

        private ITransportClient Transport { get; }

        public IMessageSerializer ParametersSerializer { get; }
        
        public IMessageSerializer MessageSerializer { get; }

        private IEncryptorClient Encryptor { get; }

        private IAccessTokenProvider TokenProvider { get; }

        private ILogger? Logger { get; }

        #endregion
    }
}

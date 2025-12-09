using System;
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

            WaitForRequest = new SemaphoreSlim(1, 1);
            ReconnectionCts = new CancellationTokenSource();
            RetryPolicy = new RetryPolicy(retryOptions, logger);

            IsInitialized = false;
            IsAuthorized = false;
            ConnectionState = ReconnectionState.Disconnected;
            
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
            return $"IsInitialized: {IsInitialized}, IsAuthorized: {IsAuthorized}, State: {ConnectionState}";
        }

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConnectionTimeout = timeout;

            if (!await Transport.ConnectAsync(timeout, cancellationToken))
                return false;

            if (!await ProcessInitialization(timeout))
                return false;

            if (!await ProcessAuthorization())
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

            return await RetryPolicy.ExecuteAsync(async () =>
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
            });
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
                        await ProcessAuthorization())
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

        private async void OnServerDisconnected(Guid sender)
        {
            var wasConnected = ConnectionState == ReconnectionState.Connected;
            
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
            WaitForRequest?.Dispose();
            Encryptor?.Dispose();
            Transport?.Dispose();
        }

        #endregion


        #region Properties

        private TimeSpan? Timeout { get; }

        private TimeSpan? ConnectionTimeout { get; set; }

        private TaskCompletionSource<WitMessage?>? WaitForResponse { get; set; }

        private SemaphoreSlim WaitForRequest { get; }

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

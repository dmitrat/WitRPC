using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OutWit.Communication.Client.Blazor.Encryption;
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Model;
using OutWit.Communication.Resilience;

namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Default implementation of IChannelFactory for WitRPC communication.
    /// Manages WebSocket connection lifecycle and authentication state.
    /// </summary>
    public sealed class ChannelFactory : IChannelFactory
    {
        #region Fields

        private readonly SemaphoreSlim m_gate = new(1, 1);
        
        #endregion

        #region Constructors

        public ChannelFactory(
            NavigationManager navigationManager, 
            AuthenticationStateProvider? authenticationProvider,
            IJSRuntime jsRuntime, 
            ChannelTokenProvider tokenProvider, 
            ChannelFactoryOptions options,
            ILogger<ChannelFactory> logger)
        {
            NavigationManager = navigationManager; 
            AuthenticationProvider = authenticationProvider;
            JsRuntime = jsRuntime;
            TokenProvider = tokenProvider;
            Options = options;
            Logger = logger;
            
            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            IsDisposed = false;
        }

        private void InitEvents()
        {
            if (AuthenticationProvider != null)
                AuthenticationProvider.AuthenticationStateChanged += OnAuthenticationProviderChanged;
        }

        #endregion

        #region Functions

        private async Task EnsureConnectedAsync()
        {
            if (Client is not null) 
                return;

            await m_gate.WaitAsync();
            try
            {
                Client ??= await CreateClientAsync();
            }
            finally
            {
                m_gate.Release();
            }
        }

        private async Task<WitClient?> CreateClientAsync()
        {
            var baseUri = new Uri(Options.BaseUrl ?? NavigationManager.BaseUri);
            var wsScheme = baseUri.Scheme == "https" ? "wss" : "ws";
            var apiUrl = $"{wsScheme}://{baseUri.Authority}/{Options.ApiPath.TrimStart('/')}";

            try
            {
                EncryptorClientWeb? encryptor = null;

                if (Options.UseEncryption)
                {
                    encryptor = new EncryptorClientWeb(JsRuntime);

                    if (!await encryptor.InitAsync())
                        throw new InvalidOperationException("Failed to initialize encryption");
                }

                var client = WitClientBuilder.Build(builder =>
                {
                    builder.WithWebSocket(apiUrl);
                    builder.WithMemoryPack();
                    builder.WithLogger(Logger);
                    builder.WithAccessTokenProvider(TokenProvider);
                    builder.WithTimeout(TimeSpan.FromSeconds(Options.TimeoutSeconds));

                    if (encryptor != null)
                        builder.WithEncryptor(encryptor);

                    ConfigureReconnect(builder);
                    ConfigureRetry(builder);

                    Options.ConfigureClient?.Invoke(builder);
                });

                var result = await client.ConnectAsync(
                    TimeSpan.FromSeconds(Options.TimeoutSeconds), 
                    CancellationToken.None);
                    
                if (!result)
                    throw new InvalidOperationException("Connection failed");

                return client;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to create WitRPC client for {Url}", apiUrl);
                return null;
            }
        }

        private async Task DisposeClientAsync()
        {
            if (Client != null)
            {
                await Client.Disconnect();
                Client.Dispose();
            }
            
            Client = null;
        }

        private void ConfigureReconnect(WitClientBuilderOptions builder)
        {
            if (Options.Reconnect is not { } reconnect)
                return;

            builder.WithAutoReconnect(r =>
            {
                r.MaxAttempts = reconnect.MaxAttempts;
                r.InitialDelay = reconnect.InitialDelay;
                r.MaxDelay = reconnect.MaxDelay;
                r.BackoffMultiplier = reconnect.BackoffMultiplier;
                r.ReconnectOnDisconnect = reconnect.ReconnectOnDisconnect;
            });
        }

        private void ConfigureRetry(WitClientBuilderOptions builder)
        {
            if (Options.Retry is not { } retry)
                return;

            builder.WithRetryPolicy(r =>
            {
                r.MaxRetries = retry.MaxRetries;
                r.InitialDelay = retry.InitialDelay;
                r.MaxDelay = retry.MaxDelay;
                r.BackoffMultiplier = retry.BackoffMultiplier;
                r.BackoffType = BackoffType.Exponential;

                r.RetryOnStatus(CommunicationStatus.InternalServerError);

                r.RetryOn<TimeoutException>();
                r.RetryOn<IOException>();
            });
        }

        #endregion

        #region IChannelFactory

        public async Task<T> GetServiceAsync<T>()
            where T : class
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(IsDisposed, this);
#else
            if (IsDisposed) throw new ObjectDisposedException(nameof(ChannelFactory));
#endif

            await EnsureConnectedAsync();

            if (Client == null)
                throw new InvalidOperationException("Client is not connected");

            return Client.GetService<T>();
        }

        public async Task ReconnectAsync()
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(IsDisposed, this);
#else
            if (IsDisposed) throw new ObjectDisposedException(nameof(ChannelFactory));
#endif

            await m_gate.WaitAsync();
            try
            {
                await DisposeClientAsync();
                Client = await CreateClientAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reconnect");
            }
            finally
            {
                m_gate.Release();
            }
        }

        #endregion
        
        #region Event Handlers

        private async void OnAuthenticationProviderChanged(Task<AuthenticationState> task)
        {
            try
            {
                var state = await task;
                if (state.User?.Identity?.IsAuthenticated == true)
                {
                    await ReconnectAsync();
                }
                else
                {
                    await m_gate.WaitAsync();
                    try
                    {
                        await DisposeClientAsync();
                    }
                    finally
                    {
                        m_gate.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process authentication status change");
            }
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) 
                return;
            
            IsDisposed = true;
            
            if (AuthenticationProvider != null)
                AuthenticationProvider.AuthenticationStateChanged -= OnAuthenticationProviderChanged;
            
            await DisposeClientAsync();
            
            m_gate.Dispose();
        }

        #endregion

        #region Properties
        
        private WitClient? Client { get; set; }
        
        private bool IsDisposed { get; set; }

        private NavigationManager NavigationManager { get; }
        
        private AuthenticationStateProvider? AuthenticationProvider { get; }
        
        private IJSRuntime JsRuntime { get; }
        
        private ChannelTokenProvider TokenProvider { get; }
        
        private ChannelFactoryOptions Options { get; }

        private ILogger<ChannelFactory> Logger { get; }

        #endregion
    }
}

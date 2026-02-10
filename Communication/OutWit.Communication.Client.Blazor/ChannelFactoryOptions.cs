using OutWit.Communication.Client;

namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Configuration options for <see cref="ChannelFactory"/>.
    /// </summary>
    public sealed class ChannelFactoryOptions
    {
        /// <summary>
        /// Optional absolute base URL for the WitRPC server.
        /// When <c>null</c>, the URL is derived from <c>NavigationManager.BaseUri</c> (same origin).
        /// Use this to connect to an external server (e.g. <c>"https://api.example.com"</c>).
        /// Default: <c>null</c>.
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// WebSocket API path (relative to base URL).
        /// Leading <c>/</c> is trimmed automatically.
        /// Default: <c>"api"</c>.
        /// </summary>
        public string ApiPath { get; set; } = "api";

        /// <summary>
        /// Connection and request timeout in seconds.
        /// Default: 10 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Whether to use RSA/AES encryption via the Web Crypto API.
        /// When <c>true</c>, an <see cref="Encryption.EncryptorClientWeb"/> is created per connection.
        /// When <c>false</c>, the default plain encryptor is used (no encryption).
        /// Default: <c>true</c>.
        /// </summary>
        public bool UseEncryption { get; set; } = true;

        /// <summary>
        /// Automatic reconnection policy.
        /// Set to <c>null</c> to disable auto-reconnect.
        /// </summary>
        public ChannelReconnectOptions? Reconnect { get; set; } = new();

        /// <summary>
        /// Per-call retry policy.
        /// Set to <c>null</c> to disable retries.
        /// </summary>
        public ChannelRetryOptions? Retry { get; set; } = new();

        /// <summary>
        /// Optional callback for advanced WitClient builder customization.
        /// Called <b>after</b> all typed settings (transport, encryption, reconnect, retry) have been applied,
        /// so it can override or extend any of them.
        /// </summary>
        /// <example>
        /// <code>
        /// options.ConfigureClient = builder =>
        /// {
        ///     builder.WithJson();          // switch serialization
        ///     builder.WithoutEncryption(); // disable encryption
        /// };
        /// </code>
        /// </example>
        public Action<WitClientBuilderOptions>? ConfigureClient { get; set; }
    }
}

namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Configuration for the automatic reconnection policy.
    /// Set <see cref="ChannelFactoryOptions.Reconnect"/> to <c>null</c> to disable auto-reconnect entirely.
    /// </summary>
    public sealed class ChannelReconnectOptions
    {
        /// <summary>
        /// Maximum number of reconnection attempts.
        /// <c>0</c> means unlimited.
        /// Default: <c>0</c>.
        /// </summary>
        public int MaxAttempts { get; set; }

        /// <summary>
        /// Delay before the first reconnection attempt.
        /// Default: 1 second.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Upper bound for the backoff delay between reconnection attempts.
        /// Default: 2 minutes.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Multiplier applied to the delay after each failed attempt.
        /// Default: 2.0 (exponential backoff).
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// When <c>true</c>, the client will automatically reconnect after an unexpected disconnect.
        /// Default: <c>true</c>.
        /// </summary>
        public bool ReconnectOnDisconnect { get; set; } = true;
    }
}

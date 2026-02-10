namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Configuration for the per-call retry policy.
    /// Set <see cref="ChannelFactoryOptions.Retry"/> to <c>null</c> to disable retries entirely.
    /// </summary>
    public sealed class ChannelRetryOptions
    {
        /// <summary>
        /// Maximum number of retry attempts before giving up.
        /// Default: 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay before the first retry attempt.
        /// Default: 500 milliseconds.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Upper bound for the backoff delay between retry attempts.
        /// Default: 10 seconds.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Multiplier applied to the delay after each failed retry.
        /// Default: 2.0 (exponential backoff).
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;
    }
}

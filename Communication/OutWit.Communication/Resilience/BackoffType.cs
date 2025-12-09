namespace OutWit.Communication.Resilience
{
    /// <summary>
    /// Specifies the type of backoff strategy for retry delays.
    /// </summary>
    public enum BackoffType
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        Fixed,

        /// <summary>
        /// Linear increase in delay between retries.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay between retries.
        /// </summary>
        Exponential
    }
}

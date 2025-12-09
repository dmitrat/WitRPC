namespace OutWit.Communication.Client.Reconnection
{
    /// <summary>
    /// Represents the current state of the reconnection process.
    /// </summary>
    public enum ReconnectionState
    {
        /// <summary>
        /// The client is connected and operating normally.
        /// </summary>
        Connected,

        /// <summary>
        /// The client is disconnected and not attempting to reconnect.
        /// </summary>
        Disconnected,

        /// <summary>
        /// The client is currently attempting to reconnect.
        /// </summary>
        Reconnecting,

        /// <summary>
        /// All reconnection attempts have failed.
        /// </summary>
        Failed
    }
}

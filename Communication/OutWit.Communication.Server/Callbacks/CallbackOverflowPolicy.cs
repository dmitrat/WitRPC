namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// What the server does when a connection's callback queue reaches its bound
    /// (<see cref="CallbackDeliveryOptions.MaxPendingCallbacks"/> or
    /// <see cref="CallbackDeliveryOptions.MaxPendingCallbackBytes"/>), and what a callback send
    /// timeout means. Responses are never subject to this policy.
    /// </summary>
    public enum CallbackOverflowPolicy
    {
        /// <summary>
        /// The default and the pre-3.2 behaviour: the callback is queued anyway, the overflow and a
        /// send timeout are logged as warnings, the connection stays open.
        /// </summary>
        Log,

        /// <summary>
        /// The callback is refused (<see cref="CallbackDeliveryStatus.QueueFull"/>) and the connection
        /// is closed; a send timeout closes it too. For a server whose client is expected to keep up
        /// and to reconnect when it cannot: closing is what tells it, and what keeps the other
        /// connections of the server unaffected.
        /// </summary>
        CloseConnection,

        /// <summary>
        /// The newest callback is dropped (<see cref="CallbackDeliveryStatus.QueueFull"/>), the
        /// connection stays open. Only for events the service declares lossy.
        /// </summary>
        DropNewest
    }
}

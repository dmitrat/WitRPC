namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// How a server delivers callbacks: whether an untargeted raise is allowed, how many callbacks
    /// may wait in one connection's outbound queue, and what happens beyond that. The defaults
    /// reproduce the pre-3.2 behaviour exactly.
    /// </summary>
    public sealed class CallbackDeliveryOptions
    {
        #region Functions

        /// <summary>
        /// Checks the option values.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">A bound is negative.</exception>
        public void Validate()
        {
            if (MaxPendingCallbacks < 0)
                throw new System.ArgumentOutOfRangeException(nameof(MaxPendingCallbacks), "The bound cannot be negative; 0 means unbounded");

            if (MaxPendingCallbackBytes < 0)
                throw new System.ArgumentOutOfRangeException(nameof(MaxPendingCallbackBytes), "The bound cannot be negative; 0 means unbounded");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether an event raised outside a <see cref="CallbackScope"/> goes to every authorized
        /// connection (the default) or is refused.
        /// </summary>
        public CallbackDeliveryMode Mode { get; set; } = CallbackDeliveryMode.BroadcastAllowed;

        /// <summary>
        /// The most callbacks that may wait in one connection's outbound queue; 0 (the default) is
        /// unbounded. Responses are not counted.
        /// </summary>
        public int MaxPendingCallbacks { get; set; }

        /// <summary>
        /// The most callback payload bytes that may wait in one connection's outbound queue; 0 (the
        /// default) is unbounded. Responses are not counted.
        /// </summary>
        public long MaxPendingCallbackBytes { get; set; }

        /// <summary>
        /// What happens when a bound is reached, and whether a callback send timeout closes the
        /// connection. <see cref="CallbackOverflowPolicy.Log"/> by default.
        /// </summary>
        public CallbackOverflowPolicy OverflowPolicy { get; set; } = CallbackOverflowPolicy.Log;

        #endregion
    }
}

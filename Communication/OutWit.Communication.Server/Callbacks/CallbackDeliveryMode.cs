namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// Whether a server delivers an untargeted event raise to every authorized connection.
    /// </summary>
    public enum CallbackDeliveryMode
    {
        /// <summary>
        /// The default and the pre-3.2 behaviour: an event raised outside a <see cref="CallbackScope"/>
        /// goes to every authorized connection of the server.
        /// </summary>
        BroadcastAllowed,

        /// <summary>
        /// An event must name its target: a raise outside a <see cref="CallbackScope"/> is refused
        /// (logged, reported as <see cref="CallbackDeliveryStatus.Refused"/>, sent to nobody). For a
        /// server whose clients must never see each other's events.
        /// </summary>
        TargetedOnly
    }
}

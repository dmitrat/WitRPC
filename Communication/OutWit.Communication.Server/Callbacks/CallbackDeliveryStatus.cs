namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// What happened to one callback on one connection.
    /// </summary>
    public enum CallbackDeliveryStatus
    {
        /// <summary>Accepted into the connection's outbound queue; not on the wire yet.</summary>
        Queued,

        /// <summary>Written to the transport.</summary>
        Sent,

        /// <summary>No connection with that id on this server.</summary>
        UnknownConnection,

        /// <summary>The connection exists but has not finished the handshake.</summary>
        NotAuthorized,

        /// <summary>The connection's callback queue is at its bound and the policy refused the callback.</summary>
        QueueFull,

        /// <summary>The transport failed while writing the callback (the connection is gone).</summary>
        SendFailed,

        /// <summary>The write did not finish within the server's timeout.</summary>
        SendTimedOut,

        /// <summary>The server runs in <see cref="CallbackDeliveryMode.TargetedOnly"/> and the raise named no target.</summary>
        Refused
    }
}

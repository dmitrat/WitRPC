namespace OutWit.Communication.MMF
{
    /// <summary>
    /// Why <see cref="MmfChannel.Receive"/> returned. Peer liveness is watched
    /// separately by <see cref="MmfPeerWatch"/>, so it is deliberately absent here:
    /// the frame reader never shares its wait with a presence mutex.
    /// </summary>
    internal enum MmfReceiveKind
    {
        /// <summary>A complete message was assembled.</summary>
        Message,

        /// <summary>The channel was stopped locally.</summary>
        Stopped,

        /// <summary>The caller's cancellation token fired.</summary>
        Cancelled,

        /// <summary>The frame header did not describe a valid frame.</summary>
        Corrupt,
    }
}

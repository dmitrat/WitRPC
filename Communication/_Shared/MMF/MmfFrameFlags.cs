namespace OutWit.Communication.MMF
{
    /// <summary>
    /// What a frame carries. Stored in the frame header.
    /// </summary>
    internal enum MmfFrameFlags
    {
        /// <summary>An application message.</summary>
        Data = 0,

        /// <summary>
        /// Sent once by the client right after it has attached. Tells the server
        /// that a client is actually present, and from that moment the server
        /// watches the client's presence mutex.
        /// </summary>
        Hello = 1,

        /// <summary>
        /// The server's answer to a <see cref="Hello"/>. It is what confirms to
        /// the client that a live server — not a dying instance still holding the
        /// same named objects during a restart handoff — received the hello. A
        /// client that gets no ack retries rather than declaring itself connected.
        /// </summary>
        HelloAck = 2,
    }
}

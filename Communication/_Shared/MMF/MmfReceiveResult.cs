namespace OutWit.Communication.MMF
{
    /// <summary>
    /// The outcome of one <see cref="MmfChannel.Receive"/> call.
    /// </summary>
    internal readonly struct MmfReceiveResult
    {
        #region Constructors

        private MmfReceiveResult(MmfReceiveKind kind, byte[]? data, MmfFrameFlags flags, string? reason)
        {
            Kind = kind;
            Data = data;
            Flags = flags;
            Reason = reason;
        }

        #endregion

        #region Factories

        public static MmfReceiveResult Message(byte[] data, MmfFrameFlags flags)
        {
            return new MmfReceiveResult(MmfReceiveKind.Message, data, flags, null);
        }

        public static MmfReceiveResult Stopped()
        {
            return new MmfReceiveResult(MmfReceiveKind.Stopped, null, MmfFrameFlags.Data, null);
        }

        public static MmfReceiveResult Cancelled()
        {
            return new MmfReceiveResult(MmfReceiveKind.Cancelled, null, MmfFrameFlags.Data, null);
        }

        public static MmfReceiveResult Corrupt(string reason)
        {
            return new MmfReceiveResult(MmfReceiveKind.Corrupt, null, MmfFrameFlags.Data, reason);
        }

        #endregion

        #region Properties

        public MmfReceiveKind Kind { get; }

        public byte[]? Data { get; }

        public MmfFrameFlags Flags { get; }

        public string? Reason { get; }

        #endregion
    }
}

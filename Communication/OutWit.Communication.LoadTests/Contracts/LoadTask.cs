namespace OutWit.Communication.LoadTests.Contracts
{
    /// <summary>
    /// One unit of work pushed to a node.
    /// </summary>
    public sealed class LoadTask
    {
        public long Id { get; set; }

        public int NodeIndex { get; set; }

        public long DispatchedAtTicks { get; set; }

        public byte[] Payload { get; set; } = Array.Empty<byte>();
    }
}

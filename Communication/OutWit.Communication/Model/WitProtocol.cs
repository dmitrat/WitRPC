namespace OutWit.Communication.Model
{
    /// <summary>
    /// The wire protocol version this build speaks. Exchanged during
    /// initialization: the client states its version, and a server that speaks a
    /// different one refuses the handshake with a readable error instead of a
    /// decode failure. Bump only with a coordinated release of both ends.
    /// </summary>
    public static class WitProtocol
    {
        public const int VERSION = 3;
    }
}

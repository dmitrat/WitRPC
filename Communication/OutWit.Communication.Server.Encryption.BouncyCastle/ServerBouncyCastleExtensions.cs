using OutWit.Communication.Server;

namespace OutWit.Communication.Server.Encryption.BouncyCastle
{
    /// <summary>
    /// Extension methods for configuring BouncyCastle encryption on WitRPC server.
    /// </summary>
    public static class ServerBouncyCastleExtensions
    {
        /// <summary>
        /// Configures the server to use BouncyCastle encryption.
        /// This provides cross-platform encryption that is compatible with 
        /// BouncyCastle clients (e.g., Blazor WebAssembly) and standard .NET clients.
        /// </summary>
        /// <param name="me">The server builder options.</param>
        /// <returns>The server builder options for chaining.</returns>
        public static WitServerBuilderOptions WithBouncyCastleEncryption(this WitServerBuilderOptions me)
        {
            me.EncryptorFactory = new EncryptorServerBouncyCastleFactory();
            return me;
        }
    }
}

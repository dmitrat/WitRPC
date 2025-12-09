using OutWit.Communication.Client;

namespace OutWit.Communication.Client.Encryption.BouncyCastle
{
    /// <summary>
    /// Extension methods for configuring BouncyCastle encryption on WitRPC client.
    /// </summary>
    public static class ClientBouncyCastleExtensions
    {
        /// <summary>
        /// Configures the client to use BouncyCastle encryption.
        /// This provides cross-platform encryption that works in Blazor WebAssembly
        /// and all .NET platforms without requiring platform-specific crypto APIs.
        /// </summary>
        /// <param name="me">The client builder options.</param>
        /// <returns>The client builder options for chaining.</returns>
        public static WitClientBuilderOptions WithBouncyCastleEncryption(this WitClientBuilderOptions me)
        {
            me.Encryptor = new EncryptorClientBouncyCastle();
            return me;
        }
    }
}

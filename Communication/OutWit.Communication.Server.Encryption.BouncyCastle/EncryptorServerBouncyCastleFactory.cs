using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Encryption.BouncyCastle
{
    /// <summary>
    /// Factory for creating BouncyCastle-based encryptors.
    /// </summary>
    public class EncryptorServerBouncyCastleFactory : IEncryptorServerFactory
    {
        public IEncryptorServer CreateEncryptor()
        {
            return new EncryptorServerBouncyCastle();
        }
    }
}

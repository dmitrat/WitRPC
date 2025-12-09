using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IEncryptorServer : IEncryptor
    {
        public byte[] GetSymmetricKey();

        public byte[] GetVector();

        public void Reset();

        /// <summary>
        /// Encrypts data using the client's public key.
        /// This is used to securely send the symmetric key to the client during initialization.
        /// </summary>
        /// <param name="data">The data to encrypt (typically the symmetric key and IV).</param>
        /// <param name="clientPublicKey">The client's public key.</param>
        /// <returns>The encrypted data.</returns>
        public Task<byte[]> EncryptForClient(byte[] data, byte[] clientPublicKey);
    }
}

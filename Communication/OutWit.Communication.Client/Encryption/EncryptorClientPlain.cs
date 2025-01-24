using System;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Encryption
{
    public class EncryptorClientPlain : IEncryptorClient
    {
        #region IEncryptorClient

        public byte[] GetPublicKey()
        {
            return new byte[] { 0 };
        }

        public byte[] GetPrivateKey()
        {
            return new byte[] { 0 };
        }

        public async Task<byte[]> DecryptRsa(byte[] data)
        {
            return data;
        }

        public bool ResetAes(byte[] symmetricKey, byte[] vector)
        {
            return true;
        }

        #endregion
        
        #region IEncryptor

        public async Task<byte[]> Encrypt(byte[] data)
        {
            return data;
        }

        public async Task<byte[]> Decrypt(byte[] data)
        {
            return data;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion
    }
}

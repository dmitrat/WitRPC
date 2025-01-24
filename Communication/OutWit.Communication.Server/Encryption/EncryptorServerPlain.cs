using System;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Encryption
{
    public class EncryptorServerPlain : IEncryptorServer
    {
        #region IEncryptorServer

        public byte[] GetSymmetricKey()
        {
            return new byte[] { 0 };
        }

        public byte[] GetVector()
        {
            return new byte[] { 0 };
        }

        public void Reset()
        {
            
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

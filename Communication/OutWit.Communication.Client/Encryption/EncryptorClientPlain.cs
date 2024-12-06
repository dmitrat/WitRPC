using System;
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

        public bool ResetAes(byte[] symmetricKey, byte[] vector)
        {
            return true;
        }

        #endregion
        
        #region IEncryptor

        public byte[] Encrypt(byte[] data)
        {
            return data;
        }

        public byte[] Decrypt(byte[] data)
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

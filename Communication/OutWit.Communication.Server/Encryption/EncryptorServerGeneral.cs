using System;
using System.Security.Cryptography;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Encryption
{
    public class EncryptorServerGeneral : IEncryptorServer
    {
        #region Constructors

        public EncryptorServerGeneral()
        {
            Reset();
        }

        #endregion

        #region IEncryptorServer

        public byte[] GetSymmetricKey()
        {
            return Aes.Key;
        }

        public byte[] GetVector()
        {
            return Aes.IV;
        }

        public void Reset()
        {
            Aes = Aes.Create();
        }

        #endregion

        #region IEncryptor

        public byte[] Encrypt(byte[] data)
        {
            using ICryptoTransform encryptor = Aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public byte[] Decrypt(byte[] data)
        {
            using ICryptoTransform decryptor = Aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Aes.Dispose();
        }

        #endregion

        #region Properties

        private Aes Aes { get; set; }

        #endregion

 
    }
}

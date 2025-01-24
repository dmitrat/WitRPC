using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter.Xml;
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
            Aes.Mode = CipherMode.CBC;
            Aes.Padding = PaddingMode.PKCS7;
        }

        #endregion

        #region IEncryptor

        public async Task<byte[]> Encrypt(byte[] data)
        {
            using ICryptoTransform encryptor = Aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public async Task<byte[]> Decrypt(byte[] data)
        {
            try
            {
                using ICryptoTransform decryptor = Aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(data, 0, data.Length);
            }
            catch (Exception e)
            {

                int hh = 0;

                return new byte[0];
            }
      
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

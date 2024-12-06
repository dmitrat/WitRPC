using System;
using System.Security.Cryptography;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client.Encryption
{
    public class EncryptorClientGeneral : IEncryptorClient
    {
        #region Constants

        private const int KEY_SIZE = 2048;

        #endregion

        #region Constructors

        public EncryptorClientGeneral()
        {
            using var rsa = RSA.Create();

            rsa.KeySize = KEY_SIZE;

            PrivateKey = rsa.ExportParameters(true);
            PublicKey = rsa.ExportParameters(false);
        }

        #endregion

        #region IEncryptor

        public byte[] GetPublicKey()
        {
            return PublicKey.ToBytes();
        }

        public byte[] GetPrivateKey()
        {
            return PrivateKey.ToBytes();
        }

        public bool ResetAes(byte[] symmetricKey, byte[] vector)
        {
            try
            {
                Aes = Aes.Create();
                Aes.Key = symmetricKey;
                Aes.IV = vector;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        #region IEncryptor

        public byte[] Encrypt(byte[] data)
        {
            using ICryptoTransform encryptor = Aes!.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public byte[] Decrypt(byte[] data)
        {
            using ICryptoTransform decryptor = Aes!.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Aes?.Dispose();
        }

        #endregion

        #region Properties

        private RSAParameters PublicKey { get; }

        private RSAParameters PrivateKey { get; }

        private Aes? Aes { get; set; }

        #endregion
    }
}

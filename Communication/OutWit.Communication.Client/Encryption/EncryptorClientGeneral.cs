using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Encryption;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client.Encryption
{
    /// <summary>
    /// The protocol-3 client encryptor: offers an RSA public key, receives the
    /// server's master key and salt through it, derives one AES-256-GCM key per
    /// direction, and moves every message as an authenticated, counter-framed
    /// payload (<see cref="AeadCipher"/>).
    /// </summary>
    public class EncryptorClientGeneral : IEncryptorClient
    {
        #region Constants

        private const int KEY_SIZE = 2048;

        #endregion

        #region Fields

        private AeadCipher? m_send;

        private AeadCipher? m_receive;

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

        #region IEncryptorClient

        public byte[] GetPublicKey()
        {
            return PublicKey.ToBytes();
        }

        public byte[] GetPrivateKey()
        {
            return PrivateKey.ToBytes();
        }

        public Task<byte[]> DecryptRsa(byte[] data)
        {
            return Task.FromResult(data.DecryptRsa(PrivateKey));
        }

        public bool ResetAes(byte[] symmetricKey, byte[] vector)
        {
            try
            {
                m_send?.Dispose();
                m_receive?.Dispose();

                var (clientToServer, serverToClient) = AeadCipher.DeriveKeys(symmetricKey, vector);

                m_send = new AeadCipher(clientToServer, AeadCipher.DIRECTION_CLIENT_TO_SERVER);
                m_receive = new AeadCipher(serverToClient, AeadCipher.DIRECTION_SERVER_TO_CLIENT);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region IEncryptor

        public Task<byte[]> Encrypt(byte[] data)
        {
            if (m_send == null)
                throw new WitExceptionEncryption("Encryptor is not initialized; the handshake has not completed");

            return Task.FromResult(m_send.Seal(data));
        }

        public Task<byte[]> Decrypt(byte[] data)
        {
            if (m_receive == null)
                throw new WitExceptionEncryption("Encryptor is not initialized; the handshake has not completed");

            return Task.FromResult(m_receive.Open(data));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_send?.Dispose();
            m_receive?.Dispose();
        }

        #endregion

        #region Properties

        private RSAParameters PublicKey { get; }

        private RSAParameters PrivateKey { get; }

        #endregion
    }
}

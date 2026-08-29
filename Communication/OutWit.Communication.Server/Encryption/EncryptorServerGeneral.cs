using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Encryption;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Encryption
{
    /// <summary>
    /// The protocol-3 server encryptor: hands the client a fresh master key and
    /// salt through the RSA handshake, derives one AES-256-GCM key per direction
    /// from them, and moves every message as an authenticated, counter-framed
    /// payload (<see cref="AeadCipher"/>). A frame that fails authentication
    /// throws instead of decrypting quietly into garbage.
    /// </summary>
    public class EncryptorServerGeneral : IEncryptorServer
    {
        #region Fields

        private byte[] m_masterKey = Array.Empty<byte>();

        private byte[] m_salt = Array.Empty<byte>();

        private AeadCipher? m_send;

        private AeadCipher? m_receive;

        #endregion

        #region Constructors

        public EncryptorServerGeneral()
        {
            Reset();
        }

        #endregion

        #region IEncryptorServer

        public byte[] GetSymmetricKey()
        {
            return m_masterKey;
        }

        public byte[] GetVector()
        {
            return m_salt;
        }

        public void Reset()
        {
            m_send?.Dispose();
            m_receive?.Dispose();

            m_masterKey = RandomNumberGenerator.GetBytes(AeadCipher.KEY_SIZE);
            m_salt = RandomNumberGenerator.GetBytes(AeadCipher.SALT_SIZE);

            var (clientToServer, serverToClient) = AeadCipher.DeriveKeys(m_masterKey, m_salt);

            m_send = new AeadCipher(serverToClient, AeadCipher.DIRECTION_SERVER_TO_CLIENT);
            m_receive = new AeadCipher(clientToServer, AeadCipher.DIRECTION_CLIENT_TO_SERVER);
        }

        public Task<byte[]> EncryptForClient(byte[] data, byte[] clientPublicKey)
        {
            return Task.FromResult(data.EncryptRsa(clientPublicKey.ToRsaParameters()));
        }

        #endregion

        #region IEncryptor

        public Task<byte[]> Encrypt(byte[] data)
        {
            if (m_send == null)
                throw new WitExceptionEncryption("Encryptor is not initialized");

            return Task.FromResult(m_send.Seal(data));
        }

        public Task<byte[]> Decrypt(byte[] data)
        {
            if (m_receive == null)
                throw new WitExceptionEncryption("Encryptor is not initialized");

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
    }
}

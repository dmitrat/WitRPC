using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Encryption.BouncyCastle
{
    /// <summary>
    /// BouncyCastle-based encryption server that is compatible with BouncyCastle client.
    /// Uses RSA-OAEP for key exchange and AES-CBC for symmetric encryption.
    /// </summary>
    public class EncryptorServerBouncyCastle : IEncryptorServer
    {
        #region Constants

        private const int AES_KEY_SIZE = 32; // 256 bits
        private const int AES_IV_SIZE = 16;  // 128 bits

        #endregion

        #region Constructors

        public EncryptorServerBouncyCastle()
        {
            Reset();
        }

        #endregion

        #region IEncryptorServer

        public byte[] GetSymmetricKey()
        {
            return AesKey;
        }

        public byte[] GetVector()
        {
            return AesIv;
        }

        public void Reset()
        {
            m_send?.Dispose();
            m_receive?.Dispose();

            var random = new SecureRandom();

            AesKey = new byte[AES_KEY_SIZE];
            random.NextBytes(AesKey);

            AesIv = new byte[AES_IV_SIZE];
            random.NextBytes(AesIv);

            var (clientToServer, serverToClient) = AeadCipher.DeriveKeys(AesKey, AesIv);

            m_send = new AeadCipher(serverToClient, AeadCipher.DIRECTION_SERVER_TO_CLIENT);
            m_receive = new AeadCipher(clientToServer, AeadCipher.DIRECTION_CLIENT_TO_SERVER);
        }

        public Task<byte[]> EncryptForClient(byte[] data, byte[] clientPublicKey)
        {
            try
            {
                // Parse the client's public key from JSON
                var publicKeyInfo = JsonSerializer.Deserialize<RsaPublicKeyInfo>(
                    Encoding.UTF8.GetString(clientPublicKey));

                if (publicKeyInfo == null || publicKeyInfo.Modulus == null || publicKeyInfo.Exponent == null)
                    throw new InvalidOperationException("Invalid public key format");

                // Create BouncyCastle RSA public key
                var modulus = new BigInteger(1, publicKeyInfo.Modulus);
                var exponent = new BigInteger(1, publicKeyInfo.Exponent);
                var rsaPublicKey = new RsaKeyParameters(false, modulus, exponent);

                // Encrypt using RSA-OAEP with SHA-256
                var engine = new OaepEncoding(new RsaEngine(), new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
                engine.Init(true, rsaPublicKey);

                var encrypted = engine.ProcessBlock(data, 0, data.Length);
                return Task.FromResult(encrypted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to encrypt data for client", ex);
            }
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

        #region Properties

        private AeadCipher? m_send;

        private AeadCipher? m_receive;

        private byte[] AesKey { get; set; } = null!;

        private byte[] AesIv { get; set; } = null!;

        #endregion
    }

    /// <summary>
    /// RSA public key information for JSON deserialization.
    /// Compatible with .NET RSAParameters format.
    /// </summary>
    internal class RsaPublicKeyInfo
    {
        public byte[] Modulus { get; set; } = null!;
        public byte[] Exponent { get; set; } = null!;
    }
}

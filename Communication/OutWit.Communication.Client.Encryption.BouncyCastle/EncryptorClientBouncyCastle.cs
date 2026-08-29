using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client.Encryption.BouncyCastle
{
    /// <summary>
    /// BouncyCastle-based encryption client that works on all platforms including Blazor WebAssembly.
    /// Uses RSA-OAEP for key exchange and AES-CBC for symmetric encryption.
    /// </summary>
    public class EncryptorClientBouncyCastle : IEncryptorClient
    {
        #region Constants

        private const int KEY_SIZE = 2048;
        private const int AES_KEY_SIZE = 256;
        private const int AES_BLOCK_SIZE = 128;

        #endregion

        #region Constructors

        public EncryptorClientBouncyCastle()
        {
            GenerateKeyPair();
        }

        #endregion

        #region Key Generation

        private void GenerateKeyPair()
        {
            var keyGenerationParameters = new KeyGenerationParameters(new SecureRandom(), KEY_SIZE);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var keyPair = keyPairGenerator.GenerateKeyPair();
            PrivateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;
            PublicKey = (RsaKeyParameters)keyPair.Public;
        }

        #endregion

        #region IEncryptorClient

        public byte[] GetPublicKey()
        {
            var publicKeyInfo = new RsaPublicKeyInfo
            {
                Modulus = PublicKey.Modulus.ToByteArrayUnsigned(),
                Exponent = PublicKey.Exponent.ToByteArrayUnsigned()
            };
            
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(publicKeyInfo));
        }

        public byte[] GetPrivateKey()
        {
            var privateKeyInfo = new RsaPrivateKeyInfo
            {
                Modulus = PrivateKey.Modulus.ToByteArrayUnsigned(),
                Exponent = PrivateKey.Exponent.ToByteArrayUnsigned(),
                D = PrivateKey.Exponent.ToByteArrayUnsigned(),
                P = PrivateKey.P.ToByteArrayUnsigned(),
                Q = PrivateKey.Q.ToByteArrayUnsigned(),
                DP = PrivateKey.DP.ToByteArrayUnsigned(),
                DQ = PrivateKey.DQ.ToByteArrayUnsigned(),
                InverseQ = PrivateKey.QInv.ToByteArrayUnsigned()
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(privateKeyInfo));
        }

        public Task<byte[]> DecryptRsa(byte[] data)
        {
            var engine = new OaepEncoding(new RsaEngine(), new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
            engine.Init(false, PrivateKey);
            
            var decrypted = engine.ProcessBlock(data, 0, data.Length);
            return Task.FromResult(decrypted);
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

        private RsaKeyParameters PublicKey { get; set; } = null!;

        private RsaPrivateCrtKeyParameters PrivateKey { get; set; } = null!;

        private AeadCipher? m_send;

        private AeadCipher? m_receive;

        private byte[] AesKey { get; set; } = null!;

        private byte[] AesIv { get; set; } = null!;

        #endregion
    }

    /// <summary>
    /// RSA public key information for JSON serialization.
    /// Compatible with .NET RSAParameters format.
    /// </summary>
    internal class RsaPublicKeyInfo
    {
        public byte[] Modulus { get; set; } = null!;
        public byte[] Exponent { get; set; } = null!;
    }

    /// <summary>
    /// RSA private key information for JSON serialization.
    /// Compatible with .NET RSAParameters format.
    /// </summary>
    internal class RsaPrivateKeyInfo
    {
        public byte[] Modulus { get; set; } = null!;
        public byte[] Exponent { get; set; } = null!;
        public byte[] D { get; set; } = null!;
        public byte[] P { get; set; } = null!;
        public byte[] Q { get; set; } = null!;
        public byte[] DP { get; set; } = null!;
        public byte[] DQ { get; set; } = null!;
        public byte[] InverseQ { get; set; } = null!;
    }
}

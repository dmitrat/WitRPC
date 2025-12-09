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
using OutWit.Communication.Interfaces;

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
            var random = new SecureRandom();
            
            AesKey = new byte[AES_KEY_SIZE];
            random.NextBytes(AesKey);
            
            AesIv = new byte[AES_IV_SIZE];
            random.NextBytes(AesIv);
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
            var cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7Padding");
            cipher.Init(true, new ParametersWithIV(new KeyParameter(AesKey), AesIv));

            var encrypted = cipher.DoFinal(data);
            return Task.FromResult(encrypted);
        }

        public Task<byte[]> Decrypt(byte[] data)
        {
            try
            {
                var cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7Padding");
                cipher.Init(false, new ParametersWithIV(new KeyParameter(AesKey), AesIv));

                var decrypted = cipher.DoFinal(data);
                return Task.FromResult(decrypted);
            }
            catch (Exception)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // BouncyCastle doesn't require explicit disposal
        }

        #endregion

        #region Properties

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

using Microsoft.JSInterop;
using OutWit.Communication.Interfaces;
using System.Text;
using System.Text.Json;
using OutWit.Communication.Client.Blazor.Extensions;
using OutWit.Communication.Model;
using OutWit.Communication.Encryption;

namespace OutWit.Communication.Client.Blazor.Encryption
{
    /// <summary>
    /// Client-side encryptor using browser's Web Crypto API.
    /// Handles RSA key exchange and AES encryption for WitRPC communication.
    /// </summary>
    public sealed class EncryptorClientWeb : IEncryptorClient
    {
        #region Constants

        private static readonly string SEND_AAD =
            Convert.ToBase64String(new[] { (byte)WitProtocol.VERSION, AeadCipher.DIRECTION_CLIENT_TO_SERVER });

        private static readonly string RECEIVE_AAD =
            Convert.ToBase64String(new[] { (byte)WitProtocol.VERSION, AeadCipher.DIRECTION_SERVER_TO_CLIENT });

        #endregion

        #region Fields

        private string? m_sendKey;

        private string? m_receiveKey;

        private ulong m_sendCounter;

        private ulong m_receiveCounter;

        #endregion

        #region Constructors

        public EncryptorClientWeb(IJSRuntime jsRuntime)
        {
            Runtime = jsRuntime;
        }

        #endregion

        #region Initialization

        public async Task<bool> InitAsync()
        {
            if (IsInitialized)
                return true;

            // generateKeys returns BOTH freshly generated JWKs in one call (no shared global key in
            // the browser) so concurrent encryptors don't clobber each other's key. See cryptoInterop.js.
            var keysJson = await Runtime.InvokeAsync<string>("cryptoInterop.generateKeys", 2048);

            using var document = JsonDocument.Parse(keysJson);
            var publicKeyJwk = document.RootElement.GetProperty("publicKey").GetRawText();
            var privateKeyJwk = document.RootElement.GetProperty("privateKey").GetRawText();

            // Keep the per-instance private JWK; DecryptRsa hands it back to JS so the browser never
            // relies on shared global key state for decryption.
            PrivateKeyJwk = privateKeyJwk;

            var options = new JsonSerializerOptions
            {
                Converters = { new DualNameJsonConverter() }
            };

            var publicKey = JsonSerializer.Deserialize<RSAParametersWeb>(publicKeyJwk, options);
            var publicKeyJson = JsonSerializer.Serialize(publicKey, options);
            PublicKey = Encoding.UTF8.GetBytes(publicKeyJson);

            var privateKey = JsonSerializer.Deserialize<RSAParametersWeb>(privateKeyJwk, options);
            var privateKeyJson = JsonSerializer.Serialize(privateKey, options);
            PrivateKey = Encoding.UTF8.GetBytes(privateKeyJson);

            IsInitialized = true;

            return true;
        }

        #endregion

        #region IEncryptor

        public byte[] GetPublicKey()
        {
            return PublicKey ?? Array.Empty<byte>();
        }

        public byte[] GetPrivateKey()
        {
            return PrivateKey ?? Array.Empty<byte>();
        }

        public async Task<byte[]> DecryptRsa(byte[] data)
        {
            if (PrivateKeyJwk == null)
                throw new InvalidOperationException("RSA key not initialized. Call InitAsync first.");

            var result = await Runtime.InvokeAsync<string>("cryptoInterop.decryptRSA", PrivateKeyJwk, Convert.ToBase64String(data));

            return Convert.FromBase64String(result.Base64UrlToBase64());
        }

        public bool ResetAes(byte[] symmetricKey, byte[] vector)
        {
            try
            {
                // The GCM keys are derived in managed code (HKDF is pure HMAC,
                // browser-safe); only the per-message GCM work goes through
                // SubtleCrypto. Counters live here so the JS stays stateless.
                var (clientToServer, serverToClient) = AeadCipher.DeriveKeys(symmetricKey, vector);

                m_sendKey = Convert.ToBase64String(clientToServer);
                m_receiveKey = Convert.ToBase64String(serverToClient);
                m_sendCounter = 0;
                m_receiveCounter = 0;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<byte[]> Encrypt(byte[] data)
        {
            if (m_sendKey == null)
                throw new InvalidOperationException("AES encryption not initialized. Call ResetAes first.");

            ulong counter = m_sendCounter;

            var result = await Runtime.InvokeAsync<string>(
                "cryptoInterop.encryptAesGcm",
                m_sendKey,
                Convert.ToBase64String(Nonce(counter)),
                SEND_AAD,
                Convert.ToBase64String(data));

            // The counter is consumed only once the JS call succeeded; a failed
            // call must not desync the channel (mirrors AeadCipher.Seal).
            m_sendCounter++;

            // WebCrypto returns ciphertext with the tag appended; reframe to the
            // shared [counter:8][tag:16][ciphertext] layout.
            byte[] cipherAndTag = Convert.FromBase64String(result.Base64UrlToBase64());
            int cipherLength = cipherAndTag.Length - AeadCipher.TAG_SIZE;

            var frame = new byte[AeadCipher.COUNTER_SIZE + AeadCipher.TAG_SIZE + cipherLength];
            BitConverter.TryWriteBytes(frame, counter);
            Buffer.BlockCopy(cipherAndTag, cipherLength, frame, AeadCipher.COUNTER_SIZE, AeadCipher.TAG_SIZE);
            Buffer.BlockCopy(cipherAndTag, 0, frame, AeadCipher.COUNTER_SIZE + AeadCipher.TAG_SIZE, cipherLength);

            return frame;
        }

        public async Task<byte[]> Decrypt(byte[] data)
        {
            if (m_receiveKey == null)
                throw new InvalidOperationException("AES encryption not initialized. Call ResetAes first.");

            if (data.Length < AeadCipher.COUNTER_SIZE + AeadCipher.TAG_SIZE)
                throw new InvalidOperationException("Encrypted frame is too short");

            ulong counter = BitConverter.ToUInt64(data, 0);
            if (counter != m_receiveCounter)
                throw new InvalidOperationException($"Out-of-order encrypted frame: expected {m_receiveCounter}, received {counter}");

            // Reframe to the ciphertext-then-tag layout WebCrypto expects.
            int cipherLength = data.Length - AeadCipher.COUNTER_SIZE - AeadCipher.TAG_SIZE;
            var cipherAndTag = new byte[cipherLength + AeadCipher.TAG_SIZE];
            Buffer.BlockCopy(data, AeadCipher.COUNTER_SIZE + AeadCipher.TAG_SIZE, cipherAndTag, 0, cipherLength);
            Buffer.BlockCopy(data, AeadCipher.COUNTER_SIZE, cipherAndTag, cipherLength, AeadCipher.TAG_SIZE);

            var result = await Runtime.InvokeAsync<string>(
                "cryptoInterop.decryptAesGcm",
                m_receiveKey,
                Convert.ToBase64String(Nonce(counter)),
                RECEIVE_AAD,
                Convert.ToBase64String(cipherAndTag));

            m_receiveCounter++;
            return Convert.FromBase64String(result.Base64UrlToBase64());
        }

        private static byte[] Nonce(ulong counter)
        {
            var nonce = new byte[AeadCipher.NONCE_SIZE];
            BitConverter.TryWriteBytes(nonce, counter);
            return nonce;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            PublicKey = null;
            PrivateKey = null;
            PrivateKeyJwk = null;
            m_sendKey = null;
            m_receiveKey = null;
            IsInitialized = false;
        }

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }

        private IJSRuntime Runtime { get; }

        private byte[]? PublicKey { get; set; }

        private byte[]? PrivateKey { get; set; }

        /// <summary>The raw private-key JWK, passed to JS per decryption so no shared global key is used.</summary>
        private string? PrivateKeyJwk { get; set; }



        #endregion
    }
}

using Microsoft.JSInterop;
using OutWit.Communication.Interfaces;
using System.Text;
using System.Text.Json;
using OutWit.Communication.Client.Blazor.Extensions;

namespace OutWit.Communication.Client.Blazor.Encryption
{
    /// <summary>
    /// Client-side encryptor using browser's Web Crypto API.
    /// Handles RSA key exchange and AES encryption for WitRPC communication.
    /// </summary>
    public sealed class EncryptorClientWeb : IEncryptorClient
    {
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
            AesKey = Convert.ToBase64String(symmetricKey);
            AesIv = Convert.ToBase64String(vector);

            return true;
        }

        public async Task<byte[]> Encrypt(byte[] data)
        {
            if (AesKey == null || AesIv == null)
                throw new InvalidOperationException("AES encryption not initialized. Call ResetAes first.");

            var result = await Runtime.InvokeAsync<string>("cryptoInterop.encryptAes", AesKey, AesIv, Convert.ToBase64String(data));

            return Convert.FromBase64String(result.Base64UrlToBase64());
        }

        public async Task<byte[]> Decrypt(byte[] data)
        {
            if (AesKey == null || AesIv == null)
                throw new InvalidOperationException("AES encryption not initialized. Call ResetAes first.");

            var result = await Runtime.InvokeAsync<string>("cryptoInterop.decryptAes", AesKey, AesIv, Convert.ToBase64String(data));

            return Convert.FromBase64String(result.Base64UrlToBase64());
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            PublicKey = null;
            PrivateKey = null;
            PrivateKeyJwk = null;
            AesKey = null;
            AesIv = null;
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

        private string? AesKey { get; set; }

        private string? AesIv { get; set; }

        #endregion
    }
}

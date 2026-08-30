using System.Text;
using Microsoft.JSInterop;
using OutWit.Communication.Client.Blazor.Encryption;

namespace OutWit.Communication.Client.Blazor.Tests
{
    [TestFixture]
    public class EncryptorClientWebTests
    {
        #region Mocks

        /// <summary>
        /// Mock IJSRuntime that hands out a queued keypair per <c>generateKeys</c> call and records the
        /// arguments of every <c>decryptRSA</c> call (so a test can assert which private key was used).
        /// </summary>
        private sealed class RecordingJSRuntime : IJSRuntime
        {
            private readonly Queue<string> m_keyPairs;

            public RecordingJSRuntime(params string[] keyPairsJson)
            {
                m_keyPairs = new Queue<string>(keyPairsJson);
            }

            public List<object?[]?> DecryptCalls { get; } = new();

            public string DecryptResultBase64 { get; set; } = "AAAA";

            /// <summary>What the interop probe answers: true = the browser runs the current script.</summary>
            public bool InteropCurrent { get; set; } = true;

            /// <summary>Every module URL handed to <c>import</c>.</summary>
            public List<string?> Imports { get; } = new();

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                object result = identifier switch
                {
                    "eval" => InteropCurrent,
                    "import" => RecordImport(args),
                    "cryptoInterop.generateKeys" => m_keyPairs.Dequeue(),
                    "cryptoInterop.decryptRSA" => RecordDecrypt(args),
                    _ => default(TValue)!
                };

                return new ValueTask<TValue>((TValue)result);
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                return InvokeAsync<TValue>(identifier, args);
            }

            private string RecordDecrypt(object?[]? args)
            {
                DecryptCalls.Add(args);
                return DecryptResultBase64;
            }

            private object RecordImport(object?[]? args)
            {
                Imports.Add(args?[0] as string);
                return null!;
            }
        }

        #endregion

        #region Fixtures

        // Minimal but well-formed JWK pair. The values are placeholders (the mock does no real crypto);
        // "d" identifies the private key so a test can tell whose key reached decryptRSA.
        private static string KeyPair(string modulus, string privateExponent)
        {
            return "{\"publicKey\":{\"kty\":\"RSA\",\"n\":\"" + modulus + "\",\"e\":\"AQAB\"}," +
                   "\"privateKey\":{\"kty\":\"RSA\",\"n\":\"" + modulus + "\",\"e\":\"AQAB\",\"d\":\"" + privateExponent + "\"}}";
        }

        #endregion

        #region Init Tests

        [Test]
        public async Task InitReimportsTheInteropModuleWhenTheCachedScriptIsStaleTest()
        {
            var runtime = new RecordingJSRuntime(KeyPair("nA", "dA")) { InteropCurrent = false };
            var encryptor = new EncryptorClientWeb(runtime);

            var initialized = await encryptor.InitAsync();

            Assert.That(initialized, Is.True);
            Assert.That(runtime.Imports, Has.Count.EqualTo(1));
            Assert.That(runtime.Imports[0], Does.Contain("_content/OutWit.Communication.Client.Blazor/js/cryptoInterop.js?v="));
        }

        [Test]
        public async Task InitLeavesACurrentInteropScriptAloneTest()
        {
            var runtime = new RecordingJSRuntime(KeyPair("nA", "dA")) { InteropCurrent = true };
            var encryptor = new EncryptorClientWeb(runtime);

            await encryptor.InitAsync();

            Assert.That(runtime.Imports, Is.Empty);
        }

        [Test]
        public async Task InitParsesKeyPairAndExposesPublicAndPrivateKeysTest()
        {
            var runtime = new RecordingJSRuntime(KeyPair("modA", "privA"));
            var encryptor = new EncryptorClientWeb(runtime);

            var initialized = await encryptor.InitAsync();

            Assert.That(initialized, Is.True);
            Assert.That(encryptor.IsInitialized, Is.True);

            var publicKey = Encoding.UTF8.GetString(encryptor.GetPublicKey());
            var privateKey = Encoding.UTF8.GetString(encryptor.GetPrivateKey());

            Assert.That(publicKey, Does.Contain("modA"));
            Assert.That(privateKey, Does.Contain("privA"));
        }

        #endregion

        #region DecryptRsa Tests

        /// <summary>
        /// Regression test for the shared-global-key race: two encryptors initialized in sequence
        /// (as two concurrent WitRPC connections would) must each decrypt with their OWN private key.
        /// Before the fix, decryptRSA used a single browser global, so the second generateKeys()
        /// clobbered the first key and the first connection failed with an RSA OperationError.
        /// </summary>
        [Test]
        public async Task DecryptRsaUsesOwnInstanceKeyNotSharedGlobalTest()
        {
            // One runtime shared by both encryptors — the second InitAsync would overwrite a global key.
            var runtime = new RecordingJSRuntime(KeyPair("modA", "privA"), KeyPair("modB", "privB"));

            var encryptorA = new EncryptorClientWeb(runtime);
            var encryptorB = new EncryptorClientWeb(runtime);

            await encryptorA.InitAsync();
            await encryptorB.InitAsync(); // happens AFTER A — a global key would now be B's

            await encryptorA.DecryptRsa(new byte[] { 1, 2, 3 });
            var privateKeyPassedByA = (string)runtime.DecryptCalls[^1]![0]!;

            await encryptorB.DecryptRsa(new byte[] { 4, 5, 6 });
            var privateKeyPassedByB = (string)runtime.DecryptCalls[^1]![0]!;

            Assert.That(privateKeyPassedByA, Does.Contain("privA"));
            Assert.That(privateKeyPassedByA, Does.Not.Contain("privB"));
            Assert.That(privateKeyPassedByB, Does.Contain("privB"));
        }

        [Test]
        public async Task DecryptRsaPassesPrivateKeyAndDataTest()
        {
            var runtime = new RecordingJSRuntime(KeyPair("modA", "privA"));
            var encryptor = new EncryptorClientWeb(runtime);
            await encryptor.InitAsync();

            await encryptor.DecryptRsa(new byte[] { 1, 2, 3 });

            var args = runtime.DecryptCalls[^1]!;
            Assert.That(args, Has.Length.EqualTo(2));
            Assert.That((string)args[0]!, Does.Contain("privA"));
            Assert.That((string)args[1]!, Is.EqualTo(Convert.ToBase64String(new byte[] { 1, 2, 3 })));
        }

        [Test]
        public void DecryptRsaBeforeInitThrowsTest()
        {
            var runtime = new RecordingJSRuntime();
            var encryptor = new EncryptorClientWeb(runtime);

            Assert.That(async () => await encryptor.DecryptRsa(new byte[] { 1 }),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region Dispose Tests

        [Test]
        public async Task DisposeClearsKeysAndAllowsReinitTest()
        {
            var runtime = new RecordingJSRuntime(KeyPair("modA", "privA"), KeyPair("modB", "privB"));
            var encryptor = new EncryptorClientWeb(runtime);

            await encryptor.InitAsync();
            encryptor.Dispose();

            Assert.That(encryptor.IsInitialized, Is.False);

            // A disposed encryptor must not keep the old private key around for decryption.
            Assert.That(async () => await encryptor.DecryptRsa(new byte[] { 1 }),
                Throws.InstanceOf<InvalidOperationException>());

            // Re-init pulls the next keypair.
            await encryptor.InitAsync();
            await encryptor.DecryptRsa(new byte[] { 1 });
            Assert.That((string)runtime.DecryptCalls[^1]![0]!, Does.Contain("privB"));
        }

        #endregion
    }
}

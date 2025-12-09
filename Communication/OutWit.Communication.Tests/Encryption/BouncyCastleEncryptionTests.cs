using System;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Client.Encryption.BouncyCastle;
using OutWit.Communication.Server.Encryption.BouncyCastle;

namespace OutWit.Communication.Tests.Encryption
{
    [TestFixture]
    public class BouncyCastleEncryptionTests
    {
        #region Client Encryptor Tests

        [Test]
        public void ClientEncryptorGeneratesKeys()
        {
            using var encryptor = new EncryptorClientBouncyCastle();

            var publicKey = encryptor.GetPublicKey();
            var privateKey = encryptor.GetPrivateKey();

            Assert.That(publicKey, Is.Not.Null);
            Assert.That(publicKey.Length, Is.GreaterThan(0));
            Assert.That(privateKey, Is.Not.Null);
            Assert.That(privateKey.Length, Is.GreaterThan(0));
        }

        [Test]
        public void ClientEncryptorResetAes()
        {
            using var encryptor = new EncryptorClientBouncyCastle();

            var key = new byte[32]; // 256 bits
            var iv = new byte[16];  // 128 bits
            new Random().NextBytes(key);
            new Random().NextBytes(iv);

            var result = encryptor.ResetAes(key, iv);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ClientEncryptorEncryptsAndDecrypts()
        {
            using var encryptor = new EncryptorClientBouncyCastle();

            var key = new byte[32];
            var iv = new byte[16];
            new Random().NextBytes(key);
            new Random().NextBytes(iv);
            encryptor.ResetAes(key, iv);

            var originalData = Encoding.UTF8.GetBytes("Hello, BouncyCastle!");

            var encrypted = await encryptor.Encrypt(originalData);
            var decrypted = await encryptor.Decrypt(encrypted);

            Assert.That(decrypted, Is.EqualTo(originalData));
        }

        #endregion

        #region Server Encryptor Tests

        [Test]
        public void ServerEncryptorGeneratesSymmetricKey()
        {
            using var encryptor = new EncryptorServerBouncyCastle();

            var symmetricKey = encryptor.GetSymmetricKey();
            var vector = encryptor.GetVector();

            Assert.That(symmetricKey, Is.Not.Null);
            Assert.That(symmetricKey.Length, Is.EqualTo(32)); // 256 bits
            Assert.That(vector, Is.Not.Null);
            Assert.That(vector.Length, Is.EqualTo(16)); // 128 bits
        }

        [Test]
        public void ServerEncryptorResetGeneratesNewKeys()
        {
            using var encryptor = new EncryptorServerBouncyCastle();

            var key1 = encryptor.GetSymmetricKey();
            var iv1 = encryptor.GetVector();

            encryptor.Reset();

            var key2 = encryptor.GetSymmetricKey();
            var iv2 = encryptor.GetVector();

            Assert.That(key2, Is.Not.EqualTo(key1));
            Assert.That(iv2, Is.Not.EqualTo(iv1));
        }

        [Test]
        public async Task ServerEncryptorEncryptsAndDecrypts()
        {
            using var encryptor = new EncryptorServerBouncyCastle();

            var originalData = Encoding.UTF8.GetBytes("Hello from server!");

            var encrypted = await encryptor.Encrypt(originalData);
            var decrypted = await encryptor.Decrypt(encrypted);

            Assert.That(decrypted, Is.EqualTo(originalData));
        }

        #endregion

        #region Factory Tests

        [Test]
        public void FactoryCreatesEncryptor()
        {
            var factory = new EncryptorServerBouncyCastleFactory();

            var encryptor = factory.CreateEncryptor();

            Assert.That(encryptor, Is.Not.Null);
            Assert.That(encryptor, Is.InstanceOf<EncryptorServerBouncyCastle>());
        }

        [Test]
        public void FactoryCreatesUniqueEncryptors()
        {
            var factory = new EncryptorServerBouncyCastleFactory();

            var encryptor1 = factory.CreateEncryptor();
            var encryptor2 = factory.CreateEncryptor();

            Assert.That(encryptor1.GetSymmetricKey(), Is.Not.EqualTo(encryptor2.GetSymmetricKey()));
        }

        #endregion

        #region Client-Server Compatibility Tests

        [Test]
        public async Task ClientAndServerCanCommunicate()
        {
            using var clientEncryptor = new EncryptorClientBouncyCastle();
            using var serverEncryptor = new EncryptorServerBouncyCastle();

            // Simulate key exchange: server encrypts AES key with client's public key
            var publicKey = clientEncryptor.GetPublicKey();
            var symmetricKey = serverEncryptor.GetSymmetricKey();
            var vector = serverEncryptor.GetVector();

            // Server encrypts the symmetric key for the client
            var encryptedKey = await serverEncryptor.EncryptForClient(
                CombineKeyAndVector(symmetricKey, vector), 
                publicKey);

            // Client decrypts the symmetric key
            var decryptedKeyAndVector = await clientEncryptor.DecryptRsa(encryptedKey);
            var (decryptedKey, decryptedVector) = SplitKeyAndVector(decryptedKeyAndVector);

            // Client initializes AES with the received key
            clientEncryptor.ResetAes(decryptedKey, decryptedVector);

            // Now client and server should be able to communicate
            var originalMessage = Encoding.UTF8.GetBytes("Secret message from client!");

            // Client encrypts
            var encryptedMessage = await clientEncryptor.Encrypt(originalMessage);

            // Server decrypts
            var decryptedMessage = await serverEncryptor.Decrypt(encryptedMessage);

            Assert.That(decryptedMessage, Is.EqualTo(originalMessage));

            // Server responds
            var responseMessage = Encoding.UTF8.GetBytes("Response from server!");
            var encryptedResponse = await serverEncryptor.Encrypt(responseMessage);
            var decryptedResponse = await clientEncryptor.Decrypt(encryptedResponse);

            Assert.That(decryptedResponse, Is.EqualTo(responseMessage));
        }

        private static byte[] CombineKeyAndVector(byte[] key, byte[] vector)
        {
            var combined = new byte[key.Length + vector.Length];
            Buffer.BlockCopy(key, 0, combined, 0, key.Length);
            Buffer.BlockCopy(vector, 0, combined, key.Length, vector.Length);
            return combined;
        }

        private static (byte[] key, byte[] vector) SplitKeyAndVector(byte[] combined)
        {
            var key = new byte[32];
            var vector = new byte[16];
            Buffer.BlockCopy(combined, 0, key, 0, 32);
            Buffer.BlockCopy(combined, 32, vector, 0, 16);
            return (key, vector);
        }

        #endregion

        #region End-to-End Communication Tests

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.Json)]
        public async Task BouncyCastleEncryptionEndToEndTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(BouncyCastleEncryptionEndToEndTest)}{transportType}{serializerType}";

            var server = Shared.GetServerWithBouncyCastle(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            try
            {
                var client = Shared.GetClientWithBouncyCastle(transportType, serializerType, testName);

                Assert.That(await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.True);
                Assert.That(client.IsInitialized, Is.True);
                Assert.That(client.IsAuthorized, Is.True);

                var service = Shared.GetService(client);

                // Test basic request
                Assert.That(service.RequestData("test"), Is.EqualTo("test"));

                await client.Disconnect();
            }
            finally
            {
                server.StopWaitingForConnection();
            }
        }

        #endregion
    }
}

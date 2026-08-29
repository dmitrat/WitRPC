using System.Security.Cryptography;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Encryption
{
    /// <summary>
    /// The protocol-3 message channel: AES-256-GCM per direction with a strict
    /// counter. Tampering, replay and reordering must be rejected loudly --
    /// the exact guarantees the audit found missing in the CBC-without-MAC path.
    /// </summary>
    [TestFixture]
    public sealed class AeadCipherTests
    {
        #region Cipher Tests

        [Test]
        public void RoundTripTest()
        {
            var (send, receive) = CreatePair();
            using (send)
            using (receive)
            {
                var data = RandomNumberGenerator.GetBytes(1024);

                var frame = send.Seal(data);
                var restored = receive.Open(frame);

                Assert.That(restored, Is.EqualTo(data));
            }
        }

        [Test]
        public void SequenceOfMessagesRoundTripsTest()
        {
            var (send, receive) = CreatePair();
            using (send)
            using (receive)
            {
                for (int i = 0; i < 20; i++)
                {
                    var data = RandomNumberGenerator.GetBytes(64 + i);
                    Assert.That(receive.Open(send.Seal(data)), Is.EqualTo(data));
                }
            }
        }

        [Test]
        public void TamperedFrameIsRejectedTest()
        {
            var (send, receive) = CreatePair();
            using (send)
            using (receive)
            {
                var frame = send.Seal(RandomNumberGenerator.GetBytes(256));
                frame[^1] ^= 0x01;

                Assert.That(() => receive.Open(frame), Throws.TypeOf<WitExceptionEncryption>());
            }
        }

        [Test]
        public void ReplayedFrameIsRejectedTest()
        {
            var (send, receive) = CreatePair();
            using (send)
            using (receive)
            {
                var frame = send.Seal(RandomNumberGenerator.GetBytes(128));

                Assert.That(receive.Open(frame), Is.Not.Null);
                Assert.That(() => receive.Open(frame), Throws.TypeOf<WitExceptionEncryption>());
            }
        }

        [Test]
        public void ReorderedFramesAreRejectedTest()
        {
            var (send, receive) = CreatePair();
            using (send)
            using (receive)
            {
                var first = send.Seal(RandomNumberGenerator.GetBytes(128));
                var second = send.Seal(RandomNumberGenerator.GetBytes(128));

                Assert.That(() => receive.Open(second), Throws.TypeOf<WitExceptionEncryption>());

                // The stream is not poisoned by the attempt: the expected frame
                // still opens.
                Assert.That(receive.Open(first), Is.Not.Null);
            }
        }

        [Test]
        public void CrossDirectionFrameIsRejectedTest()
        {
            var master = RandomNumberGenerator.GetBytes(AeadCipher.KEY_SIZE);
            var salt = RandomNumberGenerator.GetBytes(AeadCipher.SALT_SIZE);
            var (clientToServer, _) = AeadCipher.DeriveKeys(master, salt);

            // The same key bytes, but the wrong direction in the associated
            // data: a frame must never authenticate across directions.
            using var send = new AeadCipher(clientToServer, AeadCipher.DIRECTION_CLIENT_TO_SERVER);
            using var wrongDirection = new AeadCipher(clientToServer, AeadCipher.DIRECTION_SERVER_TO_CLIENT);

            var frame = send.Seal(RandomNumberGenerator.GetBytes(64));

            Assert.That(() => wrongDirection.Open(frame), Throws.TypeOf<WitExceptionEncryption>());
        }

        [Test]
        public void DerivedKeysDifferPerDirectionTest()
        {
            var master = RandomNumberGenerator.GetBytes(AeadCipher.KEY_SIZE);
            var salt = RandomNumberGenerator.GetBytes(AeadCipher.SALT_SIZE);

            var (clientToServer, serverToClient) = AeadCipher.DeriveKeys(master, salt);

            Assert.That(clientToServer, Is.Not.EqualTo(serverToClient));
            Assert.That(clientToServer, Has.Length.EqualTo(AeadCipher.KEY_SIZE));
        }

        #endregion

        #region Encryptor Interop Tests

        [Test]
        public async Task GeneralClientAndServerInteropTest()
        {
            using var server = new EncryptorServerGeneral();
            using var client = new EncryptorClientGeneral();

            Assert.That(client.ResetAes(server.GetSymmetricKey(), server.GetVector()), Is.True);

            var request = RandomNumberGenerator.GetBytes(512);
            var response = RandomNumberGenerator.GetBytes(512);

            // Client to server, then server to client, twice over -- both
            // directions advance their own counters independently.
            for (int i = 0; i < 3; i++)
            {
                Assert.That(await server.Decrypt(await client.Encrypt(request)), Is.EqualTo(request));
                Assert.That(await client.Decrypt(await server.Encrypt(response)), Is.EqualTo(response));
            }
        }

        [Test]
        public async Task TamperedMessageThrowsThroughTheEncryptorTest()
        {
            using var server = new EncryptorServerGeneral();
            using var client = new EncryptorClientGeneral();

            client.ResetAes(server.GetSymmetricKey(), server.GetVector());

            var frame = await client.Encrypt(RandomNumberGenerator.GetBytes(128));
            frame[^1] ^= 0x01;

            // The old CBC path silently returned an empty array here; a
            // corrupted message must fail, not impersonate an empty one.
            Assert.That(() => server.Decrypt(frame), Throws.TypeOf<WitExceptionEncryption>());
        }

        #endregion

        #region Helpers

        private static (AeadCipher Send, AeadCipher Receive) CreatePair()
        {
            var master = RandomNumberGenerator.GetBytes(AeadCipher.KEY_SIZE);
            var salt = RandomNumberGenerator.GetBytes(AeadCipher.SALT_SIZE);

            var (clientToServer, _) = AeadCipher.DeriveKeys(master, salt);

            return (
                new AeadCipher(clientToServer, AeadCipher.DIRECTION_CLIENT_TO_SERVER),
                new AeadCipher(clientToServer, AeadCipher.DIRECTION_CLIENT_TO_SERVER));
        }

        #endregion
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Transports
{
    /// <summary>
    /// The framing safety the stream transports rely on (audit P0-3): a length is
    /// read in full before it is trusted, checked before a byte is allocated, and
    /// the payload is read to completion.
    /// </summary>
    [TestFixture]
    public class StreamFrameReaderTests
    {
        #region Constants

        private const long MAX = 1024;

        #endregion

        #region Helpers

        private static byte[] Framed(byte[] payload)
        {
            var frame = new byte[4 + payload.Length];
            BitConverter.GetBytes(payload.Length).CopyTo(frame, 0);
            payload.CopyTo(frame, 4);
            return frame;
        }

        private static byte[] Prefix(int length)
        {
            return BitConverter.GetBytes(length);
        }

        #endregion

        #region Tests

        [Test]
        public async Task ReadsAValidFrameTest()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(Framed(payload));

            byte[]? read = await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX);

            Assert.That(read, Is.EqualTo(payload));
        }

        [Test]
        public async Task ReadsTwoFramesBackToBackTest()
        {
            var first = new byte[] { 9, 8, 7 };
            var second = new byte[] { 4, 5 };

            var combined = new byte[Framed(first).Length + Framed(second).Length];
            Framed(first).CopyTo(combined, 0);
            Framed(second).CopyTo(combined, Framed(first).Length);

            using var stream = new MemoryStream(combined);
            var lengthBuffer = new byte[4];

            Assert.That(await StreamFrameReader.ReadFrameAsync(stream, lengthBuffer, MAX), Is.EqualTo(first));
            Assert.That(await StreamFrameReader.ReadFrameAsync(stream, lengthBuffer, MAX), Is.EqualTo(second));
        }

        [Test]
        public async Task CleanEndOfStreamReturnsNullTest()
        {
            using var stream = new MemoryStream(Array.Empty<byte>());

            Assert.That(await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX), Is.Null);
        }

        [Test]
        public void NegativeLengthIsRejectedTest()
        {
            using var stream = new MemoryStream(Prefix(-1));

            Assert.That(async () => await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX),
                Throws.TypeOf<WitExceptionTransport>());
        }

        [Test]
        public void ZeroLengthIsRejectedTest()
        {
            using var stream = new MemoryStream(Prefix(0));

            Assert.That(async () => await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX),
                Throws.TypeOf<WitExceptionTransport>());
        }

        [Test]
        public void OversizedLengthIsRejectedBeforeAllocationTest()
        {
            // A hostile length that would allocate ~2 GB if trusted.
            using var stream = new MemoryStream(Prefix(int.MaxValue));

            Assert.That(async () => await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX),
                Throws.TypeOf<WitExceptionTransport>());
        }

        [Test]
        public void TruncatedPayloadIsRejectedTest()
        {
            // Prefix says 10 bytes, only 3 follow.
            var bytes = new byte[4 + 3];
            Prefix(10).CopyTo(bytes, 0);

            using var stream = new MemoryStream(bytes);

            Assert.That(async () => await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX),
                Throws.TypeOf<WitExceptionTransport>());
        }

        [Test]
        public async Task PartialPrefixAtEndOfStreamIsRejectedTest()
        {
            // Only two of the four length bytes are present.
            using var stream = new MemoryStream(new byte[] { 1, 0 });

            Assert.That(async () => await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX),
                Throws.TypeOf<WitExceptionTransport>());

            await Task.CompletedTask;
        }

        [Test]
        public async Task FrameExactlyAtTheLimitIsAcceptedTest()
        {
            var payload = new byte[MAX];
            for (int i = 0; i < payload.Length; i++)
                payload[i] = (byte)(i % 251);

            using var stream = new MemoryStream(Framed(payload));

            byte[]? read = await StreamFrameReader.ReadFrameAsync(stream, new byte[4], MAX);

            Assert.That(read, Is.EqualTo(payload));
        }

        #endregion
    }
}

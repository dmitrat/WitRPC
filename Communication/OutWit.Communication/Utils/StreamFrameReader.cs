using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;

namespace OutWit.Communication.Utils
{
    /// <summary>
    /// Reads length-prefixed frames from a stream safely: the length is read in
    /// full before it is trusted, checked against a ceiling before a byte is
    /// allocated, and the payload is read to completion.
    /// <para>
    /// The 2.x transports read the four-byte length with a single
    /// <see cref="Stream.ReadAsync(byte[], int, int)"/> — which may return fewer
    /// bytes — and then allocated <c>new byte[length]</c> with no check, so a
    /// truncated prefix was misparsed and a hostile or corrupt length (huge, or
    /// negative) could exhaust memory or throw before the client was ever
    /// authorized. This closes both.
    /// </para>
    /// </summary>
    public static class StreamFrameReader
    {
        #region Constants

        /// <summary>A generous default ceiling: large enough for ordinary RPC payloads, small enough that a single hostile length cannot exhaust memory.</summary>
        public const long DEFAULT_MAX_MESSAGE_SIZE = 256L * 1024 * 1024;

        private const int LENGTH_PREFIX_SIZE = sizeof(int);

        #endregion

        #region Functions

        /// <summary>
        /// Reads one frame. Returns the payload, or <c>null</c> on a clean end of
        /// stream (the peer closed between frames).
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="lengthBuffer">A reusable four-byte buffer.</param>
        /// <param name="maxMessageSize">The largest frame allowed.</param>
        /// <param name="token">Cancels the read.</param>
        /// <returns>The frame payload, or <c>null</c> at end of stream.</returns>
        /// <exception cref="WitExceptionTransport">The length is out of range, or the frame is truncated mid-payload.</exception>
        public static async Task<byte[]?> ReadFrameAsync(Stream stream, byte[] lengthBuffer, long maxMessageSize, CancellationToken token = default)
        {
            if (!await ReadExactlyAsync(stream, lengthBuffer, 0, LENGTH_PREFIX_SIZE, token).ConfigureAwait(false))
                return null;

            int length = BitConverter.ToInt32(lengthBuffer, 0);

            if (length <= 0 || length > maxMessageSize)
                throw new WitExceptionTransport($"Invalid frame length {length}; the limit is {maxMessageSize} bytes");

            var data = new byte[length];

            if (!await ReadExactlyAsync(stream, data, 0, length, token).ConfigureAwait(false))
                throw new WitExceptionTransport($"Truncated frame: expected {length} bytes");

            return data;
        }

        /// <summary>
        /// Fills <paramref name="count"/> bytes of <paramref name="buffer"/> from
        /// <paramref name="offset"/>. Returns <c>false</c> if the stream ends before
        /// the first byte; throws if it ends partway.
        /// </summary>
        private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            int total = 0;

            while (total < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total), token).ConfigureAwait(false);
                if (read == 0)
                {
                    if (total == 0)
                        return false;

                    throw new WitExceptionTransport($"Stream ended after {total} of {count} bytes");
                }

                total += read;
            }

            return true;
        }

        #endregion
    }
}

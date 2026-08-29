using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Model;

namespace OutWit.Communication.Encryption
{
    /// <summary>
    /// One direction of the protocol-3 message channel: AES-256-GCM under a key
    /// derived for that direction alone, a monotonically increasing counter as
    /// the nonce, and the protocol version plus direction bound in as associated
    /// data. Frame layout: <c>[counter:8][tag:16][ciphertext]</c>.
    /// <para>
    /// The transports are ordered and lossless, so the receiver requires every
    /// counter to be exactly one more than the last: a replayed, dropped,
    /// reordered or tampered frame fails authentication loudly instead of
    /// decrypting into garbage. Keys are fresh per connection, which is what
    /// makes the counter-only nonce unique for the life of a key.
    /// </para>
    /// </summary>
    public sealed class AeadCipher : IDisposable
    {
        #region Constants

        public const int KEY_SIZE = 32;

        public const int SALT_SIZE = 16;

        public const int NONCE_SIZE = 12;

        public const int TAG_SIZE = 16;

        public const int COUNTER_SIZE = 8;

        public const byte DIRECTION_CLIENT_TO_SERVER = 1;

        public const byte DIRECTION_SERVER_TO_CLIENT = 2;

        private static readonly byte[] INFO_CLIENT_TO_SERVER = { (byte)'w', (byte)'i', (byte)'t', (byte)'3', (byte)'c', (byte)'2', (byte)'s' };

        private static readonly byte[] INFO_SERVER_TO_CLIENT = { (byte)'w', (byte)'i', (byte)'t', (byte)'3', (byte)'s', (byte)'2', (byte)'c' };

        #endregion

        #region Fields

        private readonly object m_lock = new();

        private readonly AesGcm m_aes;

        private readonly byte[] m_aad;

        private ulong m_counter;

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Binds one directional key to its direction; the direction and the
        /// protocol version become the associated data of every frame.
        /// </summary>
        public AeadCipher(byte[] key, byte direction)
        {
            if (key.Length != KEY_SIZE)
                throw new WitExceptionEncryption($"AEAD key must be {KEY_SIZE} bytes");

#if NET8_0_OR_GREATER
            m_aes = new AesGcm(key, TAG_SIZE);
#else
#pragma warning disable SYSLIB0053
            m_aes = new AesGcm(key);
#pragma warning restore SYSLIB0053
#endif
            m_aad = new[] { (byte)WitProtocol.VERSION, direction };
        }

        #endregion

        #region Functions

        /// <summary>
        /// Derives the two directional keys from the handshake's master key and
        /// salt. Both ends run the same derivation, so nothing but the master
        /// travels over the wire.
        /// </summary>
        public static (byte[] ClientToServer, byte[] ServerToClient) DeriveKeys(byte[] masterKey, byte[] salt)
        {
            return (
                HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KEY_SIZE, salt, INFO_CLIENT_TO_SERVER),
                HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KEY_SIZE, salt, INFO_SERVER_TO_CLIENT));
        }

        /// <summary>Encrypts one message into a framed, authenticated payload.</summary>
        public byte[] Seal(byte[] data)
        {
            lock (m_lock)
            {
                ThrowIfDisposed();

                var frame = new byte[COUNTER_SIZE + TAG_SIZE + data.Length];
                BinaryPrimitives.WriteUInt64LittleEndian(frame, m_counter);

                Span<byte> nonce = stackalloc byte[NONCE_SIZE];
                BinaryPrimitives.WriteUInt64LittleEndian(nonce, m_counter);

                m_aes.Encrypt(
                    nonce,
                    data,
                    frame.AsSpan(COUNTER_SIZE + TAG_SIZE),
                    frame.AsSpan(COUNTER_SIZE, TAG_SIZE),
                    m_aad);

                m_counter++;
                return frame;
            }
        }

        /// <summary>
        /// Authenticates and decrypts one framed payload. Throws
        /// <see cref="WitExceptionEncryption"/> on a malformed, out-of-order or
        /// tampered frame; the counter advances only after authentication.
        /// </summary>
        public byte[] Open(byte[] frame)
        {
            lock (m_lock)
            {
                ThrowIfDisposed();

                if (frame.Length < COUNTER_SIZE + TAG_SIZE)
                    throw new WitExceptionEncryption("Encrypted frame is too short");

                ulong counter = BinaryPrimitives.ReadUInt64LittleEndian(frame);
                if (counter != m_counter)
                    throw new WitExceptionEncryption($"Out-of-order encrypted frame: expected {m_counter}, received {counter}");

                Span<byte> nonce = stackalloc byte[NONCE_SIZE];
                BinaryPrimitives.WriteUInt64LittleEndian(nonce, counter);

                var data = new byte[frame.Length - COUNTER_SIZE - TAG_SIZE];

                try
                {
                    m_aes.Decrypt(
                        nonce,
                        frame.AsSpan(COUNTER_SIZE + TAG_SIZE),
                        frame.AsSpan(COUNTER_SIZE, TAG_SIZE),
                        data,
                        m_aad);
                }
                catch (CryptographicException e)
                {
                    throw new WitExceptionEncryption("Encrypted frame failed authentication", e);
                }

                m_counter++;
                return data;
            }
        }

        #endregion

        #region Tools

        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(AeadCipher));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            lock (m_lock)
            {
                if (m_disposed)
                    return;

                m_disposed = true;
                m_aes.Dispose();
            }
        }

        #endregion
    }
}

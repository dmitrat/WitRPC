using System;
using System.Threading;
using System.Threading.Channels;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Connections
{
    /// <summary>
    /// One client's connection on the server: its transport, its per-connection
    /// encryptor, its handshake state, and the two things that keep the server
    /// correct under load — a single inbound queue processed in order, and a send
    /// lock so responses and callbacks never interleave on the one transport.
    /// </summary>
    public class ConnectionInfo : IDisposable
    {
        #region Fields

        private readonly IEncryptorServerFactory m_encryptorFactory;

        private readonly object m_encryptorLock = new();

        private IEncryptorServer? m_encryptor;

        private bool m_disposed;

        #endregion

        #region Constructors

        public ConnectionInfo(ITransportServer transport, IEncryptorServerFactory encryptorFactory)
        {
            Transport = transport;
            m_encryptorFactory = encryptorFactory;

            State = ConnectionState.Connected;

            SendLock = new SemaphoreSlim(1, 1);

            // Single reader (the connection's processing loop), many writers
            // (the transport delivers each frame on its own task).
            Inbound = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        #endregion

        #region Functions

        public void Reinitialize()
        {
            if (!CanReinitialize)
                return;

            State = ConnectionState.Connected;
        }

        /// <summary>
        /// Stops accepting more inbound frames and lets the processing loop drain
        /// and exit. Idempotent.
        /// </summary>
        public void CompleteInbound()
        {
            Inbound.Writer.TryComplete();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            Inbound.Writer.TryComplete();
            SendLock.Dispose();
            m_encryptor?.Dispose();
        }

        #endregion

        #region Properties

        public ITransportServer Transport { get; }

        /// <summary>
        /// The per-connection encryptor, created on first use. Building it eagerly
        /// in the connection's constructor is what widened the window between the
        /// transport starting to read and the server subscribing to it, which
        /// dropped a fast client's first frame. It is only ever touched from the
        /// connection's own processing loop, so the lazy build is single-threaded
        /// in practice; the lock is belt and braces.
        /// </summary>
        public IEncryptorServer Encryptor
        {
            get
            {
                if (m_encryptor != null)
                    return m_encryptor;

                lock (m_encryptorLock)
                    return m_encryptor ??= m_encryptorFactory.CreateEncryptor();
            }
        }

        public ConnectionState State { get; set; }

        public bool IsInitialized => State is ConnectionState.Initialized or ConnectionState.Authorized;

        public bool IsAuthorized => State == ConnectionState.Authorized;

        public bool CanReinitialize => Transport.CanReinitialize;

        public Guid Id => Transport.Id;

        public SemaphoreSlim SendLock { get; }

        public Channel<byte[]> Inbound { get; }

        #endregion
    }

    /// <summary>
    /// The connection's place in the handshake. Requests are only served in
    /// <see cref="Authorized"/>; a message that arrives out of this order closes
    /// the connection.
    /// </summary>
    public enum ConnectionState
    {
        Connected,
        Initialized,
        Authorized
    }
}

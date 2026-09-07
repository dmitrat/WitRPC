using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Connections
{
    /// <summary>
    /// One client's connection on the server: its transport, its per-connection
    /// encryptor, its handshake state, the principal it authorized with, and the
    /// two things that keep the server correct under load — a single inbound queue
    /// processed in order, and a single outbound queue (<see cref="ConnectionOutbox"/>)
    /// so responses and callbacks leave in order and never interleave on the one
    /// transport.
    /// </summary>
    public class ConnectionInfo : IDisposable
    {
        #region Constants

        private const int INVOCATION_CACHE_CAPACITY = 64;

        private const int INVOCATION_CACHE_MAX_ENTRY_BYTES = 256 * 1024;

        #endregion

        #region Fields

        private readonly object m_invocationCacheLock = new();

        private readonly Dictionary<Guid, byte[]> m_invocationCache = new();

        private readonly Queue<Guid> m_invocationCacheOrder = new();

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
            Principal = null;
            AuthorizedAtUtc = default;
        }

        /// <summary>
        /// Gives the connection its outbound queue. Called once by the server right
        /// after the connection is created; the queue needs the server's serializer
        /// and options, which the connection does not know.
        /// </summary>
        internal void AttachOutbox(ConnectionOutbox outbox)
        {
            Outbox = outbox;
        }

        /// <summary>
        /// Returns the cached response for an invocation already executed on
        /// this connection, if it is still within the bounded window.
        /// </summary>
        public bool TryGetCachedResponse(Guid invocationId, out byte[]? response)
        {
            lock (m_invocationCacheLock)
            {
                if (m_invocationCache.TryGetValue(invocationId, out var cached))
                {
                    response = cached;
                    return true;
                }
            }

            response = null;
            return false;
        }

        /// <summary>
        /// Remembers a response for de-duplication. The window is bounded both
        /// in entries and per-entry size; a response too large to cache simply
        /// is not -- retry is restricted to idempotent methods, so re-executing
        /// a duplicate is safe, just wasteful.
        /// </summary>
        public void CacheResponse(Guid invocationId, byte[] response)
        {
            if (response.Length > INVOCATION_CACHE_MAX_ENTRY_BYTES)
                return;

            lock (m_invocationCacheLock)
            {
                if (!m_invocationCache.TryAdd(invocationId, response))
                    return;

                m_invocationCacheOrder.Enqueue(invocationId);

                while (m_invocationCache.Count > INVOCATION_CACHE_CAPACITY)
                    m_invocationCache.Remove(m_invocationCacheOrder.Dequeue());
            }
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
            Outbox?.Dispose();
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

        /// <summary>
        /// The principal established at authorization when the server's token
        /// validator implements <see cref="Authorization.IConnectionAuthenticator"/>;
        /// null otherwise, and null again after a re-initialization.
        /// </summary>
        public ClaimsPrincipal? Principal { get; internal set; }

        /// <summary>
        /// When the connection authorized; default until it has.
        /// </summary>
        public DateTimeOffset AuthorizedAtUtc { get; internal set; }

        /// <summary>
        /// Callbacks waiting in the connection's outbound queue.
        /// </summary>
        public long PendingCallbacks => Outbox?.PendingCallbacks ?? 0;

        /// <summary>
        /// Payload bytes of the callbacks waiting in the connection's outbound queue.
        /// </summary>
        public long PendingCallbackBytes => Outbox?.PendingCallbackBytes ?? 0;

        /// <summary>
        /// Taken by the outbound writer around each transport write. The writer is
        /// the only sender since 3.2; the lock stays for anyone who wrote to the
        /// transport directly.
        /// </summary>
        public SemaphoreSlim SendLock { get; }

        public Channel<byte[]> Inbound { get; }

        internal ConnectionOutbox? Outbox { get; private set; }

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

using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.MMF;

namespace OutWit.Communication.Server.MMF
{
    /// <summary>
    /// The server end of a one-to-one memory-mapped channel.
    /// <para>
    /// Creates every kernel object (the file, four events, two mutexes) and
    /// owns the <c>server_alive</c> mutex for its lifetime. A client attaches by
    /// opening the objects, taking <c>client_alive</c>, and sending a
    /// <see cref="MmfFrameFlags.Hello"/>; from that point the reader thread
    /// watches the client's mutex and a client that leaves — gracefully or by
    /// dying — is seen immediately. A departed client always gets a fresh
    /// transport from the factory, so nothing is ever reinitialised in place.
    /// </para>
    /// </summary>
    public class MemoryMappedFileServerTransport : ITransportServer
    {
        #region Constants

        private const int JOIN_TIMEOUT_MS = 2000;

        #endregion

        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Fields

        private readonly object m_syncRoot = new();

        private readonly TaskCompletionSource<bool> m_attached = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private MemoryMappedFile? m_file;

        private MmfChannel? m_channel;

        private MmfPresence? m_serverAlive;

        private Mutex? m_clientAlive;

        private MmfPeerWatch? m_clientWatch;

        private Thread? m_reader;

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the channel objects. Throws when the name is in use — a stale
        /// server, or the previous client of this name still closing its handles.
        /// </summary>
        /// <param name="options">Name and size of the channel.</param>
        /// <exception cref="WitExceptionTransport">Invalid options or the channel name is busy.</exception>
        public MemoryMappedFileServerTransport(MemoryMappedFileServerTransportOptions options)
        {
            Id = Guid.NewGuid();
            Options = options;

            InitChannel();
        }

        #endregion

        #region Initialization

        private void InitChannel()
        {
            if (string.IsNullOrEmpty(Options.Name))
                throw new WitExceptionTransport("Failed to create memory mapped file: name is empty");

            if (Options.Size < MmfChannelLayout.MIN_FILE_SIZE)
                throw new WitExceptionTransport($"Failed to create memory mapped file: size must be at least {MmfChannelLayout.MIN_FILE_SIZE} bytes");

            string name = Options.Name;
            long size = Options.Size;

            MemoryMappedViewAccessor? accessor = null;
            EventWaitHandle? clientToServerReady = null;
            EventWaitHandle? clientToServerFree = null;
            EventWaitHandle? serverToClientReady = null;
            EventWaitHandle? serverToClientFree = null;

            try
            {
                m_file = MemoryMappedFile.CreateNew(MmfChannelLayout.FileName(name), size, MemoryMappedFileAccess.ReadWrite);

                accessor = m_file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
                accessor.Write(MmfChannelLayout.FILE_OFFSET_MAGIC, MmfChannelLayout.MAGIC);
                accessor.Write(MmfChannelLayout.FILE_OFFSET_VERSION, MmfChannelLayout.LAYOUT_VERSION);
                accessor.Write(MmfChannelLayout.FILE_OFFSET_SIZE, size);
                accessor.Write(MmfChannelLayout.FILE_OFFSET_CAPACITY, MmfChannelLayout.Capacity(size));

                clientToServerReady = CreateEvent(MmfChannelLayout.ClientToServerReadyName(name), false);
                clientToServerFree = CreateEvent(MmfChannelLayout.ClientToServerFreeName(name), true);
                serverToClientReady = CreateEvent(MmfChannelLayout.ServerToClientReadyName(name), false);
                serverToClientFree = CreateEvent(MmfChannelLayout.ServerToClientFreeName(name), true);

                m_clientAlive = CreateMutex(MmfChannelLayout.ClientAliveName(name));

                m_channel = MmfChannel.ForServer(accessor, size,
                    clientToServerReady, clientToServerFree, serverToClientReady, serverToClientFree);

                // Presence created before the reader starts, so that once a client
                // can open it the whole channel is already listening.
                m_serverAlive = MmfPresence.Create(MmfChannelLayout.ServerAliveName(name));

                m_reader = new Thread(ReaderLoop)
                {
                    IsBackground = true,
                    Name = "WitRPC MMF server reader"
                };

                m_reader.Start();
            }
            catch
            {
                if (m_channel == null)
                {
                    clientToServerReady?.Dispose();
                    clientToServerFree?.Dispose();
                    serverToClientReady?.Dispose();
                    serverToClientFree?.Dispose();
                    accessor?.Dispose();
                }

                Cleanup();
                throw;
            }
        }

        /// <summary>
        /// Creates a fresh event, refusing to adopt a leftover one. A leftover
        /// carries the previous session's signal state; adopting a still-signaled
        /// ready event would wake the new reader onto a zeroed region and it would
        /// tear itself down. Refusing makes the factory retry until every old
        /// handle is closed, so each channel instance gets clean events.
        /// </summary>
        private static EventWaitHandle CreateEvent(string name, bool initialState)
        {
            var handle = new EventWaitHandle(initialState, EventResetMode.AutoReset, name, out bool createdNew);
            if (createdNew)
                return handle;

            handle.Dispose();
            throw new WitExceptionTransport($"Memory mapped file channel is busy: {name} already exists");
        }

        /// <summary>
        /// Creates a fresh seat mutex, refusing to adopt a leftover one. As with
        /// the events, a lingering handle means the previous session has not fully
        /// closed; the factory retries until it has.
        /// </summary>
        private static Mutex CreateMutex(string name)
        {
            var mutex = new Mutex(false, name, out bool createdNew);
            if (createdNew)
                return mutex;

            mutex.Dispose();
            throw new WitExceptionTransport($"Memory mapped file channel is busy: {name} already exists");
        }

        #endregion

        #region ITransport

        /// <summary>
        /// Waits for a client to attach. The reader is already running from
        /// construction, so the caller may publish the connection slot before
        /// awaiting this. Returns <c>false</c> when <paramref name="token"/> fires
        /// first or the transport is disposed.
        /// </summary>
        /// <param name="token">Cancels the wait.</param>
        /// <returns><c>true</c> once a client has said hello.</returns>
        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                return await m_attached.Task.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public async Task SendBytesAsync(byte[] data)
        {
            MmfChannel? channel;

            lock (m_syncRoot)
            {
                channel = m_disposed ? null : m_channel;
            }

            if (channel == null)
                return;

            try
            {
                await channel.SendAsync(data, MmfFrameFlags.Data).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Dispose();
            }
        }

        #endregion

        #region Functions

        private void ReaderLoop()
        {
            MmfChannel? channel = m_channel;
            Mutex? clientAlive = m_clientAlive;

            if (channel == null || clientAlive == null)
            {
                m_attached.TrySetResult(false);
                return;
            }

            var attached = false;

            try
            {
                while (true)
                {
                    MmfReceiveResult result = channel.Receive(CancellationToken.None);

                    switch (result.Kind)
                    {
                        case MmfReceiveKind.Message:
                            if (!attached)
                            {
                                if (result.Flags != MmfFrameFlags.Hello)
                                {
                                    Dispose();
                                    return;
                                }

                                // A client is present and holds the seat. Confirm
                                // the hello so the client knows it reached a live
                                // server, start watching its liveness, then unblock
                                // the accept.
                                channel.SendAsync(Array.Empty<byte>(), MmfFrameFlags.HelloAck).GetAwaiter().GetResult();
                                attached = true;
                                m_clientWatch = new MmfPeerWatch(clientAlive, Dispose);
                                m_attached.TrySetResult(true);
                                continue;
                            }

                            if (result.Flags == MmfFrameFlags.Hello)
                            {
                                // A fresh hello while already attached means the
                                // seat changed hands: the previous client is gone
                                // and a new one wants a channel. This instance is
                                // stale — tear down so the factory hands the new
                                // client a fresh transport (its own id, encryptor
                                // and authorization), never the departed one's.
                                Dispose();
                                return;
                            }

                            byte[] data = result.Data ?? Array.Empty<byte>();
                            _ = Task.Run(() => Callback(Id, data));
                            continue;

                        case MmfReceiveKind.Stopped:
                        case MmfReceiveKind.Cancelled:
                            return;

                        default:
                            Dispose();
                            return;
                    }
                }
            }
            catch (Exception)
            {
                Dispose();
            }
            finally
            {
                m_attached.TrySetResult(false);
            }
        }

        private void Cleanup()
        {
            m_channel?.Dispose();
            m_channel = null;

            m_clientAlive?.Dispose();
            m_clientAlive = null;

            m_file?.Dispose();
            m_file = null;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Tears the channel down, releases the server's presence so the client
        /// sees the disconnect, and raises <see cref="Disconnected"/> exactly once.
        /// </summary>
        public void Dispose()
        {
            Thread? reader;

            lock (m_syncRoot)
            {
                if (m_disposed)
                    return;

                m_disposed = true;
                reader = m_reader;
            }

            m_channel?.Stop();

            if (reader != null && Thread.CurrentThread != reader)
                reader.Join(JOIN_TIMEOUT_MS);

            m_clientWatch?.Dispose();
            m_clientWatch = null;

            m_serverAlive?.Dispose();
            m_serverAlive = null;

            Cleanup();

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public bool CanReinitialize { get; } = false;

        private MemoryMappedFileServerTransportOptions Options { get; }

        #endregion
    }
}

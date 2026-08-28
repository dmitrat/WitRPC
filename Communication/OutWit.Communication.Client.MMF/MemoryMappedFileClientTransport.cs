using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.MMF;

namespace OutWit.Communication.Client.MMF
{
    /// <summary>
    /// The client end of a one-to-one memory-mapped channel.
    /// <para>
    /// Connecting means opening the objects the server created, taking the
    /// <c>client_alive</c> mutex — which is the one seat on the channel — and
    /// sending a <see cref="MmfFrameFlags.Hello"/>. The reader thread watches the
    /// server's presence mutex, so a server that stops or dies is seen at once.
    /// Disconnecting releases the seat, which is what the server watches.
    /// </para>
    /// </summary>
    public class MemoryMappedFileClientTransport : ITransportClient
    {
        #region Constants

        private const int CONNECT_RETRY_DELAY_MS = 100;

        private const int HELLO_ACK_TIMEOUT_MS = 1000;

        private const int JOIN_TIMEOUT_MS = 2000;

        #endregion

        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Fields

        private readonly object m_syncRoot = new();

        private MemoryMappedFile? m_file;

        private MmfChannel? m_channel;

        private MmfPresence? m_clientAlive;

        private Mutex? m_serverAlive;

        private MmfPeerWatch? m_serverWatch;

        private TaskCompletionSource<bool>? m_helloAck;

        private Thread? m_reader;

        private bool m_connected;

        #endregion

        #region Constructors

        public MemoryMappedFileClientTransport(MemoryMappedFileClientTransportOptions options)
        {
            Id = Guid.NewGuid();
            Options = options;
            Address = options.Name;
        }

        #endregion

        #region ITransport

        /// <summary>
        /// Polls for the server's channel until it can be attached to, the
        /// timeout elapses, or the token fires. <see cref="TimeSpan.Zero"/> waits
        /// indefinitely. A channel that belongs to a different layout version is
        /// refused immediately rather than retried.
        /// </summary>
        /// <param name="timeout">Overall connect timeout; zero for none.</param>
        /// <param name="cancellationToken">Cancels the attempt.</param>
        /// <returns><c>true</c> once attached.</returns>
        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Options.Name))
                return false;

            Close();

            DateTime? deadline = timeout == TimeSpan.Zero ? null : DateTime.UtcNow + timeout;

            while (!cancellationToken.IsCancellationRequested)
            {
                MmfConnectOutcome outcome = TryOpen();

                if (outcome == MmfConnectOutcome.Fatal)
                    return false;

                if (outcome == MmfConnectOutcome.Opened)
                {
                    // A successful open may have landed on a server instance that
                    // is on its way out during a restart handoff. Only a hello
                    // acknowledged by a live server counts as connected; otherwise
                    // drop this attempt and keep polling for the fresh instance.
                    if (await SayHelloAndAwaitAckAsync().ConfigureAwait(false))
                        return true;

                    Close();
                }

                if (deadline != null && DateTime.UtcNow >= deadline.Value)
                    return false;

                try
                {
                    await Task.Delay(CONNECT_RETRY_DELAY_MS, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            return await ConnectAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ReconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Close();

            return await ConnectAsync(timeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ReconnectAsync(CancellationToken cancellationToken)
        {
            return await ReconnectAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendBytesAsync(byte[] data)
        {
            MmfChannel? channel;

            lock (m_syncRoot)
            {
                channel = m_connected ? m_channel : null;
            }


            if (channel == null)
                return;

            try
            {
                await channel.SendAsync(data, MmfFrameFlags.Data).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Close();
            }
        }

        public async Task Disconnect()
        {
            Close();

            await Task.CompletedTask;
        }

        #endregion

        #region Functions

        private MmfConnectOutcome TryOpen()
        {
            string name = Options.Name!;

            MemoryMappedFile? file = null;
            MemoryMappedViewAccessor? accessor = null;
            Mutex? serverAlive = null;
            EventWaitHandle? clientToServerReady = null;
            EventWaitHandle? clientToServerFree = null;
            EventWaitHandle? serverToClientReady = null;
            EventWaitHandle? serverToClientFree = null;
            MmfPresence? clientAlive = null;
            MmfChannel? channel = null;

            try
            {
                // The server creates its presence last, so once this opens the rest exists.
                serverAlive = Mutex.OpenExisting(MmfChannelLayout.ServerAliveName(name));

                // Claim the connection slot. Exactly one permit is posted by a
                // fresh, ready channel, so claiming it is what guarantees we attach
                // to that instance and never to a departing one still holding the
                // same named objects during a restart. No permit means no channel
                // is currently offering itself — retry.
                using (var slot = Semaphore.OpenExisting(MmfChannelLayout.SlotName(name)))
                {
                    if (!slot.WaitOne(0))
                    {
                        serverAlive.Dispose();
                        return MmfConnectOutcome.Retry;
                    }
                }

                // The slot makes the seat uncontended; take it for liveness.
                clientAlive = MmfPresence.Acquire(MmfChannelLayout.ClientAliveName(name), TimeSpan.Zero);

                file = MemoryMappedFile.OpenExisting(MmfChannelLayout.FileName(name), MemoryMappedFileRights.ReadWrite);
                accessor = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);

                int magic = accessor.ReadInt32(MmfChannelLayout.FILE_OFFSET_MAGIC);
                int version = accessor.ReadInt32(MmfChannelLayout.FILE_OFFSET_VERSION);
                long size = accessor.ReadInt64(MmfChannelLayout.FILE_OFFSET_SIZE);

                if (magic != MmfChannelLayout.MAGIC || version != MmfChannelLayout.LAYOUT_VERSION || size < MmfChannelLayout.MIN_FILE_SIZE)
                {
                    accessor.Dispose();
                    file.Dispose();
                    clientAlive.Dispose();
                    serverAlive.Dispose();

                    return MmfConnectOutcome.Fatal;
                }

                clientToServerReady = EventWaitHandle.OpenExisting(MmfChannelLayout.ClientToServerReadyName(name));
                clientToServerFree = EventWaitHandle.OpenExisting(MmfChannelLayout.ClientToServerFreeName(name));
                serverToClientReady = EventWaitHandle.OpenExisting(MmfChannelLayout.ServerToClientReadyName(name));
                serverToClientFree = EventWaitHandle.OpenExisting(MmfChannelLayout.ServerToClientFreeName(name));

                channel = MmfChannel.ForClient(accessor, size,
                    clientToServerReady, clientToServerFree, serverToClientReady, serverToClientFree);

                lock (m_syncRoot)
                {
                    m_file = file;
                    m_channel = channel;
                    m_clientAlive = clientAlive;
                    m_serverAlive = serverAlive;
                    m_helloAck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    m_connected = true;

                    m_reader = new Thread(ReaderLoop)
                    {
                        IsBackground = true,
                        Name = "WitRPC MMF client reader"
                    };

                    m_reader.Start();

                    // Liveness is watched off the frame path, so a server that
                    // stops or dies is seen without the reader waiting on a mutex.
                    m_serverWatch = new MmfPeerWatch(serverAlive, Close);
                }

                return MmfConnectOutcome.Opened;
            }
            catch (Exception)
            {
                if (channel != null)
                {
                    channel.Dispose();
                }
                else
                {
                    clientToServerReady?.Dispose();
                    clientToServerFree?.Dispose();
                    serverToClientReady?.Dispose();
                    serverToClientFree?.Dispose();
                    accessor?.Dispose();
                }

                clientAlive?.Dispose();
                serverAlive?.Dispose();
                file?.Dispose();

                return MmfConnectOutcome.Retry;
            }
        }

        private async Task<bool> SayHelloAndAwaitAckAsync()
        {
            MmfChannel? channel;
            TaskCompletionSource<bool>? helloAck;

            lock (m_syncRoot)
            {
                channel = m_channel;
                helloAck = m_helloAck;
            }

            if (channel == null || helloAck == null)
                return false;

            try
            {
                await channel.SendAsync(Array.Empty<byte>(), MmfFrameFlags.Hello).ConfigureAwait(false);

                return await helloAck.Task.WaitAsync(TimeSpan.FromMilliseconds(HELLO_ACK_TIMEOUT_MS)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ReaderLoop()
        {
            MmfChannel? channel;

            lock (m_syncRoot)
            {
                channel = m_channel;
            }

            if (channel == null)
                return;

            try
            {
                while (true)
                {
                    MmfReceiveResult result = channel.Receive(CancellationToken.None);

                    switch (result.Kind)
                    {
                        case MmfReceiveKind.Message:
                            if (result.Flags == MmfFrameFlags.HelloAck)
                            {
                                m_helloAck?.TrySetResult(true);
                                continue;
                            }

                            if (result.Flags != MmfFrameFlags.Data)
                                continue;

                            byte[] data = result.Data ?? Array.Empty<byte>();
                            _ = Task.Run(() => Callback(Id, data));
                            continue;

                        case MmfReceiveKind.Stopped:
                        case MmfReceiveKind.Cancelled:
                            return;

                        default:
                            Close();
                            return;
                    }
                }
            }
            catch (Exception)
            {
                Close();
            }
        }

        /// <summary>
        /// Leaves the channel: releases the seat (which the server watches),
        /// closes every handle, and raises <see cref="Disconnected"/> once per
        /// connection. A later <see cref="ConnectAsync(TimeSpan, CancellationToken)"/> starts afresh.
        /// </summary>
        private void Close()
        {
            MemoryMappedFile? file;
            MmfChannel? channel;
            MmfPresence? clientAlive;
            Mutex? serverAlive;
            MmfPeerWatch? serverWatch;
            TaskCompletionSource<bool>? helloAck;
            Thread? reader;
            bool wasConnected;

            lock (m_syncRoot)
            {
                wasConnected = m_connected;

                file = m_file;
                channel = m_channel;
                clientAlive = m_clientAlive;
                serverAlive = m_serverAlive;
                serverWatch = m_serverWatch;
                helloAck = m_helloAck;
                reader = m_reader;

                m_file = null;
                m_channel = null;
                m_clientAlive = null;
                m_serverAlive = null;
                m_serverWatch = null;
                m_helloAck = null;
                m_reader = null;
                m_connected = false;
            }

            helloAck?.TrySetResult(false);

            channel?.Stop();

            if (reader != null && Thread.CurrentThread != reader)
                reader.Join(JOIN_TIMEOUT_MS);

            serverWatch?.Dispose();
            clientAlive?.Dispose();
            channel?.Dispose();
            serverAlive?.Dispose();
            file?.Dispose();

            if (wasConnected)
                Disconnected(Id);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public string? Address { get; }

        private string Tag => Id.ToString().Substring(0, 4);

        private MemoryMappedFileClientTransportOptions Options { get; }

        #endregion

        #region Nested Types

        private enum MmfConnectOutcome
        {
            Opened,
            Retry,
            Fatal
        }

        #endregion
    }
}

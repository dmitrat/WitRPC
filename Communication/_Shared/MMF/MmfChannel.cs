using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;

namespace OutWit.Communication.MMF
{
    /// <summary>
    /// One direction-agnostic end of a memory-mapped channel: writes frames into
    /// the outbound region and assembles messages from the inbound one.
    /// <para>
    /// Each region is a single slot guarded by two auto-reset events. The writer
    /// waits <c>free</c>, writes a chunk, sets <c>ready</c>; the reader waits
    /// <c>ready</c>, copies the chunk out, sets <c>free</c>. A chunk can never be
    /// overwritten before it is read and a signal can never be lost, which is
    /// exactly what the 2.x single-slot design could not promise. Throughput is
    /// one chunk per round-trip — microseconds on a local machine.
    /// </para>
    /// </summary>
    internal sealed class MmfChannel : IDisposable
    {
        #region Fields

        private readonly MemoryMappedViewAccessor m_accessor;

        private readonly long m_outboundOffset;

        private readonly long m_inboundOffset;

        private readonly int m_capacity;

        private readonly EventWaitHandle m_outboundReady;

        private readonly EventWaitHandle m_outboundFree;

        private readonly EventWaitHandle m_inboundReady;

        private readonly EventWaitHandle m_inboundFree;

        private readonly SemaphoreSlim m_sendLock = new(1, 1);

        private readonly ManualResetEvent m_stopHandle = new(false);

        private readonly CancellationTokenSource m_stop = new();

        private bool m_disposed;

        #endregion

        #region Constructors

        private MmfChannel(MemoryMappedViewAccessor accessor, int capacity, long outboundOffset, long inboundOffset,
            EventWaitHandle outboundReady, EventWaitHandle outboundFree, EventWaitHandle inboundReady, EventWaitHandle inboundFree)
        {
            m_accessor = accessor;
            m_capacity = capacity;
            m_outboundOffset = outboundOffset;
            m_inboundOffset = inboundOffset;
            m_outboundReady = outboundReady;
            m_outboundFree = outboundFree;
            m_inboundReady = inboundReady;
            m_inboundFree = inboundFree;
        }

        #endregion

        #region Factories

        /// <summary>The server end: writes server→client, reads client→server.</summary>
        public static MmfChannel ForServer(MemoryMappedViewAccessor accessor, long fileSize,
            EventWaitHandle clientToServerReady, EventWaitHandle clientToServerFree,
            EventWaitHandle serverToClientReady, EventWaitHandle serverToClientFree)
        {
            return new MmfChannel(accessor, MmfChannelLayout.Capacity(fileSize),
                MmfChannelLayout.ServerToClientOffset(fileSize), MmfChannelLayout.ClientToServerOffset(fileSize),
                serverToClientReady, serverToClientFree, clientToServerReady, clientToServerFree);
        }

        /// <summary>The client end: writes client→server, reads server→client.</summary>
        public static MmfChannel ForClient(MemoryMappedViewAccessor accessor, long fileSize,
            EventWaitHandle clientToServerReady, EventWaitHandle clientToServerFree,
            EventWaitHandle serverToClientReady, EventWaitHandle serverToClientFree)
        {
            return new MmfChannel(accessor, MmfChannelLayout.Capacity(fileSize),
                MmfChannelLayout.ClientToServerOffset(fileSize), MmfChannelLayout.ServerToClientOffset(fileSize),
                clientToServerReady, clientToServerFree, serverToClientReady, serverToClientFree);
        }

        #endregion

        #region Send

        /// <summary>
        /// Writes one message, chunked to the region capacity. Concurrent callers
        /// are serialised; each waits for the peer to consume the previous chunk.
        /// </summary>
        /// <param name="data">The message; may be empty.</param>
        /// <param name="flags">What the message is.</param>
        /// <exception cref="WitExceptionTransport">The channel was stopped before the message was fully written.</exception>
        public async Task SendAsync(byte[] data, MmfFrameFlags flags)
        {
            try
            {
                await m_sendLock.WaitAsync(m_stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new WitExceptionTransport("Memory mapped file channel is closed");
            }

            try
            {
                int total = data.Length;
                int offset = 0;

                do
                {
                    int chunk = Math.Min(m_capacity, total - offset);

                    // Wait for the peer to free the slot. A synchronous WaitAny on
                    // the free and stop handles, not a thread-pool wait
                    // registration: the peer consumes a slot in microseconds on a
                    // local machine, and registering hundreds of pooled waits per
                    // burst starves the pool and stalls later sends.
                    if (WaitHandle.WaitAny(new WaitHandle[] { m_outboundFree, m_stopHandle }) == 1)
                        throw new WitExceptionTransport("Memory mapped file channel is closed");

                    m_accessor.Write(m_outboundOffset + MmfChannelLayout.FRAME_OFFSET_CHUNK_LENGTH, chunk);
                    m_accessor.Write(m_outboundOffset + MmfChannelLayout.FRAME_OFFSET_TOTAL_LENGTH, total);
                    m_accessor.Write(m_outboundOffset + MmfChannelLayout.FRAME_OFFSET_CHUNK_OFFSET, offset);
                    m_accessor.Write(m_outboundOffset + MmfChannelLayout.FRAME_OFFSET_FLAGS, (int)flags);

                    if (chunk > 0)
                        m_accessor.WriteArray(m_outboundOffset + MmfChannelLayout.FRAME_HEADER_SIZE, data, offset, chunk);

                    m_outboundReady.Set();

                    offset += chunk;

                } while (offset < total);
            }
            finally
            {
                m_sendLock.Release();
            }
        }

        #endregion

        #region Receive

        /// <summary>
        /// Blocks until one complete message has been assembled, the channel is
        /// stopped, or the token fires. Meant for a dedicated reader thread.
        /// <para>
        /// The wait set is only the inbound-ready event, the stop handle and the
        /// token — never a presence mutex. Peer liveness is watched on its own
        /// thread (<see cref="MmfPeerWatch"/>) so that a probe of the peer's
        /// presence object can never perturb frame delivery.
        /// </para>
        /// </summary>
        /// <param name="token">Cancels the wait.</param>
        /// <returns>What happened.</returns>
        public MmfReceiveResult Receive(CancellationToken token)
        {
            WaitHandle[] handles = token.CanBeCanceled
                ? new WaitHandle[] { m_inboundReady, m_stopHandle, token.WaitHandle }
                : new WaitHandle[] { m_inboundReady, m_stopHandle };

            byte[]? buffer = null;
            int received = 0;
            int total = 0;
            var flags = MmfFrameFlags.Data;

            while (true)
            {
                int index = WaitHandle.WaitAny(handles);

                if (index == 1)
                    return MmfReceiveResult.Stopped();

                if (token.CanBeCanceled && index == 2)
                    return MmfReceiveResult.Cancelled();

                int chunk = m_accessor.ReadInt32(m_inboundOffset + MmfChannelLayout.FRAME_OFFSET_CHUNK_LENGTH);
                int totalLength = m_accessor.ReadInt32(m_inboundOffset + MmfChannelLayout.FRAME_OFFSET_TOTAL_LENGTH);
                int chunkOffset = m_accessor.ReadInt32(m_inboundOffset + MmfChannelLayout.FRAME_OFFSET_CHUNK_OFFSET);
                int rawFlags = m_accessor.ReadInt32(m_inboundOffset + MmfChannelLayout.FRAME_OFFSET_FLAGS);

                if (chunk < 0 || chunk > m_capacity || totalLength < 0 || chunkOffset < 0 || chunkOffset + chunk > totalLength)
                    return MmfReceiveResult.Corrupt($"Invalid frame header: chunk {chunk}, total {totalLength}, offset {chunkOffset}");

                if (buffer == null)
                {
                    if (chunkOffset != 0)
                        return MmfReceiveResult.Corrupt($"Message starts at offset {chunkOffset}");

                    total = totalLength;
                    buffer = new byte[total];
                    flags = (MmfFrameFlags)rawFlags;
                }
                else if (chunkOffset != received || totalLength != total)
                {
                    return MmfReceiveResult.Corrupt($"Chunk out of sequence: expected offset {received} of {total}, got {chunkOffset} of {totalLength}");
                }

                if (chunk > 0)
                    m_accessor.ReadArray(m_inboundOffset + MmfChannelLayout.FRAME_HEADER_SIZE, buffer, received, chunk);

                received += chunk;

                m_inboundFree.Set();

                if (received >= total)
                    return MmfReceiveResult.Message(buffer, flags);
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Wakes every pending <see cref="Receive"/> and <see cref="SendAsync"/>
        /// and makes further calls fail. Safe to call more than once and from any thread.
        /// </summary>
        public void Stop()
        {
            if (m_stop.IsCancellationRequested)
                return;

            try
            {
                m_stop.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                m_stopHandle.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Stops the channel and disposes the accessor and the four events.
        /// The caller is expected to have stopped its reader thread first.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            Stop();

            m_outboundReady.Dispose();
            m_outboundFree.Dispose();
            m_inboundReady.Dispose();
            m_inboundFree.Dispose();
            m_accessor.Dispose();

            m_stopHandle.Dispose();
            m_stop.Dispose();
            m_sendLock.Dispose();
        }

        #endregion

        #region Properties

        public int Capacity => m_capacity;

        #endregion
    }
}

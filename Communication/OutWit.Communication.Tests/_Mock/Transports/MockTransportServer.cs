using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Tests.Mock.Transports
{
    /// <summary>
    /// A server-side transport under the test's control: frames the server writes are
    /// recorded in order, a write can be made to block until the test releases it, and the
    /// test raises inbound frames and the disconnect itself.
    /// </summary>
    public sealed class MockTransportServer : ITransportServer
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Fields

        private readonly ConcurrentQueue<byte[]> m_sent = new();

        private volatile TaskCompletionSource<bool>? m_block;

        private int m_writtenFrames;

        private int m_blockedWrites;

        private int m_disconnectedRaised;

        #endregion

        #region Constructors

        public MockTransportServer()
        {
            Id = Guid.NewGuid();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Delivers one inbound frame the way the real transport would.
        /// </summary>
        public void RaiseDataReceived(byte[] data)
        {
            Callback(Id, data);
        }

        /// <summary>
        /// Makes every following write wait until <see cref="Release"/>.
        /// </summary>
        public void Block()
        {
            m_block = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Lets the blocked writes through and stops blocking new ones.
        /// </summary>
        public void Release()
        {
            var block = Interlocked.Exchange(ref m_block, null);
            block?.TrySetResult(true);
        }

        /// <summary>
        /// Waits until <paramref name="count"/> writes have hit the block.
        /// </summary>
        public bool WaitForBlockedWrites(int count, TimeSpan timeout)
        {
            return SpinWait.SpinUntil(() => BlockedWrites >= count, timeout);
        }

        private void RaiseDisconnected()
        {
            if (Interlocked.Exchange(ref m_disconnectedRaised, 1) == 0)
                Disconnected(Id);
        }

        #endregion

        #region ITransportServer

        public Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            return Task.FromResult(true);
        }

        public async Task SendBytesAsync(byte[] data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(MockTransportServer));

            var block = m_block;
            if (block != null)
            {
                Interlocked.Increment(ref m_blockedWrites);
                await block.Task.ConfigureAwait(false);

                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(MockTransportServer));
            }

            m_sent.Enqueue(data);
            Interlocked.Increment(ref m_writtenFrames);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Release();
            RaiseDisconnected();
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public bool CanReinitialize => false;

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The frames written to the transport, in write order.
        /// </summary>
        public ConcurrentQueue<byte[]> Sent => m_sent;

        /// <summary>
        /// Frames written so far.
        /// </summary>
        public int WrittenFrames => Volatile.Read(ref m_writtenFrames);

        /// <summary>
        /// Writes that hit the block and waited.
        /// </summary>
        public int BlockedWrites => Volatile.Read(ref m_blockedWrites);

        #endregion
    }
}

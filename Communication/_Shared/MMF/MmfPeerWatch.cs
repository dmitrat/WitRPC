using System;
using System.Threading;

namespace OutWit.Communication.MMF
{
    /// <summary>
    /// Watches the other end's presence mutex on a thread of its own and calls
    /// back once, when that end is gone — released gracefully or abandoned by a
    /// dead thread or process.
    /// <para>
    /// Kept off the frame-reading path on purpose. The reader waits only on the
    /// inbound-ready and stop events; putting a presence mutex in that same wait
    /// let a second client's <c>WaitOne(0)</c> probe of the seat perturb the
    /// server's wait and drop a frame. Here the mutex has its own thread and
    /// touches nothing else.
    /// </para>
    /// </summary>
    internal sealed class MmfPeerWatch : IDisposable
    {
        #region Constants

        private const int JOIN_TIMEOUT_MS = 2000;

        #endregion

        #region Fields

        private readonly Mutex m_peer;

        private readonly Action m_onGone;

        private readonly ManualResetEvent m_stop = new(false);

        private readonly Thread m_thread;

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Starts watching immediately.
        /// </summary>
        /// <param name="peer">The other end's presence mutex, opened but not owned by this side.</param>
        /// <param name="onGone">Invoked once when the peer is gone. Not invoked on <see cref="Dispose"/>.</param>
        public MmfPeerWatch(Mutex peer, Action onGone)
        {
            m_peer = peer;
            m_onGone = onGone;

            m_thread = new Thread(Watch)
            {
                IsBackground = true,
                Name = "WitRPC MMF peer watch"
            };

            m_thread.Start();
        }

        #endregion

        #region Functions

        private void Watch()
        {
            var acquired = false;
            var gone = false;

            try
            {
                int index = WaitHandle.WaitAny(new WaitHandle[] { m_peer, m_stop });
                if (index == 0)
                {
                    // The peer released its presence: the wait acquired the mutex.
                    acquired = true;
                    gone = true;
                }
            }
            catch (AbandonedMutexException)
            {
                // The peer's thread or process died still owning the mutex.
                acquired = true;
                gone = true;
            }
            catch (Exception)
            {
                gone = true;
            }
            finally
            {
                if (acquired)
                    m_peer.ReleaseQuietly();
            }

            if (gone)
                m_onGone();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            m_stop.Set();

            if (Thread.CurrentThread != m_thread)
                m_thread.Join(JOIN_TIMEOUT_MS);

            m_stop.Dispose();
        }

        #endregion
    }
}

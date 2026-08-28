using System;
using System.Threading;
using OutWit.Communication.Exceptions;

namespace OutWit.Communication.MMF
{
    /// <summary>
    /// Holds a named mutex for the lifetime of a transport, on a thread of its
    /// own, so that the peer can learn that this end is gone without heartbeats.
    /// <para>
    /// A mutex is abandoned by the kernel when its owning thread terminates —
    /// including when the whole process dies. The peer keeps the mutex in its
    /// wait set; a graceful <see cref="Dispose"/> releases it, a crash abandons
    /// it, and either way the peer's wait returns at once. The holder thread is
    /// a background thread precisely so that process exit abandons the mutex
    /// rather than waiting for it.
    /// </para>
    /// </summary>
    internal sealed class MmfPresence : IDisposable
    {
        #region Constants

        private const int JOIN_TIMEOUT_MS = 2000;

        #endregion

        #region Fields

        private readonly ManualResetEventSlim m_settled = new(false);

        private readonly ManualResetEvent m_stop = new(false);

        private Thread? m_thread;

        private Exception? m_error;

        private bool m_owned;

        private bool m_disposed;

        #endregion

        #region Constructors

        private MmfPresence()
        {
        }

        #endregion

        #region Factories

        /// <summary>
        /// Creates the named mutex and takes ownership. Fails when the name
        /// already exists — that is a stale owner, and taking over silently would
        /// hide it.
        /// </summary>
        /// <param name="name">Kernel object name.</param>
        /// <returns>The presence, already owned.</returns>
        /// <exception cref="WitExceptionTransport">The name exists or could not be acquired.</exception>
        public static MmfPresence Create(string name)
        {
            return Start(name, () =>
            {
                var mutex = new Mutex(false, name, out bool createdNew);
                if (createdNew)
                    return mutex;

                mutex.Dispose();
                throw new WitExceptionTransport($"Memory mapped file channel is busy: {name} already exists");
            }, TimeSpan.Zero);
        }

        /// <summary>
        /// Opens an existing named mutex and acquires it within the timeout.
        /// </summary>
        /// <param name="name">Kernel object name.</param>
        /// <param name="timeout">How long to wait for the current owner, if any.</param>
        /// <returns>The presence, owned.</returns>
        /// <exception cref="WitExceptionTransport">The mutex does not exist or is held by someone else.</exception>
        public static MmfPresence Acquire(string name, TimeSpan timeout)
        {
            return Start(name, () => Mutex.OpenExisting(name), timeout);
        }

        private static MmfPresence Start(string name, Func<Mutex> open, TimeSpan timeout)
        {
            var presence = new MmfPresence();

            presence.m_thread = new Thread(() => presence.Hold(name, open, timeout))
            {
                IsBackground = true,
                Name = "WitRPC MMF presence"
            };

            presence.m_thread.Start();
            presence.m_settled.Wait();

            if (presence.m_error == null)
                return presence;

            presence.Dispose();

            throw presence.m_error as WitExceptionTransport
                  ?? new WitExceptionTransport($"Failed to acquire presence mutex {name}: {presence.m_error.Message}");
        }

        #endregion

        #region Functions

        private void Hold(string name, Func<Mutex> open, TimeSpan timeout)
        {
            Mutex? mutex = null;

            try
            {
                mutex = open();

                try
                {
                    m_owned = mutex.WaitOne(timeout);
                }
                catch (AbandonedMutexException)
                {
                    m_owned = true;
                }

                if (!m_owned)
                    throw new WitExceptionTransport($"Memory mapped file channel is busy: {name} is held by another process");
            }
            catch (Exception e)
            {
                m_error = e;
            }
            finally
            {
                m_settled.Set();
            }

            if (!m_owned)
            {
                mutex?.Dispose();
                return;
            }

            m_stop.WaitOne();

            mutex.ReleaseQuietly();
            mutex?.Dispose();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            m_stop.Set();

            if (m_thread != null && Thread.CurrentThread != m_thread)
                m_thread.Join(JOIN_TIMEOUT_MS);

            m_stop.Dispose();
            m_settled.Dispose();
        }

        #endregion
    }
}

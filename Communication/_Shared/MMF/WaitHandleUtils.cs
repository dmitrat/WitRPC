using System;
using System.Threading;

namespace OutWit.Communication.MMF
{
    /// <summary>
    /// Small helpers for the kernel wait handles the MMF transport uses.
    /// </summary>
    internal static class WaitHandleUtils
    {
        #region Functions

        /// <summary>
        /// Releases a mutex the calling thread may or may not own, swallowing the
        /// exception thrown when it does not. Used after <see cref="WaitHandle.WaitAny(WaitHandle[])"/>
        /// hands ownership of a peer's presence mutex to the waiting thread.
        /// </summary>
        /// <param name="mutex">The mutex to release; <c>null</c> is ignored.</param>
        public static void ReleaseQuietly(this Mutex? mutex)
        {
            if (mutex == null)
                return;

            try
            {
                mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion
    }
}

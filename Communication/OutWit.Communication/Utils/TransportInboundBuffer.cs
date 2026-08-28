using System;
using System.Collections.Generic;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Utils
{
    /// <summary>
    /// Backs a server transport's <c>Callback</c> event so that frames read
    /// before anyone has subscribed are held, not dropped, and delivered in order
    /// the moment a subscriber attaches.
    /// <para>
    /// A server transport starts reading as soon as its connection is accepted,
    /// which is before the factory raises <c>NewClientConnected</c> and the server
    /// subscribes. A fast client's first frame — its initialization — could reach
    /// a transport whose <c>Callback</c> was still the empty default and vanish,
    /// leaving the client waiting forever. Buffering closes that window without a
    /// handshake or a change to the transport contract.
    /// </para>
    /// </summary>
    public sealed class TransportInboundBuffer
    {
        #region Fields

        private readonly Guid m_id;

        private readonly object m_lock = new();

        private readonly Queue<byte[]> m_pending = new();

        private TransportDataEventHandler? m_handler;

        #endregion

        #region Constructors

        public TransportInboundBuffer(Guid id)
        {
            m_id = id;
        }

        #endregion

        #region Functions

        /// <summary>Adds a subscriber and hands it every frame buffered so far, in order.</summary>
        public void Subscribe(TransportDataEventHandler handler)
        {
            byte[][] flush;

            lock (m_lock)
            {
                m_handler = (TransportDataEventHandler?)Delegate.Combine(m_handler, handler);

                flush = m_pending.Count > 0 ? m_pending.ToArray() : Array.Empty<byte[]>();
                m_pending.Clear();
            }

            foreach (var data in flush)
                handler(m_id, data);
        }

        public void Unsubscribe(TransportDataEventHandler handler)
        {
            lock (m_lock)
                m_handler = (TransportDataEventHandler?)Delegate.Remove(m_handler, handler);
        }

        /// <summary>Delivers a frame to the subscriber, or buffers it if none has attached yet.</summary>
        public void Raise(byte[] data)
        {
            TransportDataEventHandler? handler;

            lock (m_lock)
            {
                if (m_handler == null)
                {
                    m_pending.Enqueue(data);
                    return;
                }

                handler = m_handler;
            }

            handler(m_id, data);
        }

        #endregion
    }
}

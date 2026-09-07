using OutWit.Communication.Interfaces;

namespace OutWit.Communication.LoadTests.Transports
{
    /// <summary>
    /// Wraps a real server transport and makes its writes slow: after the handshake replies, each
    /// write waits <see cref="Delay"/> before it goes to the wire, or forever while
    /// <see cref="Stalled"/>. What the server sees is exactly a client whose socket does not drain.
    /// Inbound frames are forwarded from the inner transport only once the server has subscribed,
    /// so the inner transport's first-frame buffer flushes to a listener that exists.
    /// </summary>
    public sealed class ThrottlingTransportServer : ITransportServer
    {
        #region Constants

        /// <summary>
        /// The initialization and authorization replies and the response to the node's Attach go
        /// through untouched; the throttle starts with the first task callback.
        /// </summary>
        private const int HANDSHAKE_WRITES = 3;

        #endregion

        #region Events

        private TransportDataEventHandler? m_callback;

        public event TransportDataEventHandler Callback
        {
            add
            {
                var first = m_callback == null;
                m_callback += value;

                if (first)
                    m_inner.Callback += OnInnerData;
            }
            remove => m_callback -= value;
        }

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Fields

        private readonly ITransportServer m_inner;

        private readonly TaskCompletionSource<bool> m_stall = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int m_writes;

        #endregion

        #region Constructors

        public ThrottlingTransportServer(ITransportServer inner, TimeSpan delay, bool stalled)
        {
            m_inner = inner;
            Delay = delay;
            Stalled = stalled;

            m_inner.Disconnected += _ =>
            {
                DisconnectedAt ??= DateTime.UtcNow;
                Disconnected(Id);
            };
        }

        #endregion

        #region Functions

        /// <summary>
        /// Ends a stall; the writes waiting on it go through.
        /// </summary>
        public void Unstall()
        {
            Stalled = false;
            m_stall.TrySetResult(true);
        }

        private void OnInnerData(Guid sender, byte[] data)
        {
            m_callback?.Invoke(Id, data);
        }

        #endregion

        #region ITransportServer

        public Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            return m_inner.InitializeConnectionAsync(token);
        }

        public async Task SendBytesAsync(byte[] data)
        {
            var write = Interlocked.Increment(ref m_writes);

            if (write > HANDSHAKE_WRITES)
            {
                if (Stalled)
                    await m_stall.Task.ConfigureAwait(false);
                else if (Delay > TimeSpan.Zero)
                    await Task.Delay(Delay).ConfigureAwait(false);
            }

            await m_inner.SendBytesAsync(data).ConfigureAwait(false);
        }

        public void Dispose()
        {
            DisconnectedAt ??= DateTime.UtcNow;
            m_stall.TrySetResult(true);
            m_inner.Dispose();
        }

        #endregion

        #region Properties

        public Guid Id => m_inner.Id;

        public bool CanReinitialize => m_inner.CanReinitialize;

        public TimeSpan Delay { get; }

        public bool Stalled { get; private set; }

        /// <summary>
        /// When the connection went away (closed by the server's policy or by the client), or null.
        /// </summary>
        public DateTime? DisconnectedAt { get; private set; }

        #endregion
    }
}

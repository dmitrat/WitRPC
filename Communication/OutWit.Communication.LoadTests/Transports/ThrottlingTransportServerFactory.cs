using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.LoadTests.Transports
{
    /// <summary>
    /// Wraps a real transport factory and hands the server a <see cref="ThrottlingTransportServer"/>
    /// for the connections the harness wants slow (chosen by arrival order), a pass-through for
    /// the rest. The server's code path is the real one; only the socket "drains" slowly.
    /// </summary>
    public sealed class ThrottlingTransportServerFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        #endregion

        #region Fields

        private readonly ITransportServerFactory m_inner;

        private readonly Func<int, (TimeSpan Delay, bool Stalled)?> m_throttleFor;

        private int m_arrivals;

        #endregion

        #region Constructors

        /// <param name="inner">The real factory.</param>
        /// <param name="throttleFor">Given the arrival index of a connection, the throttle to apply, or null for none.</param>
        public ThrottlingTransportServerFactory(ITransportServerFactory inner, Func<int, (TimeSpan Delay, bool Stalled)?> throttleFor)
        {
            m_inner = inner;
            m_throttleFor = throttleFor;
            m_inner.NewClientConnected += OnNewClientConnected;
        }

        #endregion

        #region Event Handlers

        private void OnNewClientConnected(ITransportServer transport)
        {
            var index = Interlocked.Increment(ref m_arrivals) - 1;
            var throttle = m_throttleFor(index);

            if (throttle == null)
            {
                NewClientConnected(transport);
                return;
            }

            var throttled = new ThrottlingTransportServer(transport, throttle.Value.Delay, throttle.Value.Stalled);
            Throttled[throttled.Id] = throttled;
            NewClientConnected(throttled);
        }

        #endregion

        #region ITransportServerFactory

        public void StartWaitingForConnection(ILogger? logger)
        {
            m_inner.StartWaitingForConnection(logger);
        }

        public void StopWaitingForConnection()
        {
            m_inner.StopWaitingForConnection();
        }

        public void Dispose()
        {
            m_inner.Dispose();
        }

        #endregion

        #region Properties

        public IServerOptions Options => m_inner.Options;

        /// <summary>
        /// The throttled transports by connection id.
        /// </summary>
        public System.Collections.Concurrent.ConcurrentDictionary<Guid, ThrottlingTransportServer> Throttled { get; } = new();

        #endregion
    }
}

using System;
using System.Collections.Concurrent;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, ITransportServer> m_connections = new ();

        #endregion

        #region Constructors

        public NamedPipeServerTransportFactory(NamedPipeServerTransportOptions options)
        {
            Options = options;
            WaitForConnectionSlot = new AutoResetEvent(true);
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection()
        {
            CancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!CancellationTokenSource.Token.IsCancellationRequested)
                {
                    WaitForConnectionSlot.WaitOne();

                    var transport = new NamedPipeServerTransport(Options);

                    if (await transport.InitializeConnectionAsync(CancellationTokenSource.Token))
                    {
                        m_connections.TryAdd(transport.Id, transport);

                        transport.Disconnected += OnTransportDisconnected;

                        NewClientConnected(transport);
                    }

                    if (m_connections.Count >= Options.MaxNumberOfClients)
                        WaitForConnectionSlot.Reset();
                    else
                        WaitForConnectionSlot.Set();
                    
                }
            });
        }

        public void StopWaitingForConnection()
        {
            CancellationTokenSource?.Cancel(false);
        }

        #endregion

        #region Event Handlers

        private void OnTransportDisconnected(Guid sender)
        {
            if (m_connections.ContainsKey(sender))
                m_connections.TryRemove(sender, out ITransportServer? transport);

            if (m_connections.Count < Options.MaxNumberOfClients)
                WaitForConnectionSlot.Set();
        }

        #endregion

        #region Proeprties

        private NamedPipeServerTransportOptions Options { get; }

        private AutoResetEvent WaitForConnectionSlot { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

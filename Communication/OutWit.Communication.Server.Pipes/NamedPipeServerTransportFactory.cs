using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly HashSet<Guid> m_clients = new HashSet<Guid>();

        #endregion

        #region Constructors

        public NamedPipeServerTransportFactory(NamedPipeServerTransportOptions options)
        {
            Options = options;
            WaitForConnectionSlot = new Semaphore(Options.MaxNumberOfClients, Options.MaxNumberOfClients);
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
                    WaitForConnectionSlot?.WaitOne();

                    var transport = new NamedPipeServerTransport(Options);

                    if (await transport.InitializeConnectionAsync(CancellationTokenSource.Token))
                    {
                        transport.Disconnected += OnTransportDisconnected;
                        NewClientConnected(transport);
                        m_clients.Add(transport.Id);
                    }
                    else
                        transport.Dispose();
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
            if (m_clients.Contains(sender))
            {
                m_clients.Remove(sender);
                WaitForConnectionSlot?.Release();
            }
        }

        #endregion

        #region Properties

        private NamedPipeServerTransportOptions Options { get; }

        private Semaphore? WaitForConnectionSlot { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

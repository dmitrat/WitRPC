using System;
using System.Collections.Concurrent;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

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
                    }
                    
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
            WaitForConnectionSlot?.Release();
        }

        #endregion

        #region Properties

        private NamedPipeServerTransportOptions Options { get; }

        private Semaphore? WaitForConnectionSlot { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

using System;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.MMF
{
    public class MemoryMappedFileServerTransportFactory : ITransportServerFactory, IDisposable
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        #endregion

        #region Constructors

        public MemoryMappedFileServerTransportFactory(MemoryMappedFileServerTransportOptions options)
        {
            Options = options;
            WaitForConnectionSlot = new Semaphore(1, 1);
            WaitForClient = new Semaphore(0, 1, $"Global\\{options.Name}_connection");
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection(ILogger? logger)
        {
            CancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!CancellationTokenSource.Token.IsCancellationRequested)
                {
                    WaitForConnectionSlot?.WaitOne();

                    var transport = new MemoryMappedFileServerTransport(Options);

                    transport.InitializeConnectionAsync(CancellationTokenSource.Token).Wait();
                    transport.Disconnected += OnTransportDisconnected;

                    NewClientConnected(transport);
                    WaitForClient?.Release();

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

        #region IDisposable

        public void Dispose()
        {
            WaitForConnectionSlot?.Dispose();
            WaitForClient?.Dispose();
        }

        #endregion

        #region Properties

        IServerOptions ITransportServerFactory.Options => Options;

        private MemoryMappedFileServerTransportOptions Options { get; }

        private Semaphore? WaitForConnectionSlot { get; }

        private Semaphore? WaitForClient { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

   
    }
}

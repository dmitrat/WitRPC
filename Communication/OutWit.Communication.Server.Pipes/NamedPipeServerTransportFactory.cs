using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly HashSet<Guid> m_clients = new HashSet<Guid>();

        private readonly object m_syncRoot = new();

        #endregion

        #region Constructors

        public NamedPipeServerTransportFactory(NamedPipeServerTransportOptions options)
        {
            Options = options;
            WaitForConnectionSlot = new Semaphore(Options.MaxNumberOfClients, Options.MaxNumberOfClients);
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection(ILogger? logger)
        {
            lock (m_syncRoot)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(NamedPipeServerTransportFactory));

                if (CancellationTokenSource != null)
                    throw new InvalidOperationException("Pipe listener is already running");

                CancellationTokenSource = new CancellationTokenSource();
                AcceptLoopTask = Task.Run(() => AcceptLoopAsync(CancellationTokenSource.Token));
            }
        }

        public void StopWaitingForConnection()
        {
            CancellationTokenSource? cancellationTokenSource;
            Task? acceptLoopTask;

            lock (m_syncRoot)
            {
                cancellationTokenSource = CancellationTokenSource;
                acceptLoopTask = AcceptLoopTask;

                CancellationTokenSource = null;
                AcceptLoopTask = null;
            }

            if (cancellationTokenSource == null && acceptLoopTask == null)
                return;

            try
            {
                cancellationTokenSource?.Cancel(false);
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                acceptLoopTask?.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }

            cancellationTokenSource?.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, WaitForConnectionSlot! }) == 0)
                    return;

                var transport = new NamedPipeServerTransport(Options);

                if (await transport.InitializeConnectionAsync(cancellationToken))
                {
                    transport.Disconnected += OnTransportDisconnected;
                    NewClientConnected(transport);
                    m_clients.Add(transport.Id);
                }
                else
                {
                    transport.Dispose();
                    WaitForConnectionSlot?.Release();
                }
            }
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

        IServerOptions ITransportServerFactory.Options => Options;

        private NamedPipeServerTransportOptions Options { get; }

        private Semaphore? WaitForConnectionSlot { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        private Task? AcceptLoopTask { get; set; }

        private bool IsDisposed { get; set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            StopWaitingForConnection();
            WaitForConnectionSlot?.Dispose();
        }

        #endregion

    }
}

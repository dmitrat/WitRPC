using System;
using System.Collections.Generic;
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

        private readonly HashSet<Guid> m_clients = new();

        private readonly object m_syncRoot = new();

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
            lock (m_syncRoot)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(MemoryMappedFileServerTransportFactory));

                if (CancellationTokenSource != null)
                    throw new InvalidOperationException("MMF listener is already running");

                CancellationTokenSource = new CancellationTokenSource();
                AcceptLoopTask = Task.Run(() => AcceptLoopAsync(CancellationTokenSource.Token, logger));
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

            DrainPendingClientSignals();
            cancellationTokenSource?.Dispose();
        }

        private void DrainPendingClientSignals()
        {
            if (WaitForClient == null)
                return;

            while (WaitForClient.WaitOne(0))
            {
            }
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken, ILogger? logger)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, WaitForConnectionSlot! }) == 0)
                    return;

                MemoryMappedFileServerTransport? transport = null;
                var registered = false;

                try
                {
                    transport = new MemoryMappedFileServerTransport(Options);

                    if (await transport.InitializeConnectionAsync(cancellationToken))
                    {
                        transport.Disconnected += OnTransportDisconnected;

                        m_clients.Add(transport.Id);
                        registered = true;
                        NewClientConnected(transport);
                        WaitForClient?.Release();
                    }
                    else
                    {
                        transport.Dispose();
                        WaitForConnectionSlot?.Release();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    if (registered)
                        transport?.Dispose();
                    else
                        WaitForConnectionSlot?.Release();

                    return;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "MMF SERVER LOOP ERROR");

                    if (registered)
                        transport?.Dispose();
                    else
                        WaitForConnectionSlot?.Release();

                    await Task.Delay(100, cancellationToken);
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

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            StopWaitingForConnection();
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

        private Task? AcceptLoopTask { get; set; }

        private bool IsDisposed { get; set; }

        #endregion

   
    }
}

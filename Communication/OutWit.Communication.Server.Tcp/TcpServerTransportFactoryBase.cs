using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Tcp
{
    public abstract class TcpServerTransportFactoryBase<TOptions, TTransport> : ITransportServerFactory
        where TOptions : TcpServerTransportOptions
        where TTransport: ITransportServer
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, ITransportServer> m_connections = new ();

        private readonly object m_syncRoot = new();

        #endregion

        #region Constructors

        protected TcpServerTransportFactoryBase(TOptions options)
        {
            Options = options;

            if (Options.Port == null)
                throw new WitException($"Port cannot be null");
            
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection(ILogger? logger)
        {
            lock (m_syncRoot)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (Listener != null)
                    throw new InvalidOperationException("TCP listener is already running");

                var cancellationTokenSource = new CancellationTokenSource();
                var listener = new TcpListener(IPAddress.Any, Options.Port.Value);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                try
                {
                    listener.Start();
                }
                catch
                {
                    cancellationTokenSource.Dispose();
                    throw;
                }

                CancellationTokenSource = cancellationTokenSource;
                Listener = listener;
                AcceptLoopTask = Task.Run(() => AcceptLoopAsync(listener, cancellationTokenSource.Token));
            }
        }

        public void StopWaitingForConnection()
        {
            TcpListener? listener;
            CancellationTokenSource? cancellationTokenSource;
            Task? acceptLoopTask;

            lock (m_syncRoot)
            {
                listener = Listener;
                cancellationTokenSource = CancellationTokenSource;
                acceptLoopTask = AcceptLoopTask;

                Listener = null;
                CancellationTokenSource = null;
                AcceptLoopTask = null;
            }

            if (listener == null && cancellationTokenSource == null && acceptLoopTask == null)
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
                listener?.Stop();
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

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(cancellationToken);

                    if (m_connections.Count >= Options.MaxNumberOfClients)
                    {
                        client.Close();
                        continue;
                    }

                    var transport = CreateTransport(client, Options);

                    if (await transport.InitializeConnectionAsync(cancellationToken))
                    {
                        m_connections.TryAdd(transport.Id, transport);

                        transport.Disconnected += OnTransportDisconnected;

                        NewClientConnected(transport);
                    }
                    else
                    {
                        transport.Dispose();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        protected abstract TTransport CreateTransport(TcpClient client, TOptions options);

        #endregion

        #region Event Handlers

        private void OnTransportDisconnected(Guid sender)
        {
            if (m_connections.ContainsKey(sender))
                m_connections.TryRemove(sender, out ITransportServer? transport);
        }

        #endregion

        #region Properties

        IServerOptions ITransportServerFactory.Options => Options;

        private TOptions Options { get; }

        private TcpListener? Listener { get; set; }

        private Task? AcceptLoopTask { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        private bool IsDisposed { get; set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            StopWaitingForConnection();
        }

        #endregion

    }
}

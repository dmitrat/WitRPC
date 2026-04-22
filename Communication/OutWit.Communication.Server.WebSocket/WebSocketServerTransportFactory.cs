using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, ITransportServer> m_connections = new ();

        private readonly object m_syncRoot = new();

        #endregion

        #region Constructors

        public WebSocketServerTransportFactory(WebSocketServerTransportOptions options)
        {
            Options = options;

            if (Options.Host == null)
                throw new WitException($"Url cannot be null");

            if (Options.MaxNumberOfClients <= 0)
                throw new WitException($"Max number od clients must be greater than zero");

            if (Options.BufferSize < 1024)
                throw new WitException($"Buffer size must be grater or equals 1024 bytes");


        }

        #endregion

        #region Functions

        public void StartWaitingForConnection(ILogger? logger)
        {
            lock (m_syncRoot)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(WebSocketServerTransportFactory));

                if (Listener != null)
                    throw new InvalidOperationException("WebSocket listener is already running");

                var cancellationTokenSource = new CancellationTokenSource();
                var listener = new HttpListener();
                listener.Prefixes.Add(Options.Host.BuildConnection());

                try
                {
                    listener.Start();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "SERVER: Failed to start listener for {Prefix}", Options.Host.BuildConnection());
                    listener.Close();
                    cancellationTokenSource.Dispose();
                    throw;
                }

                CancellationTokenSource = cancellationTokenSource;
                Listener = listener;
                AcceptLoopTask = Task.Run(() => AcceptLoopAsync(listener, cancellationTokenSource.Token, logger));

                logger?.LogInformation("SERVER: Listener started successfully for {Prefix}", Options.Host.BuildConnection());
            }
        }

        public void StopWaitingForConnection()
        {
            HttpListener? listener;
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

            if (cancellationTokenSource == null && listener == null && acceptLoopTask == null)
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
            finally
            {
                listener?.Close();
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

        private async Task AcceptLoopAsync(HttpListener listener, CancellationToken cancellationToken, ILogger? logger)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext? httpContext = null;
                try
                {
                    httpContext = await listener.GetContextAsync();
                    logger?.LogDebug("SERVER: Got a request from {RemoteEndPoint}.", httpContext.Request.RemoteEndPoint);

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (!httpContext.Request.IsWebSocketRequest)
                    {
                        httpContext.Response.StatusCode = 400;
                        httpContext.Response.Close();
                        continue;
                    }

                    if (m_connections.Count >= Options.MaxNumberOfClients)
                    {
                        httpContext.Response.StatusCode = 503;
                        httpContext.Response.Close();
                        continue;
                    }

                    logger?.LogDebug("SERVER: Accepting WebSocket connection...");

                    var webSocketContext = await httpContext.AcceptWebSocketAsync(null);

                    var transport = new WebSocketServerTransport(webSocketContext.WebSocket, Options);
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
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "SERVER LOOP ERROR");

                    if (httpContext != null)
                    {
                        try
                        {
                            if (httpContext.Response.OutputStream.CanWrite)
                            {
                                httpContext.Response.StatusCode = 500;
                                httpContext.Response.Close();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                        catch (HttpListenerException)
                        {
                        }
                    }
                }
            }
        }

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

        private WebSocketServerTransportOptions Options { get; }

        private HttpListener? Listener { get; set; }

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

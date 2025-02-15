using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, ITransportServer> m_connections = new ();

        #endregion

        #region Constructors

        public WebSocketServerTransportFactory(WebSocketServerTransportOptions options)
        {
            Options = options;

            if (Options.Host == null)
                throw new WitComException($"Url cannot be null");

            if (Options.MaxNumberOfClients <= 0)
                throw new WitComException($"Max number od clients must be greater than zero");

            if (Options.BufferSize < 1024)
                throw new WitComException($"Buffer size must be grater or equals 1024 bytes");


            Listener = new HttpListener();
            Listener.Prefixes.Add(Options.Host.BuildConnection());
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection()
        {
            CancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                Listener.Start();
                while (!CancellationTokenSource.Token.IsCancellationRequested)
                {
                    var httpContext = await Listener.GetContextAsync();

                    if(CancellationTokenSource.Token.IsCancellationRequested)
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

                    var webSocketContext = await httpContext.AcceptWebSocketAsync(null);

                    var transport = new WebSocketServerTransport(webSocketContext.WebSocket, Options);
                    if (await transport.InitializeConnectionAsync(CancellationTokenSource.Token))
                    {
                        m_connections.TryAdd(transport.Id, transport);

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
            if (m_connections.ContainsKey(sender))
                m_connections.TryRemove(sender, out ITransportServer? transport);
        }

        #endregion

        #region Properties

        IServerOptions ITransportServerFactory.Options => Options;

        private WebSocketServerTransportOptions Options { get; }

        private HttpListener Listener { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

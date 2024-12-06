using System;
using System.Collections.Concurrent;
using System.Net;
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

            if (string.IsNullOrEmpty(Options.Url))
                throw new CommunicationException($"Url cannot be null");
            

            Listener = new HttpListener();
            Listener.Prefixes.Add(Options.Url);
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

        #region Proeprties

        private WebSocketServerTransportOptions Options { get; }

        private HttpListener Listener { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketSecureServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, ITransportServer> m_connections = new ();

        #endregion

        #region Constructors

        public WebSocketSecureServerTransportFactory(WebSocketSecureServerTransportOptions options)
        {
            Options = options;

            if (string.IsNullOrEmpty(Options.Url))
                throw new CommunicationException($"Url cannot be null");

            if (Options.Certificate == null)
                throw new CommunicationException($"Certificate cannot be null");


            Listener = new TcpListener(IPAddress.Any, 5001);

            WaitForConnectionSlot = new AutoResetEvent(true);
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
                    WaitForConnectionSlot.WaitOne();

                    var tcpClient = await Listener.AcceptTcpClientAsync(CancellationTokenSource.Token);
                    var sslStream = new SslStream(tcpClient.GetStream(), false);
                    await sslStream.AuthenticateAsServerAsync(Options.Certificate!, clientCertificateRequired: false, 
                        SslProtocols.Tls13 | SslProtocols.Tls12, checkCertificateRevocation: false);

                    if (!sslStream.IsAuthenticated)
                    {
                        tcpClient.Close();
                        continue;
                    }

                    var webSocket = System.Net.WebSockets.WebSocket.CreateFromStream(sslStream, new WebSocketCreationOptions
                    {
                        IsServer = true,
                        SubProtocol = null,
                    });

                    var transport = new WebSocketServerTransport(webSocket, Options);
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

        private WebSocketSecureServerTransportOptions Options { get; }

        private TcpListener Listener { get; }

        private AutoResetEvent WaitForConnectionSlot { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

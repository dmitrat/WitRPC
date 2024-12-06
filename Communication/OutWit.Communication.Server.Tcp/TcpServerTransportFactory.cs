using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, ITransportServer> m_connections = new ();

        #endregion

        #region Constructors

        public TcpServerTransportFactory(TcpServerTransportOptions options)
        {
            Options = options;

            if (Options.Port == null)
                throw new WitComException($"Port cannot be null");
            

            Listener = new TcpListener(IPAddress.Any, Options.Port.Value);
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
                    var client = await Listener.AcceptTcpClientAsync(CancellationTokenSource.Token);

                    if (m_connections.Count >= Options.MaxNumberOfClients)
                    {
                        client.Close();
                        continue;
                    } 

                    var transport = new TcpServerTransport(client, Options);
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

        private TcpServerTransportOptions Options { get; }

        private TcpListener Listener { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

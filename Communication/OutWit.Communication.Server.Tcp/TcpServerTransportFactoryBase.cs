using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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

        #endregion

        #region Constructors

        protected TcpServerTransportFactoryBase(TOptions options)
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

                    var transport = CreateTransport(client, Options);

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

        private TOptions Options { get; }

        private TcpListener Listener { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

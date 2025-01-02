using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OutWit.Common.Collections;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;
using static System.Collections.Specialized.BitVector32;
using CloseReason = SuperSocket.Connection.CloseReason;

namespace OutWit.Communication.Server.WebSocketSecure
{
    public class WebSocketSecureServerTransportFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        private readonly ConcurrentDictionary<Guid, WebSocketSecureServerTransport> m_connections = new ();
        private readonly ConcurrentDictionary<string, WebSocketSecureServerTransport> m_sessions = new ();

        #endregion

        #region Constructors

        public WebSocketSecureServerTransportFactory(WebSocketSecureServerTransportOptions options)
        {
            Options = options;

            if (Options.HostInfo == null || Options.HostInfo.Port == null)
                throw new WitComException($"Url cannot be null");

            Host = WebSocketHostBuilder.Create()
                .UseWebSocketMessageHandler(OnMessageReceived)
                .UseSessionHandler(OnClientConnected)
                .ConfigureSuperSocket(config =>
                {
                    config.Name = "ddd";
                    config.AddListener(new ListenOptions
                    {
                        Ip = "Any",
                        Port = Options.HostInfo.Port.Value,
                        Path = $"/{Options.HostInfo.Path ?? ""}",
                        AuthenticationOptions = new ServerAuthenticationOptions
                        {
                            ServerCertificate = Options.Certificate
                        }
                    });
                }).Build();
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection()
        {
            CancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Host.Run();
            });

        }

        public void StopWaitingForConnection()
        {
            CancellationTokenSource?.Cancel(false);
        }

        #endregion

        #region Event Handlers

        private async ValueTask OnClientConnected(IAppSession session)
        {
            if (m_connections.Count >= Options.MaxNumberOfClients)
            {
                await session.CloseAsync(CloseReason.ApplicationError);
                return;
            }

            var transport = new WebSocketSecureServerTransport((WebSocketSession)session, Options);
            m_sessions.TryAdd(session.SessionID, transport);
            m_connections.TryAdd(transport.Id, transport);

            transport.Disconnected += OnTransportDisconnected;

            NewClientConnected(transport);

        }

        private async ValueTask OnMessageReceived(WebSocketSession session, WebSocketPackage package)
        {
            if (m_sessions.TryGetValue(session.SessionID, out var transport))
                await transport.HandleMessageAsync(package);
            
        }

        private void OnTransportDisconnected(Guid sender)
        {
            if(!m_connections.TryGetValue(sender, out var transport))
                return;

            m_connections.TryRemove(sender, out WebSocketSecureServerTransport? t);
            m_sessions.TryRemove(transport.SessionId, out WebSocketSecureServerTransport? s);

        }

        #endregion

        #region Proeprties

        private WebSocketSecureServerTransportOptions Options { get; }

        private IHost Host { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}

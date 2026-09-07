using System.Net;
using System.Net.Sockets;
using OutWit.Communication.LoadTests.Contracts;
using OutWit.Communication.LoadTests.Transports;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Callbacks;
using OutWit.Communication.Server.Tcp.Utils;
using OutWit.Communication.Server.WebSocket.Utils;

namespace OutWit.Communication.LoadTests.Server
{
    /// <summary>
    /// One server with one <see cref="LoadService"/>: the shared server of the 3.2 model, or one
    /// of the N per-client servers of the baseline. Built on a real WebSocket or TCP transport,
    /// optionally behind a <see cref="ThrottlingTransportServerFactory"/>.
    /// </summary>
    public sealed class LoadHost : IDisposable
    {
        #region Constructors

        private LoadHost(WitServer server, LoadService service, string clientEndpoint, ThrottlingTransportServerFactory? throttling)
        {
            Server = server;
            Service = service;
            ClientEndpoint = clientEndpoint;
            Throttling = throttling;
        }

        #endregion

        #region Functions

        public static LoadHost Start(bool webSocket, int maxClients, string token, TimeSpan timeout,
            CallbackDeliveryOptions delivery, Func<int, (TimeSpan Delay, bool Stalled)?>? throttleFor)
        {
            var port = FreePort();
            var service = new LoadService();
            ThrottlingTransportServerFactory? throttling = null;

            var server = WitServerBuilder.Build(options =>
            {
                var transport = webSocket
                    ? options.WithWebSocket($"http://127.0.0.1:{port}/", maxClients, 10 * 1024 * 1024).TransportFactory!
                    : options.WithTcp(port, maxClients).TransportFactory!;

                if (throttleFor != null)
                {
                    throttling = new ThrottlingTransportServerFactory(transport, throttleFor);
                    options.WithTransport(throttling);
                }

                options.WithJson();
                options.WithEncryption();
                options.WithAccessToken(token);
                options.WithTimeout(timeout);
                options.WithService<ILoadService>(service);
                options.WithCallbackDelivery(delivery);
            });

            server.StartWaitingForConnection();

            var endpoint = webSocket ? $"ws://127.0.0.1:{port}/" : $"127.0.0.1:{port}";
            return new LoadHost(server, service, endpoint, throttling);
        }

        private static int FreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Server.StopWaitingForConnection();
            Server.Dispose();
        }

        #endregion

        #region Properties

        public WitServer Server { get; }

        public LoadService Service { get; }

        public string ClientEndpoint { get; }

        public ThrottlingTransportServerFactory? Throttling { get; }

        #endregion
    }
}

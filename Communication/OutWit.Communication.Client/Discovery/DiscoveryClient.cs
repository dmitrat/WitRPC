using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;

namespace OutWit.Communication.Client.Discovery
{
    public class DiscoveryClient : IDiscoveryClient
    {
        #region Events

        public event DiscoveryClientEventHandler MessageReceived = delegate { };

        #endregion

        #region Constructors

        public DiscoveryClient(DiscoveryClientOptions options)
        {
            if (options.IpAddress == null)
                throw new WitException("Discovery ip address is empty");

            if (options.Port == 0)
                throw new WitException("Discovery port is empty");

            if (options.Serializer == null)
                throw new WitException("Serializer os empty");

            Options = options;
        }

        #endregion

        #region Functions

        public async Task<bool> Start(ILogger? logger = null)
        {
            if(Options.IpAddress == null || Options.Port == 0 || Options.Serializer == null)
                return false;

            try
            {
                if (UdpClient != null)
                    Dispose();

                LocalEndpoint = new IPEndPoint(IPAddress.Any, Options.Port!.Value);
                MulticastEndpoint = new IPEndPoint(Options.IpAddress!, Options.Port!.Value);
                UdpClient = new UdpClient();
                UdpClient.ExclusiveAddressUse = false;
                UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                UdpClient.Client.Bind(LocalEndpoint);

                UdpClient.JoinMulticastGroup(MulticastEndpoint.Address);

                CancellationToken = new CancellationTokenSource();

                _ = StartListeningAsync(CancellationToken.Token, logger);

                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error starting discovery server");
                return false;
            }
        }

        private async Task StartListeningAsync(CancellationToken token, ILogger? logger)
        {
            if(UdpClient == null || Options.Serializer == null)
                return;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await UdpClient.ReceiveAsync(token);
                    var message = Options.Serializer.Deserialize<DiscoveryMessage>(result.Buffer);
                    if (message == null)
                        logger?.LogError("Failed to deserialize discovery message");

                    else
                        MessageReceived(this, message);

                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to receive discovery message");
                }
            }
        }

        public async Task Stop()
        {
            
#if NET8_0_OR_GREATER
            if (CancellationToken != null)
                await CancellationToken.CancelAsync();

#else
            if (CancellationToken != null)
                CancellationToken.Cancel();
#endif
            Dispose();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (MulticastEndpoint != null && UdpClient != null)
                UdpClient.DropMulticastGroup(MulticastEndpoint.Address);

            UdpClient?.Close();
            UdpClient?.Dispose();
            UdpClient = null;

            MulticastEndpoint = null;
        }

        #endregion

        #region Properties

        private DiscoveryClientOptions Options { get; }


        private CancellationTokenSource? CancellationToken { get; set; }

        private IPEndPoint? MulticastEndpoint { get; set; }

        private IPEndPoint? LocalEndpoint { get; set; }

        private UdpClient? UdpClient { get; set; }


        #endregion
    }
}

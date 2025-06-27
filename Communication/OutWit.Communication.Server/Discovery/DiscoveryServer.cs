using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Discovery
{
    public class DiscoveryServer : IDiscoveryServer
    {
        #region Events

        public event DiscoveryServerEventHandler DiscoveryMessageRequested = delegate { };

        #endregion

        #region Constructors

        public DiscoveryServer(DiscoveryServerOptions options)
        {
            if (options.IpAddress == null)
                throw new WitException("Discovery ip address is empty");

            if(options.Port == 0)
                throw new WitException("Discovery port is empty");

            if(options.Mode == DiscoveryServerMode.Continuous && (options.Period == null || options.Period == TimeSpan.Zero))
                throw new WitException("Discovery period is empty");

            Options = options;
        }

        #endregion

        #region Functions

        public async Task<bool> Start(ILogger? logger = null)
        {
            try
            {
                if (UdpClient != null)
                    Dispose();

                MulticastEndpoint = new IPEndPoint(Options.IpAddress!, Options.Port);
                UdpClient = new UdpClient();
                UdpClient.JoinMulticastGroup(MulticastEndpoint.Address);

                if(Options.Mode == DiscoveryServerMode.StartStop)
                    return true;

                Timer = new Timer(OnTimer, null, Options.Period!.Value, Options.Period!.Value);

                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error starting discovery server");
                return false;
            }
        }

        public async Task<bool> SendDiscoveryMessage(byte[] data, ILogger? logger = null)
        {
            if (UdpClient == null)
                return false;

            try
            {
                await UdpClient.SendAsync(data, data.Length, MulticastEndpoint);

                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error sending discovery message");
                return false;
            }
        }

        public async Task Stop()
        {
            Dispose();
        }

        #endregion

        #region EventHandler

        private void OnTimer(object? state)
        {
            DiscoveryMessageRequested(this);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Timer?.Dispose();
            Timer = null;

            if(MulticastEndpoint !=null && UdpClient != null)
                UdpClient.DropMulticastGroup(MulticastEndpoint.Address);

            UdpClient?.Close();
            UdpClient?.Dispose();
            UdpClient = null;

            MulticastEndpoint = null;
        }

        #endregion

        #region Properties

        private DiscoveryServerOptions Options { get; }


        private IPEndPoint? MulticastEndpoint { get; set; }

        private UdpClient? UdpClient { get; set; }

        private Timer? Timer { get; set; }

        #endregion

    }
}

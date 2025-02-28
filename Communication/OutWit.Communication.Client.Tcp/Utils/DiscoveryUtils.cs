using System;
using System.Net.Security;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Messages;

namespace OutWit.Communication.Client.Tcp.Utils
{
    public static class DiscoveryUtils
    {
        private const string TRANSPORT = "TCP";

        public static bool IsTcp(this DiscoveryMessage me)
        {
            return me.Transport == TRANSPORT;
        }

        public static int TcpPort(this DiscoveryMessage me)
        {
            if(!me.IsTcp())
                throw new WitComException($"Wrong transport type: {me.Transport}");

            if(me.Data == null)
                throw new WitComException($"Discovery data is empty", new ArgumentNullException(nameof(me.Data)));

            if (!me.Data.TryGetValue(nameof(TcpClientTransportOptions.Port), out var portString))
                throw new WitComException($"Cannot find parameter value for parameter: {nameof(TcpClientTransportOptions.Port)}");
            
            if (!int.TryParse(portString, out int port))
                throw new WitComException($"Wrong value for parameter value for parameter: {nameof(TcpClientTransportOptions.Port)},value: {portString}");

            return port;
        }

        public static WitComClientBuilderOptions WithTcp(this WitComClientBuilderOptions me, string host, DiscoveryMessage message)
        {
            return me.WithTcp(host, message.TcpPort());
        }

        public static WitComClientBuilderOptions WithTcpSecure(this WitComClientBuilderOptions me, string host, string targetHost, DiscoveryMessage message, RemoteCertificateValidationCallback? sslValidationCallback)
        {
            return me.WithTcpSecure(host, message.TcpPort(), targetHost, sslValidationCallback);
        }
    }
}

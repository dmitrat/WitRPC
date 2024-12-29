using System;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.Tcp.Utils
{
    public static class ClientTcpUtils
    {
        public static WitComClientBuilderOptions WithTcp(this WitComClientBuilderOptions me, TcpClientTransportOptions options)
        {
            me.Transport = new TcpClientTransport(options);
            return me;
        }

        public static WitComClientBuilderOptions WithTcp(this WitComClientBuilderOptions me, string host, int port)
        {
            return me.WithTcp(new TcpClientTransportOptions
            {
                Host = host,
                Port = port
            });
        }

        public static WitComClientBuilderOptions WithTcp(this WitComClientBuilderOptions me, HostInfo hostInfo)
        {
            return me.WithTcp(new TcpClientTransportOptions
            {
                Host = hostInfo.Host,
                Port = hostInfo.Port
            });
        }
    }
}

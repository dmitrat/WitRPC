using OutWit.Communication.Interfaces;
using System;
using OutWit.Communication.Model;

namespace OutWit.Communication.Server.Tcp.Utils
{
    public static class ServeTcpUtils
    {
        public static WitComServerBuilderOptions WithTcp(this WitComServerBuilderOptions me, TcpServerTransportOptions options)
        {
            me.TransportFactory = new TcpServerTransportFactory(options);
            return me;
        }

        public static WitComServerBuilderOptions WithTcp(this WitComServerBuilderOptions me, int port, int maxNumberOfClients)
        {
            return me.WithTcp(new TcpServerTransportOptions
            {
                Port = port,
                MaxNumberOfClients = maxNumberOfClients
            });
        }

        public static WitComServerBuilderOptions WithTcp(this WitComServerBuilderOptions me, HostInfo hostInfo)
        {
            return me.WithTcp(new TcpServerTransportOptions
            {
                Port = hostInfo.Port,
                MaxNumberOfClients = 1
            });
        }
    }
}

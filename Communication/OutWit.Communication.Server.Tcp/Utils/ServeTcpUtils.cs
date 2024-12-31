using OutWit.Communication.Interfaces;
using System;
using System.Security.Cryptography.X509Certificates;
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

        public static WitComServerBuilderOptions WithTcpSecure(this WitComServerBuilderOptions me, TcpSecureServerTransportOptions options)
        {
            me.TransportFactory = new TcpSecureServerTransportFactory(options);
            return me;
        }

        public static WitComServerBuilderOptions WithTcpSecure(this WitComServerBuilderOptions me, int port, int maxNumberOfClients, X509Certificate certificate)
        {
            return me.WithTcpSecure(new TcpSecureServerTransportOptions
            {
                Port = port,
                MaxNumberOfClients = maxNumberOfClients,
                Certificate = certificate
            });
        }

        public static WitComServerBuilderOptions WithTcpSecure(this WitComServerBuilderOptions me, HostInfo hostInfo, X509Certificate certificate)
        {
            return me.WithTcpSecure(new TcpSecureServerTransportOptions
            {
                Port = hostInfo.Port,
                MaxNumberOfClients = 1,
                Certificate = certificate
            });
        }
    }
}

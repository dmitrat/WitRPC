using System;
using System.Net.Security;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.Tcp.Utils
{
    public static class ClientTcpUtils
    {
        public static WitClientBuilderOptions WithTcp(this WitClientBuilderOptions me, TcpClientTransportOptions options)
        {
            me.Transport = new TcpClientTransport(options);
            return me;
        }

        public static WitClientBuilderOptions WithTcp(this WitClientBuilderOptions me, string host, int port)
        {
            return me.WithTcp(new TcpClientTransportOptions
            {
                Host = host,
                Port = port
            });
        }

        public static WitClientBuilderOptions WithTcp(this WitClientBuilderOptions me, HostInfo hostInfo)
        {
            return me.WithTcp(new TcpClientTransportOptions
            {
                Host = hostInfo.Host,
                Port = hostInfo.Port
            });
        }

        public static WitClientBuilderOptions WithTcpSecure(this WitClientBuilderOptions me, TcpSecureClientTransportOptions options)
        {
            me.Transport = new TcpSecureClientTransport(options);
            return me;
        }

        public static WitClientBuilderOptions WithTcpSecure(this WitClientBuilderOptions me, string host, int port, string targetHost, RemoteCertificateValidationCallback? sslValidationCallback)
        {
            return me.WithTcpSecure(new TcpSecureClientTransportOptions
            {
                Host = host,
                Port = port,
                TargetHost = targetHost,
                SslValidationCallback = sslValidationCallback
            });
        }

        public static WitClientBuilderOptions WithTcpSecure(this WitClientBuilderOptions me, HostInfo hostInfo, string targetHost, RemoteCertificateValidationCallback? sslValidationCallback)
        {
            return me.WithTcpSecure(new TcpSecureClientTransportOptions
            {
                Host = hostInfo.Host,
                Port = hostInfo.Port,
                TargetHost = targetHost,
                SslValidationCallback = sslValidationCallback
            });
        }
    }
}

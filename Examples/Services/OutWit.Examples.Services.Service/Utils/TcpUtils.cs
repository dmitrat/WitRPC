using System.Net.Sockets;
using System.Net;

namespace OutWit.Examples.Services.Service.Utils
{
    public static class TcpUtils
    {
        public static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}

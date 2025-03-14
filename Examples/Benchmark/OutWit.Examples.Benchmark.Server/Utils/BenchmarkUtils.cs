using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Benchmark.Server.Utils
{
    public static class NetworkUtils
    {
        public static int NextFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            listener.Stop();

            return port;
        }
    }
}

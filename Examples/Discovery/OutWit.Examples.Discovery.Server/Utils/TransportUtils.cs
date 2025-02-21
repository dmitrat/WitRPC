using System;
using OutWit.Communication.Server;
using OutWit.Communication.Server.MMF.Utils;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Communication.Server.Tcp.Utils;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Communication.Model;
using OutWit.Examples.Discovery.Server.Model;

namespace OutWit.Examples.Discovery.Server.Utils
{
    public static class TransportUtils
    {
        public static WitComServerBuilderOptions WithTransport(this WitComServerBuilderOptions me, TransportType transport, string address, int port)
        {
            switch (transport)
            {
                case TransportType.NamedPipe:
                    return me.WithNamedPipe(address, 10);

                case TransportType.MemoryMappedFile:
                    return me.WithMemoryMappedFile(address);

                case TransportType.TCP:
                    return me.WithTcp(port, 10);

                case TransportType.WebSocket:
                    return me.WithWebSocket($"http://localhost:{port}/{address}", 10);

                default:
                    throw new ArgumentOutOfRangeException(nameof(me), me, null);
            }
            return me;
        }
    }
}

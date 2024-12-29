using System;
using OutWit.Communication.Server;
using OutWit.Communication.Server.MMF.Utils;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Communication.Server.Tcp.Utils;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Communication.Model;
using OutWit.Examples.InterProcess.Shared;

namespace OutWit.Examples.InterProcess.BasicAgent.Utils
{
    public static class TransportUtils
    {
        public static WitComServerBuilderOptions WithTransport(this WitComServerBuilderOptions me, TransportType transport, string address)
        {
            switch (transport)
            {
                case TransportType.Pipes:
                    return me.WithNamedPipe(address);

                case TransportType.MMF:
                    return me.WithMemoryMappedFile(address);

                case TransportType.Tcp:
                    return me.WithTcp((HostInfo)address);

                case TransportType.WebSocket:
                {
                    var info = (HostInfo)address;
                    info.UseWebSocket = false;
                    return me.WithWebSocket(info);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(me), me, null);
            }
            return me;
        }
    }
}

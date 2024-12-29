using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Random;
using OutWit.Communication.Client;
using OutWit.Communication.Client.MMF.Utils;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Model;
using OutWit.Examples.InterProcess.Shared;
using OutWit.Examples.InterProcess.Shared.Utils;

namespace OutWit.Examples.InterProcess.BasicHost.Utils
{
    public static class TransportUtils
    {
        public static StartupParametersTransport GetParameters( this TransportType me)
        {
            switch (me)
            {
                case TransportType.Pipes:
                    return new StartupParametersTransport(me, RandomUtils.RandomString(), Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                case TransportType.MMF:
                    return new StartupParametersTransport(me, RandomUtils.RandomString(), Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                case TransportType.Tcp:
                    return new StartupParametersTransport(me, $"127.0.0.1:{TcpUtils.FreeTcpPort()}", Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                case TransportType.WebSocket:
                    return new StartupParametersTransport(me, $"ws://localhost:{TcpUtils.FreeTcpPort()}/api/", Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                default:
                    throw new ArgumentOutOfRangeException(nameof(me), me, null);
            }
        }

        public static WitComClientBuilderOptions WithTransport(this WitComClientBuilderOptions me, TransportType transport, string address)
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
                    return me.WithWebSocket((HostInfo)address);

                default:
                    throw new ArgumentOutOfRangeException(nameof(me), me, null);
            }
            return me;
        }
    }
}

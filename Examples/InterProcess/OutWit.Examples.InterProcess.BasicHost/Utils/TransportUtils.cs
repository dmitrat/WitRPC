using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
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
        public static StartupParametersTransport GetParameters( this TransportType me, ILogger logger)
        {
            switch (me)
            {
                case TransportType.Pipes:
                {
                    var pipeName = RandomUtils.RandomString();

                    logger.LogInformation($"Starting pipes agent, pipe name: {pipeName}");

                    return new StartupParametersTransport(me, pipeName, Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                }

                case TransportType.MMF:
                {
                    var mmfName = RandomUtils.RandomString();

                    logger.LogInformation($"Starting memory mapped file agent, file name: {mmfName}");

                    return new StartupParametersTransport(me, mmfName, Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                }

                case TransportType.Tcp:
                {
                    var tcpPort = TcpUtils.FreeTcpPort();

                    logger.LogInformation($"Starting tcp agent, tcp port: {tcpPort}");

                    return new StartupParametersTransport(me, $"localhost:{tcpPort}", Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);

                }

                case TransportType.WebSocket:
                {
                    var port = TcpUtils.FreeTcpPort();

                    logger.LogInformation($"Starting web socket agent, port: {port}");

                    return new StartupParametersTransport(me, $"ws://localhost:{port}/api/", Process.GetCurrentProcess().Id,
                        TimeSpan.Zero, true);
                }

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

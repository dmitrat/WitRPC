using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OutWit.Common.Random;
using OutWit.Communication.Client;
using OutWit.Communication.Client.MMF;
using OutWit.Communication.Client.MMF.Utils;
using OutWit.Communication.Client.Pipes;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Tcp;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;

namespace OutWit.Examples.Discovery.Client.Utils
{
    public static class TransportUtils
    {
        public static WitComClientBuilderOptions WithTransport(this WitComClientBuilderOptions me, DiscoveryMessage message)
        {
            if(message.Data == null)
                throw new ArgumentOutOfRangeException(nameof(me), me, null);

            switch (message.Transport)
            {
                case "NamedPipe":
                {
                    if(!message.Data.TryGetValue(nameof(NamedPipeClientTransportOptions.PipeName), out var pipeName))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);
                    return me.WithNamedPipe(pipeName);
                }

                case "MemoryMappedFile":
                {
                    if (!message.Data.TryGetValue(nameof(MemoryMappedFileClientTransportOptions.Name), out var name))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);
                    
                    return me.WithMemoryMappedFile(name);
                }

                case "TCP":
                {
                    if (!message.Data.TryGetValue(nameof(TcpClientTransportOptions.Port), out var portString))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);

                    if(!int.TryParse(portString, out int port))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);

                    return me.WithTcp("localhost", port);
                }

                case "WebSocket":
                {
                    if (!message.Data.TryGetValue(nameof(HostInfo.Path), out var path))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);

                    if (!message.Data.TryGetValue(nameof(HostInfo.Port), out var portString))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);

                    if (!int.TryParse(portString, out int port))
                        throw new ArgumentOutOfRangeException(nameof(me), me, null);

                    return me.WithWebSocket($"ws://localhost:{port}/{path}");

                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(me), me, null);
            }
            return me;
        }
    }
}

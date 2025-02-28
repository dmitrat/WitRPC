using System;
using OutWit.Communication.Client;
using OutWit.Communication.Client.MMF.Utils;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Messages;

namespace OutWit.Examples.Discovery.Client.Utils
{
    public static class TransportUtils
    {
        public static WitComClientBuilderOptions WithTransport(this WitComClientBuilderOptions me, DiscoveryMessage message)
        {
            if(message.Data == null)
                throw new ArgumentOutOfRangeException(nameof(me), me, null);

            if (message.IsMemoryMappedFile())
                return me.WithMemoryMappedFile(message);

            if (message.IsNamedPipe())
                return me.WithNamedPipe(message);

            if (message.IsTcp())
                return me.WithTcp("localhost", message);

            if (message.IsWebSocket())
                return me.WithWebSocket("ws://localhost", message);

            throw new ArgumentOutOfRangeException(nameof(me), me, null);
 
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.WebSocket.Utils
{
    public static class DiscoveryUtils
    {
        private const string TRANSPORT = "WebSocket";

        public static bool IsWebSocket(this DiscoveryMessage me)
        {
            return me.Transport == TRANSPORT;
        }

        public static int WebSocketPort(this DiscoveryMessage me)
        {
            if(!me.IsWebSocket())
                throw new WitException($"Wrong transport type: {me.Transport}");

            if(me.Data == null)
                throw new WitException($"Discovery data is empty", new ArgumentNullException(nameof(me.Data)));

            if (!me.Data.TryGetValue(nameof(HostInfo.Port), out var portString))
                throw new WitException($"Cannot find parameter value for parameter: {nameof(HostInfo.Port)}");
            
            if (!int.TryParse(portString, out int port))
                throw new WitException($"Wrong value for parameter value for parameter: {nameof(HostInfo.Port)},value: {portString}");

            return port;
        }

        public static string WebSocketPath(this DiscoveryMessage me)
        {
            if (!me.IsWebSocket())
                throw new WitException($"Wrong transport type: {me.Transport}");

            if (me.Data == null)
                throw new WitException($"Discovery data is empty", new ArgumentNullException(nameof(me.Data)));

            if (!me.Data.TryGetValue(nameof(HostInfo.Path), out var path))
                throw new WitException($"Cannot find parameter value for parameter: {nameof(HostInfo.Path)}");

            return path;
        }

        public static WitClientBuilderOptions WithWebSocket(this WitClientBuilderOptions me, string host, DiscoveryMessage message, int bufferSize = ClientWebSocketUtils.DEFAULT_BUFFER_SIZE)
        {
            return me.WithWebSocket($"{host}:{message.WebSocketPort()}/{message.WebSocketPath()}", bufferSize);
        }
    }
}

using System;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.WebSocket.Utils
{
    public static class ClientWebSocketUtils
    {
        public static WitComClientBuilderOptions WithWebSocket(this WitComClientBuilderOptions me, WebSocketClientTransportOptions options)
        {
            me.Transport = new WebSocketClientTransport(options);
            return me;
        }

        public static WitComClientBuilderOptions WithWebSocket(this WitComClientBuilderOptions me, string url)
        {
            return me.WithWebSocket(new WebSocketClientTransportOptions
            {
                Url = url
            });
        }

        public static WitComClientBuilderOptions WithWebSocket(this WitComClientBuilderOptions me, HostInfo hostInfo)
        {
            return me.WithWebSocket(new WebSocketClientTransportOptions
            {
                Url = hostInfo.BuildConnection(true)
            });
        }
    }
}

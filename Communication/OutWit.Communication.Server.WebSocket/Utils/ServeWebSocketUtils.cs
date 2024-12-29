using OutWit.Communication.Interfaces;
using System;
using OutWit.Communication.Model;

namespace OutWit.Communication.Server.WebSocket.Utils
{
    public static class ServeWebSocketUtils
    {
        public static WitComServerBuilderOptions WithWebSocket(this WitComServerBuilderOptions me, WebSocketServerTransportOptions options)
        {
            me.TransportFactory = new WebSocketServerTransportFactory(options);
            return me;
        }

        public static WitComServerBuilderOptions WithWebSocket(this WitComServerBuilderOptions me, string url, int maxNumberOfClients)
        {
            return me.WithWebSocket(new WebSocketServerTransportOptions
            {
                Url = url,
                MaxNumberOfClients = maxNumberOfClients
            });
        }

        public static WitComServerBuilderOptions WithWebSocket(this WitComServerBuilderOptions me, HostInfo hostInfo)
        {
            return me.WithWebSocket(new WebSocketServerTransportOptions
            {
                Url = hostInfo.BuildConnection(true),
                MaxNumberOfClients = 1
            });
        }
    }
}

using System;
using OutWit.Communication.Model;

namespace OutWit.Communication.Server.WebSocket.Utils
{
    public static class ServerWebSocketUtils
    {
        private const int DEFAULT_BUFFER_SIZE = 4096;

        public static WitServerBuilderOptions WithWebSocket(this WitServerBuilderOptions me, WebSocketServerTransportOptions options)
        {
            me.TransportFactory = new WebSocketServerTransportFactory(options);
            return me;
        }

        public static WitServerBuilderOptions WithWebSocket(this WitServerBuilderOptions me, string url, int maxNumberOfClients, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return me.WithWebSocket(new WebSocketServerTransportOptions
            {
                Host = (HostInfo)url,
                MaxNumberOfClients = maxNumberOfClients,
                BufferSize = bufferSize
            });
        }

        public static WitServerBuilderOptions WithWebSocket(this WitServerBuilderOptions me, HostInfo hostInfo, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return me.WithWebSocket(new WebSocketServerTransportOptions
            {
                Host = hostInfo,
                MaxNumberOfClients = 1,
                BufferSize = bufferSize
            });
        }
    }
}

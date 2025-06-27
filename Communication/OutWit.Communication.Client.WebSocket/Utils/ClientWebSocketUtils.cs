using System;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.WebSocket.Utils
{
    public static class ClientWebSocketUtils
    {
        internal const int DEFAULT_BUFFER_SIZE = 4096;

        public static WitClientBuilderOptions WithWebSocket(this WitClientBuilderOptions me, WebSocketClientTransportOptions options)
        {
            me.Transport = new WebSocketClientTransport(options);

            return me;
        }

        public static WitClientBuilderOptions WithWebSocket(this WitClientBuilderOptions me, string url, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return me.WithWebSocket(new WebSocketClientTransportOptions
            {
                Url = url,
                BufferSize = bufferSize
            });
        }

        public static WitClientBuilderOptions WithWebSocket(this WitClientBuilderOptions me, HostInfo hostInfo, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return me.WithWebSocket(new WebSocketClientTransportOptions
            {
                Url = hostInfo.BuildConnection(true),
                BufferSize = bufferSize
            });
        }
    }
}

using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client.WebSocket
{
    public class WebSocketClientTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Url: {Url}, BufferSize: {BufferSize}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WebSocketClientTransportOptions options))
                return false;

            return Url.Is(options.Url) &&
                   BufferSize.Is(options.BufferSize);
        }

        public override WebSocketClientTransportOptions Clone()
        {
            return new WebSocketClientTransportOptions
            {
                MaxMessageSize = MaxMessageSize,
                Url = Url,
                BufferSize = BufferSize
            };
        }

        #endregion

        #region Properties

        /// <summary>The largest frame accepted from the peer, in bytes. Larger frames close the connection instead of being allocated.</summary>
        public long MaxMessageSize { get; set; } = StreamFrameReader.DEFAULT_MAX_MESSAGE_SIZE;

        public string? Url { get; set; }

        public int BufferSize { get; set; }

        #endregion
    }
}

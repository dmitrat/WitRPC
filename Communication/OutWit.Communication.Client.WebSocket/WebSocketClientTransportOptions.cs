using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

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
                Url = Url,
                BufferSize = BufferSize
            };
        }

        #endregion

        #region Properties

        public string? Url { get; set; }

        public int BufferSize { get; set; }

        #endregion
    }
}

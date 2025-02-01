using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketServerTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Url: {Url}, MaxNumberOfClients: {MaxNumberOfClients}, BufferSize: {BufferSize}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WebSocketServerTransportOptions options))
                return false;

            return Url.Is(options.Url) && 
                   MaxNumberOfClients.Is(options.MaxNumberOfClients) &&
                   BufferSize.Is(options.BufferSize);
        }

        public override WebSocketServerTransportOptions Clone()
        {
            return new WebSocketServerTransportOptions
            {
                Url = Url,
                MaxNumberOfClients = MaxNumberOfClients,
                BufferSize = BufferSize
            };
        }

        #endregion

        #region Properties

        public int MaxNumberOfClients { get; set; }

        public int BufferSize { get; set; }
        
        public string? Url { get; set; }

        #endregion
    }
}

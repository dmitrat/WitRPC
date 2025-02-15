using System;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketServerTransportOptions : ModelBase, IServerOptions
    {
        #region Constants

        private const string TRANSPORT = "WebSocket";

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Url: {Host?.BuildConnection(true)}, MaxNumberOfClients: {MaxNumberOfClients}, BufferSize: {BufferSize}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WebSocketServerTransportOptions options))
                return false;

            return Host.Check(options.Host) && 
                   MaxNumberOfClients.Is(options.MaxNumberOfClients) &&
                   BufferSize.Is(options.BufferSize);
        }

        public override WebSocketServerTransportOptions Clone()
        {
            return new WebSocketServerTransportOptions
            {
                Host = Host?.Clone(),
                MaxNumberOfClients = MaxNumberOfClients,
                BufferSize = BufferSize
            };
        }

        #endregion

        #region IServerOptions

        public string Transport { get; } = TRANSPORT;

        public Dictionary<string, string> Data =>
            Host == null
                ? new Dictionary<string, string>()
                : new()
                {
                    { $"{nameof(HostInfo.Port)}", $"{Host.Port}" },
                    { $"{nameof(HostInfo.Path)}", $"{Host.Path}" },
                    { $"{nameof(HostInfo.UseSsl)}", $"{Host.UseSsl}" },
                    { $"{nameof(HostInfo.UseWebSocket)}", $"{Host.UseWebSocket}" },
                    { $"{nameof(MaxNumberOfClients)}", $"{MaxNumberOfClients}" },
                    { $"{nameof(BufferSize)}", $"{BufferSize}" },
                };


        #endregion

        #region Properties

        public int MaxNumberOfClients { get; set; }

        public int BufferSize { get; set; }
        
        public HostInfo? Host { get; set; }

        #endregion
    }
}

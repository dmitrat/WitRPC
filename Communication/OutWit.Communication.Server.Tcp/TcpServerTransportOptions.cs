using System;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransportOptions : ModelBase, IServerOptions
    {
        #region Constants

        private const string TRANSPORT = "TCP";

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Pipe: {Port}, MaxNumberOfClients: {MaxNumberOfClients}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is TcpServerTransportOptions options))
                return false;

            return Port.Is(options.Port) && 
                   MaxNumberOfClients.Is(options.MaxNumberOfClients);
        }

        public override TcpServerTransportOptions Clone()
        {
            return new TcpServerTransportOptions
            {
                MaxMessageSize = MaxMessageSize,
                Port = Port,
                MaxNumberOfClients = MaxNumberOfClients
            };
        }

        #endregion

        #region IServerOptions

        public string Transport { get; } = TRANSPORT;

        public Dictionary<string, string> Data =>
            new()
            {
                { $"{nameof(Port)}", $"{Port}" },
                { $"{nameof(MaxNumberOfClients)}", $"{MaxNumberOfClients}" },
            };

        #endregion

        #region Properties

        /// <summary>The largest frame accepted from the peer, in bytes. Larger frames close the connection instead of being allocated.</summary>
        public long MaxMessageSize { get; set; } = StreamFrameReader.DEFAULT_MAX_MESSAGE_SIZE;

        public int MaxNumberOfClients { get; set; }
        
        public int? Port { get; set; }

        #endregion
    }
}

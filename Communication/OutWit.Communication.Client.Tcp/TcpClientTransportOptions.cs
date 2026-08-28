using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client.Tcp
{
    public class TcpClientTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Host: {Host}, Port: {Port}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is TcpClientTransportOptions options))
                return false;

            return Host.Is(options.Host) &&
                   Port.Is(options.Port);
        }

        public override TcpClientTransportOptions Clone()
        {
            return new TcpClientTransportOptions
            {
                MaxMessageSize = MaxMessageSize,
                Host = Host,
                Port = Port
            };
        }

        #endregion

        #region Properties

        /// <summary>The largest frame accepted from the peer, in bytes. Larger frames close the connection instead of being allocated.</summary>
        public long MaxMessageSize { get; set; } = StreamFrameReader.DEFAULT_MAX_MESSAGE_SIZE;

        public string? Host { get; set; }

        public int? Port { get; set; }

        #endregion
    }
}

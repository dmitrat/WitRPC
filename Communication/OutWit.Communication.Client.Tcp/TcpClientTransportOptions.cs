using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

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
                Host = Host,
                Port = Port
            };
        }

        #endregion

        #region Properties

        public string? Host { get; set; }

        public int? Port { get; set; }

        #endregion
    }
}

using System;
using System.Net.Security;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Client.Tcp
{
    public class TcpSecureClientTransportOptions : TcpClientTransportOptions
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is TcpSecureClientTransportOptions options))
                return false;

            return base.Is(options, tolerance) 
                   && TargetHost.Is(options.TargetHost);
        }

        public override TcpSecureClientTransportOptions Clone()
        {
            return new TcpSecureClientTransportOptions
            {
                Host = Host,
                Port = Port,
                TargetHost = TargetHost,
                SslValidationCallback = SslValidationCallback
            };
        }

        #endregion

        #region Properties

        public string? TargetHost { get; set; }

        public RemoteCertificateValidationCallback? SslValidationCallback { get; set; }

        #endregion
    }
}

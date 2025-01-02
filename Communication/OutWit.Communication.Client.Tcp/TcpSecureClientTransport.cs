using System;
using System.IO;
using System.Net.Security;

namespace OutWit.Communication.Client.Tcp
{
    public class TcpSecureClientTransport : TcpClientTransportBase<TcpSecureClientTransportOptions>
    {
        #region Constructors

        public TcpSecureClientTransport(TcpSecureClientTransportOptions options)
            : base(options)
        {

        }

        #endregion

        #region ITransport

        protected override Stream CreateStream()
        {
            var stream = Options.SslValidationCallback == null 
                ? new SslStream(Client!.GetStream(), false) 
                : new SslStream(Client!.GetStream(), false, Options.SslValidationCallback);

            stream.AuthenticateAsClient(Options.TargetHost ?? "");

            return stream;
        }

        #endregion
    }
}

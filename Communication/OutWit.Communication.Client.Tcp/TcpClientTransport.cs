using System;

namespace OutWit.Communication.Client.Tcp
{
    public class TcpClientTransport : TcpClientTransportBase<TcpClientTransportOptions>
    {
        #region Constructors

        public TcpClientTransport(TcpClientTransportOptions options)
            : base(options)
        {

        }

        #endregion

        #region ITransport

        protected override Stream CreateStream()
        {
            return Client!.GetStream();
        }

        #endregion
    }
}

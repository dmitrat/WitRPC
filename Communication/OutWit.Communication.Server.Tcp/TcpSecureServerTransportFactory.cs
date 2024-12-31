using System;
using System.Net.Sockets;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpSecureServerTransportFactory : TcpServerTransportFactoryBase<TcpSecureServerTransportOptions, TcpSecureServerTransport>
    {
        #region Constructors

        public TcpSecureServerTransportFactory(TcpSecureServerTransportOptions options)
            : base(options)
        {
        }

        #endregion

        #region Functions

        protected override TcpSecureServerTransport CreateTransport(TcpClient client, TcpSecureServerTransportOptions options)
        {
            return new TcpSecureServerTransport(client, options);
        }

        #endregion


    }
}

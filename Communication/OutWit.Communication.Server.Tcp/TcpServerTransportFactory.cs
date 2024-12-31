using System;
using System.Net.Sockets;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransportFactory : TcpServerTransportFactoryBase<TcpServerTransportOptions, TcpServerTransport>
    {
        #region Constructors

        public TcpServerTransportFactory(TcpServerTransportOptions options) : base(options)
        {
        }

        #endregion

        #region Functions

        protected override TcpServerTransport CreateTransport(TcpClient client, TcpServerTransportOptions options)
        {
            return new TcpServerTransport(client, options);
        }

        #endregion


    }
}

using System.IO;
using OutWit.Communication.Interfaces;
using System.Net.Sockets;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransport : TcpServerTransportBase<TcpServerTransportOptions>
    {
        #region Constructors

        public TcpServerTransport(TcpClient? client, TcpServerTransportOptions options)
            : base(client, options)
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

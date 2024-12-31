using OutWit.Communication.Interfaces;
using System.Net.Sockets;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransport : TcpServerTransportBase<TcpServerTransportOptions>
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

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

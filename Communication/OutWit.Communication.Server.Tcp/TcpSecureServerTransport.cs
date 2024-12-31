using OutWit.Communication.Exceptions;
using System.Net.Security;
using System.Net.Sockets;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpSecureServerTransport : TcpServerTransportBase<TcpSecureServerTransportOptions>
    {
        #region Constructors

        public TcpSecureServerTransport(TcpClient? client, TcpSecureServerTransportOptions options) 
            : base(client, options)
        {
        }

        #endregion

        #region ITransport

        protected override Stream CreateStream()
        {
            if (Options.Certificate == null)
                throw new WitComExceptionTransport($"Ssl certificate cannot be empty");

            var stream = new SslStream(Client!.GetStream(), false);
            stream.AuthenticateAsServer(Options.Certificate);

            return stream;
        }

        #endregion
    }
}

using System.Buffers;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using System.Net.WebSockets;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;

namespace OutWit.Communication.Server.WebSocketSecure
{
    public class WebSocketSecureServerTransport : ITransportServer
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public WebSocketSecureServerTransport(WebSocketSession session, WebSocketSecureServerTransportOptions options)
        {
            Id = Guid.NewGuid();

            Session = session;
            SessionId = session.SessionID;
            Options = options;
        }

        #endregion

        #region ITransport

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public async Task SendBytesAsync(byte[] data)
        {
            try
            {
                await Session.SendAsync(data);

            }
            catch (IOException e)
            {
                Dispose();
            }

        }

        #endregion

        #region Functions

        public async Task HandleMessageAsync(WebSocketPackage package)
        {
            Callback(Id, package.Data.ToArray());
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public bool CanReinitialize { get; } = false;

        private WebSocketSecureServerTransportOptions Options { get; }

        public WebSocketSession Session { get; }

        public string SessionId { get; }

        #endregion
    }
}

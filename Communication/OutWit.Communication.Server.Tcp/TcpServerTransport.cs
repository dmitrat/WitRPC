using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using System.IO.Pipes;
using System.Net.Sockets;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransport : ITransportServer
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public TcpServerTransport(TcpClient? client, TcpServerTransportOptions options)
        {
            Id = Guid.NewGuid();

            Client = client;
            Options = options;
        }

        #endregion

        #region ITransport

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                if (Client == null)
                    throw new WitComExceptionTransport($"Failed to init tcp client");

                Stream = Client.GetStream();
                Reader = new BinaryReader(Stream);
                Writer = new BinaryWriter(Stream);

                IsListening = true;

                Task.Run(ListenForIncomingData);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public async Task SendBytesAsync(byte[] data)
        {
            if (Writer == null || Reader == null)
                return;

            try
            {
                Writer.Write(data.Length);
                Writer.Write(data);

            }
            catch (IOException e)
            {
                Dispose();
            }

        }

        #endregion

        #region Functions

        private async Task ListenForIncomingData()
        {
            if (Client == null || Stream == null || Writer == null || Reader == null)
                return;

            try
            {
                while (IsListening && Client.Connected)
                {
                    int dataLength = Reader.ReadInt32();
                    if (dataLength > 0)
                    {
                        byte[] data = Reader.ReadBytes(dataLength);
                        Callback(Id, data);
                    }
                }
            }
            catch (Exception)
            {
                Dispose();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            IsListening = false;

            Reader?.Dispose();
            Writer?.Dispose();
            Stream?.Dispose();
            Client?.Close();

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public bool CanReinitialize { get; } = false;

        private TcpServerTransportOptions Options { get; }

        private TcpClient? Client { get; }

        private NetworkStream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

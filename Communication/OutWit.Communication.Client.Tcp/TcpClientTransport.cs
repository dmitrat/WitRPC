using System;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Tcp
{
    public class TcpClientTransport : ITransportClient
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public TcpClientTransport(TcpClientTransportOptions options)
        {
            Options = options; }

        #endregion

        #region ITransport

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Client = new TcpClient();

            try
            {
                if (!IPAddress.TryParse(Options.Host, out IPAddress? address) || Options.Port == null)
                    return false;

                await Client.ConnectAsync(address, Options.Port.Value, cancellationToken);

                Stream = Client.GetStream();
                Reader = new BinaryReader(Stream);
                Writer = new BinaryWriter(Stream);

                IsListening = true;

                Task.Run(ListenForIncomingData);
                return true;
            }
            catch
            {
                Dispose();
                return false;
            }
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            return await ConnectAsync(TimeSpan.Zero, cancellationToken);
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

        public async Task Disconnect()
        {
            Dispose();
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

        private TcpClientTransportOptions Options { get; }

        private TcpClient? Client { get; set; }

        private NetworkStream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Tcp
{
    public abstract class TcpClientTransportBase<TOptions> : ITransportClient
        where TOptions : TcpClientTransportOptions
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        protected TcpClientTransportBase(TOptions options)
        {
            Options = options;
            Address = $"{options.Host}:{options.Port}";
            Id = Guid.NewGuid();
        }

        #endregion

        #region ITransport

        protected abstract Stream CreateStream();

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Client = new TcpClient();

            try
            {
                if (string.IsNullOrEmpty(Options.Host) || Options.Port == null)
                    return false;

                await Client.ConnectAsync(Options.Host, Options.Port.Value, cancellationToken);

                Stream = CreateStream();
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

        public string? Address { get; }

        protected TOptions Options { get; }

        protected TcpClient? Client { get; set; }

        private Stream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

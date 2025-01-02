using System;
using System.IO;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Server.Tcp
{
    public abstract class TcpServerTransportBase<TOptions> : ITransportServer
        where TOptions : TcpServerTransportOptions
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        protected TcpServerTransportBase(TcpClient? client, TOptions options)
        {
            Id = Guid.NewGuid();

            Client = client;
            Options = options;
        }

        #endregion

        #region ITransport

        protected abstract Stream CreateStream();

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                if (Client == null)
                    throw new WitComExceptionTransport($"Failed to init tcp client");

                Stream = CreateStream();
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

        protected TOptions Options { get; }

        protected TcpClient? Client { get; }

        private Stream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

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
            if (Stream == null)
                return;

            try
            {
                var lengthBuffer = BitConverter.GetBytes(data.Length);

                await Stream.WriteAsync(lengthBuffer);
                await Stream.WriteAsync(data);

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
            if (Client == null || Stream == null)
                return;

            var lengthBuffer = new byte[sizeof(int)];

            try
            {
                while (IsListening && Client.Connected)
                {
                    int bytesRead = await Stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
                    if (bytesRead == 0)
                        throw new WitComExceptionTransport($"Client disconnected");

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    var dataBuffer = new byte[messageLength];
                    int totalBytesRead = 0;

                    while (totalBytesRead < messageLength)
                    {
                        int read = await Stream.ReadAsync(dataBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0)
                            throw new WitComExceptionTransport($"Client disconnected");

                        totalBytesRead += read;
                    }

                    if (totalBytesRead == messageLength)
                        _ = Task.Run(() => Callback(Id, dataBuffer));
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

        private bool IsListening { get; set; }

        #endregion
    }
}

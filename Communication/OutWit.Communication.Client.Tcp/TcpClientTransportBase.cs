using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
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

        public async Task Disconnect()
        {
            Dispose();
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
                        throw new WitComExceptionTransport($"Server disconnected");

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    var dataBuffer = new byte[messageLength];
                    int totalBytesRead = 0;

                    while (totalBytesRead < messageLength)
                    {
                        int read = await Stream.ReadAsync(dataBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0)
                            throw new WitComExceptionTransport($"Server disconnected");

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

        public string? Address { get; }

        protected TOptions Options { get; }

        protected TcpClient? Client { get; set; }

        private Stream? Stream { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Pipes
{
    public class NamedPipeClientTransport : ITransportClient
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public NamedPipeClientTransport(NamedPipeClientTransportOptions options)
        {
            Options = options;
            Address = options.PipeName;
        }

        #endregion

        #region Initialization

        private void InitPipe()
        {
            if (string.IsNullOrEmpty(Options.ServerName))
                throw new WitExceptionTransport($"Failed to create pipe: server name is empty. " +
                                             $"Use \".\" as server name for local communication");

            if (string.IsNullOrEmpty(Options.PipeName))
                throw new WitExceptionTransport($"Failed to create pipe: pipe name is empty");

            Stream = new NamedPipeClientStream(Options.ServerName, Options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        #endregion

        #region ITransport

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            InitPipe();

            if (Stream == null)
                return false;

            try
            {
                if(timeout == TimeSpan.Zero)
                    await Stream.ConnectAsync(cancellationToken);
                else
                    await Stream.ConnectAsync((int)timeout.TotalMilliseconds, cancellationToken);

                IsListening = true;

                Task.Run(ListenForIncomingData);

                return true;
            }
            catch (Exception e)
            {
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
                await Stream.FlushAsync();
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
            if (Stream == null)
                return;

            var lengthBuffer = new byte[sizeof(int)];

            try
            {
                while (IsListening && Stream.IsConnected)
                {
                    int bytesRead = await Stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
                    if (bytesRead == 0)
                        throw new WitExceptionTransport($"Server disconnected");

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    var dataBuffer = new byte[messageLength];
                    int totalBytesRead = 0;

                    while (totalBytesRead < messageLength)
                    {
                        int read = await Stream.ReadAsync(dataBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0)
                            throw new WitExceptionTransport($"Server disconnected");

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

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public string? Address { get; }

        private NamedPipeClientTransportOptions Options { get; }

        private NamedPipeClientStream? Stream { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

using System;
using System.IO;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransport : ITransportServer
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public NamedPipeServerTransport(NamedPipeServerTransportOptions options)
        {
            Id = Guid.NewGuid();

            Options = options;

            InitPipe();

        }

        #endregion

        #region Initialization

        private void InitPipe()
        {
            if (string.IsNullOrEmpty(Options.PipeName))
                throw new WitExceptionTransport($"Failed to create pipe: pipe name is empty");

            Stream = new NamedPipeServerStream(Options.PipeName, PipeDirection.InOut,
                Options.MaxNumberOfClients, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        
        }

        #endregion

        #region ITransport

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                await Stream!.WaitForConnectionAsync(token);

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
                await Stream.FlushAsync();
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
            if (Stream == null)
                return;


            var lengthBuffer = new byte[sizeof(int)];

            try
            {
                while (IsListening && Stream.IsConnected)
                {
                    int bytesRead = await Stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
                    if (bytesRead == 0)
                        throw new WitExceptionTransport($"Client disconnected");

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    var dataBuffer = new byte[messageLength];
                    int totalBytesRead = 0;

                    while (totalBytesRead < messageLength)
                    {
                        int read = await Stream.ReadAsync(dataBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0)
                            throw new WitExceptionTransport($"Client disconnected");

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

        public bool CanReinitialize { get; } = false;

        private NamedPipeServerTransportOptions Options { get; }

        private NamedPipeServerStream? Stream { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

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

                _ = Task.Run(ListenForIncomingData);

                return true;
            }
            catch (Exception)
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
            catch (IOException)
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
                    byte[]? dataBuffer = await StreamFrameReader.ReadFrameAsync(Stream, lengthBuffer, Options.MaxMessageSize);
                    if (dataBuffer == null)
                        throw new WitExceptionTransport($"Server disconnected");

                    // Frames must reach the subscriber in read order -- the AEAD
                    // counter (and event ordering in general) depends on it. The
                    // subscriber only enqueues, so the read loop is not held up.
                    Callback(Id, dataBuffer);
                }
            }
            catch (Exception)
            {
                Dispose();
            }
        }

        #endregion

        #region IDisposable

        private int m_disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) != 0)
                return;

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

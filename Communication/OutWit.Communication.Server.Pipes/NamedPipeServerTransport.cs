using System;
using System.IO;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransport : ITransportServer
    {
        #region Events

        public event TransportDataEventHandler Callback
        {
            add => InboundBuffer.Subscribe(value);
            remove => InboundBuffer.Unsubscribe(value);
        }

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public NamedPipeServerTransport(NamedPipeServerTransportOptions options)
        {
            Id = Guid.NewGuid();
            InboundBuffer = new TransportInboundBuffer(Id);

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

                _ = Task.Run(ListenForIncomingData);

                return true;
            }
            catch (Exception)
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
            catch (IOException)
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
                    byte[]? dataBuffer = await StreamFrameReader.ReadFrameAsync(Stream, lengthBuffer, Options.MaxMessageSize);
                    if (dataBuffer == null)
                        throw new WitExceptionTransport($"Client disconnected");

                    InboundBuffer.Raise(dataBuffer);
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

        public bool CanReinitialize { get; } = false;

        private TransportInboundBuffer InboundBuffer { get; }

        private NamedPipeServerTransportOptions Options { get; }

        private NamedPipeServerStream? Stream { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

using System;
using System.IO;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Server.Tcp
{
    public abstract class TcpServerTransportBase<TOptions> : ITransportServer
        where TOptions : TcpServerTransportOptions
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

        protected TcpServerTransportBase(TcpClient? client, TOptions options)
        {
            Id = Guid.NewGuid();
            InboundBuffer = new TransportInboundBuffer(Id);

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
                    throw new WitExceptionTransport($"Failed to init tcp client");

                Stream = CreateStream();

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
            if (Client == null || Stream == null)
                return;

            var lengthBuffer = new byte[sizeof(int)];

            try
            {
                while (IsListening && Client.Connected)
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
            Client?.Close();

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public bool CanReinitialize { get; } = false;

        private TransportInboundBuffer InboundBuffer { get; }

        protected TOptions Options { get; }

        protected TcpClient? Client { get; }

        private Stream? Stream { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

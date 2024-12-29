using System;
using System.IO.Pipes;
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
        }

        #endregion

        #region Initialization

        private void InitPipe()
        {
            if (string.IsNullOrEmpty(Options.ServerName))
                throw new WitComExceptionTransport($"Failed to create pipe: server name is empty. " +
                                             $"Use \".\" as server name for local communication");

            if (string.IsNullOrEmpty(Options.PipeName))
                throw new WitComExceptionTransport($"Failed to create pipe: pipe name is empty");

            Stream = new NamedPipeClientStream(Options.ServerName, Options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);

            IsListening = true;
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
                    await Stream.ConnectAsync(timeout, cancellationToken);

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
            if (Stream == null || Writer == null || Reader == null)
                return;

            try
            {
                while (IsListening && Stream.IsConnected)
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

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        private NamedPipeClientTransportOptions Options { get; }

        private NamedPipeClientStream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

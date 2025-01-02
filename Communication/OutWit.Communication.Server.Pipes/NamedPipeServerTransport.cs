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
                throw new WitComExceptionTransport($"Failed to create pipe: pipe name is empty");

            Stream = new NamedPipeServerStream(Options.PipeName, PipeDirection.InOut,
                Options.MaxNumberOfClients, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
            IsListening = true;
        }

        #endregion

        #region ITransport

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                await Stream!.WaitForConnectionAsync(token);

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
                Writer.Flush();
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

        public bool CanReinitialize { get; } = false;

        private NamedPipeServerTransportOptions Options { get; }

        private NamedPipeServerStream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

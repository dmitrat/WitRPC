using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Server.MMF
{
    public class MemoryMappedFileServerTransport : ITransportServer
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public MemoryMappedFileServerTransport(MemoryMappedFileServerTransportOptions options)
        {
            Id = Guid.NewGuid();

            Options = options;

            InitChannel();

        }

        #endregion

        #region Initialization

        private void InitChannel()
        {
            if (string.IsNullOrEmpty(Options.Name))
                throw new WitComExceptionTransport($"Failed to create memory mapped file: name is empty");

            if (Options.Size <= 0)
                throw new WitComExceptionTransport($"Failed to create memory mapped file: size is zero");

            File = MemoryMappedFile.CreateNew(Options.Name, Options.Size, MemoryMappedFileAccess.ReadWrite);
            Stream = File.CreateViewStream(0, 0);
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);

            WaitForDataFromClient = new EventWaitHandle(false, EventResetMode.AutoReset, $"Global\\{Options.Name}_client");
            WaitForDataFromServer = new EventWaitHandle(false, EventResetMode.AutoReset, $"Global\\{Options.Name}_server");

            IsListening = true;
        }

        #endregion

        #region ITransport

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
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
                Stream?.Seek(0, SeekOrigin.Begin);

                Writer.Write(data.Length);
                Writer.Write(data);
                Writer.Flush();

                WaitForDataFromServer?.Set();

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
                while (IsListening && Stream != null)
                {
                    WaitForDataFromClient?.WaitOne();
                    if (!IsListening)
                        return;

                    Stream?.Seek(0, SeekOrigin.Begin);
                    int dataLength = Reader.ReadInt32();
                    if (dataLength > 0)
                    {
                        byte[] data = Reader.ReadBytes(dataLength);

                        _ = Task.Run(() => Callback(Id, data));
                    }
                }
            }
            catch (Exception ex)
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
            File?.Dispose();

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }
        
        public bool CanReinitialize { get; } = true;

        private MemoryMappedFileServerTransportOptions Options { get; }

        private MemoryMappedFile? File { get; set; }

        private MemoryMappedViewStream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private EventWaitHandle? WaitForDataFromClient { get; set; }

        private EventWaitHandle? WaitForDataFromServer { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

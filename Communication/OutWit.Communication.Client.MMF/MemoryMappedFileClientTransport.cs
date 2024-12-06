using System;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.MMF
{
    public class MemoryMappedFileClientTransport : ITransportClient
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public MemoryMappedFileClientTransport(MemoryMappedFileClientTransportOptions options)
        {
            Options = options;
        }

        #endregion

        #region Initialization

        private void InitChannel()
        {
            if (string.IsNullOrEmpty(Options.Name))
                throw new WitComExceptionTransport($"Failed to open memory mapped file: name is empty");

            File = MemoryMappedFile.OpenExisting(Options.Name, MemoryMappedFileRights.ReadWrite);
            Stream = File.CreateViewStream(0, 0);
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);

            WaitForDataFromClient = EventWaitHandle.OpenExisting($"Global\\{Options.Name}_client");
            WaitForDataFromServer = EventWaitHandle.OpenExisting($"Global\\{Options.Name}_server");

            IsListening = true;
        }

        #endregion

        #region ITransport

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                WaitForClient = Semaphore.OpenExisting($"Global\\{Options.Name}_connection");

                if (WaitForClient == null)
                    return false;

                var result = timeout == TimeSpan.Zero 
                    ? WaitForClient.WaitOne() 
                    : WaitForClient.WaitOne(timeout);

                if (!result)
                    return false;

                InitChannel();

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


        public async Task<bool> ReconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Dispose();
            InitChannel();

            return await ConnectAsync(timeout, cancellationToken);
        }

        public async Task<bool> ReconnectAsync(CancellationToken cancellationToken)
        {
            return await ReconnectAsync(TimeSpan.Zero, cancellationToken);
        }

        public async Task SendBytesAsync(byte[] data)
        {
            if (Writer == null || Reader == null)
                return;

            try
            {
                await Task.Run(() =>
                {
                    Stream?.Seek(0, SeekOrigin.Begin);

                    Writer.Write(data.Length);
                    Writer.Write(data);
                    Writer?.Flush();

                    WaitForDataFromClient?.Set();
                });
            }
            catch (IOException ex)
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
                while (IsListening && Stream!.CanRead)
                {
                    WaitForDataFromServer?.WaitOne();
                    if(!IsListening)
                        return;

                    Stream?.Seek(0, SeekOrigin.Begin);
                    int dataLength = Reader.ReadInt32();
                    if (dataLength > 0)
                    {
                        byte[] data = Reader.ReadBytes(dataLength);
                        Callback(Id, data);
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

            WaitForClient?.Release();
            WaitForClient = null;

            WaitForDataFromServer?.Set();
            WaitForDataFromServer = null;

            Disconnected(Id);


        }

        #endregion

        #region Properties

        public Guid Id { get; }

        private MemoryMappedFileClientTransportOptions Options { get; }

        private MemoryMappedFile? File { get; set; }

        private MemoryMappedViewStream? Stream { get; set; }

        private BinaryReader? Reader { get; set; }

        private BinaryWriter? Writer { get; set; }

        private EventWaitHandle? WaitForDataFromClient { get; set; }

        private EventWaitHandle? WaitForDataFromServer{ get; set; }

        private Semaphore? WaitForClient { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

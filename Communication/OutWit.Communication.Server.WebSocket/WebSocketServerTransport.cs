using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketServerTransport : ITransportServer
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

        public WebSocketServerTransport(System.Net.WebSockets.WebSocket? client, WebSocketServerTransportOptions options)
        {
            Id = Guid.NewGuid();
            InboundBuffer = new TransportInboundBuffer(Id);

            Client = client;
            Options = options;
        }

        #endregion

        #region ITransport

        public async Task<bool> InitializeConnectionAsync(CancellationToken token)
        {
            try
            {
                if (Client == null)
                    throw new WitExceptionTransport($"Failed to init tcp client");

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
            if (Client == null || Client.State != WebSocketState.Open)
                return;

            try
            {
                int chunkSize = Options.BufferSize;
                int offset = 0;

                while (offset < data.Length)
                {
                    int size = Math.Min(chunkSize, data.Length - offset);
                    bool isEndOfMessage = (offset + size) == data.Length;

                    await Client.SendAsync(
                        new ArraySegment<byte>(data, offset, size),
                        WebSocketMessageType.Binary,
                        isEndOfMessage,
                        CancellationToken.None
                    );

                    offset += size;
                }
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
            if (Client == null)
                return;

            var buffer = new byte[Options.BufferSize];
            using var memoryStream = new MemoryStream();

            try
            {
                while (IsListening && Client.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await Client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
                                CancellationToken.None);
                            Dispose();
                            return;
                        }

                        memoryStream.Write(buffer, 0, result.Count);

                        if (memoryStream.Length > Options.MaxMessageSize)
                            throw new WitExceptionTransport($"Incoming message exceeds the {Options.MaxMessageSize} byte limit");

                    } while (!result.EndOfMessage);

                    byte[] data = memoryStream.ToArray();
                    memoryStream.SetLength(0);

                    if (data.Length == 0)
                        continue;

                    InboundBuffer.Raise(data);
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

            Client?.Dispose();

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public bool CanReinitialize { get; } = false;

        private TransportInboundBuffer InboundBuffer { get; }

        private WebSocketServerTransportOptions Options { get; }

        private System.Net.WebSockets.WebSocket? Client { get; }

        private bool IsListening { get; set; }

        #endregion
    }
}

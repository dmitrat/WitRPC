using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.WebSocket
{
    public class WebSocketClientTransport : ITransportClient
    {
        #region Events

        public event TransportDataEventHandler Callback = delegate { };

        public event TransportEventHandler Disconnected = delegate { };

        #endregion

        #region Constructors

        public WebSocketClientTransport(WebSocketClientTransportOptions options)
        {
            Options = options;
            Address = $"{options.Url}";
        }

        #endregion

        #region ITransport

        public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Client = new ClientWebSocket();

            try
            {
                if (string.IsNullOrEmpty(Options.Url))
                    return false;

                if(timeout == TimeSpan.Zero || timeout == TimeSpan.MaxValue)
                    await Client.ConnectAsync(new Uri(Options.Url),  cancellationToken);
                else if (!Client.ConnectAsync(new Uri(Options.Url), cancellationToken).Wait(timeout, cancellationToken))
                    return false;

                IsListening = true;

                Task.Run(ListenForIncomingData);

                return true;
            }
            catch(Exception ex)
            {
                Dispose();
                return false;
            }
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            return await ConnectAsync(TimeSpan.Zero, cancellationToken);
        }

        public async Task SendBytesAsync(byte[] data)
        {
            if (Client == null || Client.State != WebSocketState.Open)
                return;

            try
            {
                const int chunkSize = 1024 * 4;
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

        public async Task Disconnect()
        {
            if (Client != null && Client.State == WebSocketState.Open)
                await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);

            Dispose();
        }

        #endregion

        #region Functions

        private async Task ListenForIncomingData()
        {
            if (Client == null)
                return;

            var buffer = new byte[1024 * 4];
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
                            Dispose();
                            return;
                        }

                        memoryStream.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    byte[] data = memoryStream.ToArray();
                    memoryStream.SetLength(0);
                    Callback(Id, data);
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

            Client?.Dispose();

            Disconnected(Id);
        }

        #endregion

        #region Properties

        public Guid Id { get; }

        public string? Address { get; }

        private WebSocketClientTransportOptions Options { get; }

        private ClientWebSocket? Client { get; set; }

        private bool IsListening { get; set; }

        #endregion
    }
}

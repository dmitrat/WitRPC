using OutWit.Communication.Client;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.LoadTests.Contracts;

namespace OutWit.Communication.LoadTests.Client
{
    /// <summary>
    /// A simulated node: one client connection, one proxy, an ack for every task it receives.
    /// </summary>
    public sealed class LoadNode : IAsyncDisposable
    {
        #region Fields

        private WitClient? m_client;

        private ILoadService? m_service;

        private long m_received;

        #endregion

        #region Constructors

        public LoadNode(int index, string endpoint, bool webSocket, string token, TimeSpan timeout)
        {
            Index = index;
            Endpoint = endpoint;
            WebSocket = webSocket;
            Token = token;
            Timeout = timeout;
        }

        #endregion

        #region Functions

        public async Task<bool> ConnectAsync(CancellationToken cancellation)
        {
            m_client = WitClientBuilder.Build(options =>
            {
                if (WebSocket)
                {
                    options.WithWebSocket(Endpoint);
                }
                else
                {
                    var parts = Endpoint.Split(':');
                    options.WithTcp(parts[0], int.Parse(parts[1]));
                }

                options.WithJson();
                options.WithEncryption();
                options.WithAccessToken(Token);
                options.WithTimeout(Timeout);
            });

            if (!await m_client.ConnectAsync(Timeout, cancellation).ConfigureAwait(false))
                return false;

            m_service = m_client.GetService<ILoadService>();
            m_service.TaskReceived += OnTaskReceived;
            ConnectionId = m_service.Attach(Index);
            return true;
        }

        private void OnTaskReceived(LoadTask task)
        {
            Interlocked.Increment(ref m_received);

            // A broadcast reaches every node; only the addressee answers.
            if (task.NodeIndex != Index)
                return;

            _ = AckAsync(task);
        }

        private async Task AckAsync(LoadTask task)
        {
            try
            {
                if (m_service != null)
                    await m_service.AckAsync(task.Id, Index).ConfigureAwait(false);
            }
            catch
            {
                // A node that lost its connection cannot ack; the harness counts the gap.
            }
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (m_client == null)
                return;

            try
            {
                await m_client.Disconnect().ConfigureAwait(false);
            }
            catch
            {
                // Tearing down; nothing to report.
            }

            m_client.Dispose();
        }

        #endregion

        #region Properties

        public int Index { get; }

        public string Endpoint { get; }

        public bool WebSocket { get; }

        public string Token { get; }

        public TimeSpan Timeout { get; }

        public Guid ConnectionId { get; private set; }

        public long Received => Interlocked.Read(ref m_received);

        #endregion
    }
}

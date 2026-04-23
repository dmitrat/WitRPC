using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.WebSocket;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Server.WebSocket;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Tests.Communication
{
    [TestFixture]
    public sealed class WitClientIncomingPayloadTests
    {
        [Test]
        public void EmptyIncomingPayloadIsIgnoredTest()
        {
            using var context = CreateClientContext();

            context.Transport.RaiseDataReceived(Array.Empty<byte>());

            Assert.That(WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "Ignoring empty incoming payload")), Is.True);
        }

        [Test]
        public void NullDeserializedIncomingMessageIsIgnoredTest()
        {
            using var context = CreateClientContext(new TestMessageSerializer
            {
                OnDeserializeGeneric = (bytes, type) => type == typeof(WitMessage) ? null : null
            });

            context.Transport.RaiseDataReceived(new byte[] { 42 });

            Assert.That(WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "could not be deserialized into a message")), Is.True);
        }

        [Test]
        public void MalformedIncomingPayloadIsLoggedAndIgnoredTest()
        {
            using var context = CreateClientContext(new TestMessageSerializer
            {
                OnDeserializeGeneric = (bytes, type) => throw new InvalidOperationException("Broken payload")
            });

            context.Transport.RaiseDataReceived(new byte[] { 13, 37 });

            Assert.That(WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "Failed to process incoming payload")), Is.True);
        }

        [Test]
        public async Task WebSocketClientTransportIgnoresEmptyFramesTest()
        {
            var port = GetAvailablePort();
            var prefix = $"http://localhost:{port}/client-empty/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            using var transport = new WebSocketClientTransport(new WebSocketClientTransportOptions
            {
                Url = $"ws://localhost:{port}/client-empty/",
                BufferSize = 1024
            });

            var callbackCount = 0;
            transport.Callback += (_, _) => Interlocked.Increment(ref callbackCount);

            var serverTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                await webSocketContext.WebSocket.SendAsync(ArraySegment<byte>.Empty, WebSocketMessageType.Binary, true, CancellationToken.None);
                await Task.Delay(200);
                webSocketContext.WebSocket.Dispose();
            });

            Assert.That(await transport.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.True);
            await Task.WhenAll(serverTask);
            await Task.Delay(200);

            Assert.That(callbackCount, Is.EqualTo(0));
            await transport.Disconnect();
        }

        [Test]
        public async Task WebSocketServerTransportIgnoresEmptyFramesTest()
        {
            using var transport = new WebSocketServerTransport(new FakeWebSocket(
                new WebSocketReceiveResult(0, WebSocketMessageType.Binary, true),
                new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)), new WebSocketServerTransportOptions
            {
                Host = (HostInfo?)"http://localhost/server-empty/",
                BufferSize = 1024,
                MaxNumberOfClients = 1
            });

            var callbackCount = 0;
            transport.Callback += (_, _) => Interlocked.Increment(ref callbackCount);

            Assert.That(await transport.InitializeConnectionAsync(CancellationToken.None), Is.True);
            await Task.Delay(200);

            Assert.That(callbackCount, Is.EqualTo(0));
        }

        private static ClientTestContext CreateClientContext(TestMessageSerializer? serializer = null)
        {
            var transport = new TestTransportClient();
            var logger = new TestLogger();
            var messageSerializer = serializer ?? new TestMessageSerializer();

            var client = new WitClient(
                transport,
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(string.Empty),
                new MessageSerializerJson(),
                messageSerializer,
                logger,
                timeout: null);

            return new ClientTestContext(client, transport, logger);
        }

        private static int GetAvailablePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        private static bool WaitUntil(Func<bool> condition)
        {
            return SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(1));
        }

        private sealed class ClientTestContext : IDisposable
        {
            public ClientTestContext(WitClient client, TestTransportClient transport, TestLogger logger)
            {
                Client = client;
                Transport = transport;
                Logger = logger;
            }

            public WitClient Client { get; }

            public TestTransportClient Transport { get; }

            public TestLogger Logger { get; }

            public void Dispose()
            {
                Client.Dispose();
            }
        }

        private sealed class TestTransportClient : ITransportClient
        {
            public event TransportDataEventHandler Callback = delegate { };

            public event TransportEventHandler Disconnected = delegate { };

            public Guid Id { get; } = Guid.NewGuid();

            public string? Address => "test://client";

            public Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }

            public Task<bool> ConnectAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }

            public Task Disconnect()
            {
                return Task.CompletedTask;
            }

            public Task SendBytesAsync(byte[] data)
            {
                return Task.CompletedTask;
            }

            public void RaiseDataReceived(byte[] data)
            {
                Callback(Guid.NewGuid(), data);
            }

            public void Dispose()
            {
            }
        }

        private sealed class TestMessageSerializer : IMessageSerializer
        {
            public Func<byte[], Type, object?>? OnDeserializeGeneric { get; set; }

            public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
            {
                return Array.Empty<byte>();
            }

            public byte[] Serialize(object message, Type type, ILogger? logger = null)
            {
                return Array.Empty<byte>();
            }

            public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T : class
            {
                return (T?)OnDeserializeGeneric?.Invoke(bytes, typeof(T));
            }

            public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
            {
                return OnDeserializeGeneric?.Invoke(bytes, type);
            }
        }

        private sealed class TestLogger : ILogger
        {
            private readonly ConcurrentQueue<(LogLevel LogLevel, string Message)> m_entries = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                m_entries.Enqueue((logLevel, formatter(state, exception)));
            }

            public bool Contains(LogLevel logLevel, string messagePart)
            {
                return m_entries.Any(x => x.LogLevel == logLevel && x.Message.Contains(messagePart, StringComparison.Ordinal));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }

        private sealed class FakeWebSocket : WebSocket
        {
            private readonly Queue<WebSocketReceiveResult> m_results;

            private WebSocketCloseStatus? m_closeStatus;

            private WebSocketState m_state = WebSocketState.Open;

            public FakeWebSocket(params WebSocketReceiveResult[] results)
            {
                m_results = new Queue<WebSocketReceiveResult>(results);
            }

            public override WebSocketCloseStatus? CloseStatus => m_closeStatus;

            public override string? CloseStatusDescription { get; }

            public override WebSocketState State => m_state;

            public override string? SubProtocol => null;

            public override void Abort()
            {
                m_state = WebSocketState.Aborted;
            }

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            {
                m_closeStatus = closeStatus;
                m_state = WebSocketState.Closed;
                return Task.CompletedTask;
            }

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            {
                m_closeStatus = closeStatus;
                m_state = WebSocketState.CloseSent;
                return Task.CompletedTask;
            }

            public override void Dispose()
            {
                m_state = WebSocketState.Closed;
            }

            public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                if (m_results.Count == 0)
                    return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

                var result = m_results.Dequeue();
                if (result.MessageType == WebSocketMessageType.Close)
                    m_state = WebSocketState.CloseReceived;

                return Task.FromResult(result);
            }

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}

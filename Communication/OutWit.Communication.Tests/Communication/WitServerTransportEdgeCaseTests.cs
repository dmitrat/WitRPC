using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Tests.Communication
{
    [TestFixture]
    public sealed class WitServerTransportEdgeCaseTests
    {
        #region Transport Edge Tests

        [Test]
        public void LateRequestFrameAfterDisconnectIsIgnoredTest()
        {
            using var context = CreateContext();

            context.Transport.RaiseDisconnected();
            context.Transport.RaiseDataReceived(CreateRequestFrame(context.MessageSerializer, "LateRequest"));

            Assert.That(
                WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "Ignoring message for disconnected or unknown client")),
                Is.True);
            Assert.That(context.Transport.SentData.Count, Is.EqualTo(0));
        }

        [Test]
        public void InitializationFrameAfterDisconnectIsIgnoredTest()
        {
            using var context = CreateContext();

            context.Transport.RaiseDisconnected();
            context.Transport.RaiseDataReceived(CreateInitializationFrame(context.MessageSerializer));

            Assert.That(
                WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "Ignoring message for disconnected or unknown client")),
                Is.True);
            Assert.That(context.Transport.SentData.Count, Is.EqualTo(0));
        }

        [Test]
        public void AuthorizationFrameAfterDisconnectIsIgnoredTest()
        {
            using var context = CreateContext();

            context.Transport.RaiseDisconnected();
            context.Transport.RaiseDataReceived(CreateAuthorizationFrame(context.MessageSerializer));

            Assert.That(
                WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "Ignoring message for disconnected or unknown client")),
                Is.True);
            Assert.That(context.Transport.SentData.Count, Is.EqualTo(0));
        }

        [Test]
        public void TransportBoundaryFailureDoesNotBlockNextMessageTest()
        {
            var shouldThrow = true;
            using var context = CreateContext(request =>
            {
                if (shouldThrow)
                {
                    shouldThrow = false;
                    throw new InvalidOperationException("Simulated request processor failure");
                }

                return Task.FromResult(WitResponse.Success(Array.Empty<byte>()));
            });

            context.Transport.RaiseDataReceived(CreateRequestFrame(context.MessageSerializer, "FirstRequest"));

            Assert.That(WaitUntil(() => context.RequestProcessor.ProcessCallCount == 1), Is.True);
            Assert.That(context.Transport.SentData.Count, Is.EqualTo(0));

            context.Transport.RaiseDataReceived(CreateRequestFrame(context.MessageSerializer, "SecondRequest"));

            Assert.That(WaitUntil(() => context.Transport.SentData.Count == 1), Is.True);
            Assert.That(context.RequestProcessor.ProcessCallCount, Is.EqualTo(2));
        }

        #endregion

        #region Helpers

        private static TestContext CreateContext(Func<WitRequest?, Task<WitResponse>>? processRequest = null)
        {
            var messageSerializer = new MessageSerializerMemoryPack();
            var logger = new TestLogger();
            var factory = new TestTransportServerFactory();
            var requestProcessor = new TestRequestProcessor(processRequest);

            var server = new WitServer(
                factory,
                new EncryptorServerFactory<EncryptorServerPlain>(),
                new AccessTokenValidatorPlain(),
                new MessageSerializerJson(),
                messageSerializer,
                requestProcessor,
                discoveryServer: null,
                logger,
                timeout: null,
                name: null,
                description: null);

            var transport = new TestTransportServer();
            factory.ConnectClient(transport);

            return new TestContext(server, transport, logger, messageSerializer, requestProcessor);
        }

        private static byte[] CreateAuthorizationFrame(IMessageSerializer messageSerializer)
        {
            return CreateFrame(
                messageSerializer,
                WitMessageType.Authorization,
                new WitRequestAuthorization
                {
                    Token = string.Empty
                });
        }

        private static byte[] CreateInitializationFrame(IMessageSerializer messageSerializer)
        {
            return CreateFrame(
                messageSerializer,
                WitMessageType.Initialization,
                new WitRequestInitialization
                {
                    PublicKey = new byte[] { 1, 2, 3 }
                });
        }

        private static byte[] CreateRequestFrame(IMessageSerializer messageSerializer, string methodName)
        {
            return CreateFrame(
                messageSerializer,
                WitMessageType.Request,
                new WitRequest
                {
                    Token = string.Empty,
                    MethodName = methodName
                });
        }

        private static byte[] CreateFrame(IMessageSerializer messageSerializer, WitMessageType type, object payload)
        {
            return messageSerializer.Serialize(new WitMessage
            {
                Id = Guid.NewGuid(),
                Type = type,
                Data = messageSerializer.Serialize(payload, payload.GetType())
            });
        }

        private static bool WaitUntil(Func<bool> condition)
        {
            return WaitUntil(condition, TimeSpan.FromSeconds(1));
        }

        private static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            return SpinWait.SpinUntil(condition, timeout);
        }

        private sealed class TestContext : IDisposable
        {
            #region Constructors

            public TestContext(
                WitServer server,
                TestTransportServer transport,
                TestLogger logger,
                IMessageSerializer messageSerializer,
                TestRequestProcessor requestProcessor)
            {
                Server = server;
                Transport = transport;
                Logger = logger;
                MessageSerializer = messageSerializer;
                RequestProcessor = requestProcessor;
            }

            #endregion

            #region IDisposable

            public void Dispose()
            {
                Server.Dispose();
            }

            #endregion

            #region Properties

            public TestLogger Logger { get; }

            public IMessageSerializer MessageSerializer { get; }

            public TestRequestProcessor RequestProcessor { get; }

            public WitServer Server { get; }

            public TestTransportServer Transport { get; }

            #endregion
        }

        private sealed class TestLogger : ILogger
        {
            #region Fields

            private readonly ConcurrentQueue<TestLogEntry> m_entries = new();

            #endregion

            #region ILogger

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                m_entries.Enqueue(new TestLogEntry(logLevel, formatter(state, exception), exception));
            }

            #endregion

            #region Functions

            public bool Contains(LogLevel logLevel, string messagePart)
            {
                return m_entries.Any(entry => entry.LogLevel == logLevel && entry.Message.Contains(messagePart, StringComparison.Ordinal));
            }

            #endregion
        }

        private sealed record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception);

        private sealed class TestRequestProcessor : IRequestProcessor
        {
            #region Fields

            private readonly Func<WitRequest?, Task<WitResponse>> m_processRequest;

            #endregion

            #region Constructors

            public TestRequestProcessor(Func<WitRequest?, Task<WitResponse>>? processRequest = null)
            {
                m_processRequest = processRequest ?? (_ => Task.FromResult(WitResponse.Success(Array.Empty<byte>())));
            }

            #endregion

            #region IRequestProcessor

            public event RequestProcessorEventHandler Callback = delegate { };

            public async Task<WitResponse> Process(WitRequest? request)
            {
                ProcessCallCount++;
                return await m_processRequest(request);
            }

            public void ResetSerializer(IMessageSerializer serializer)
            {
            }

            #endregion

            #region Properties

            public int ProcessCallCount { get; private set; }

            #endregion
        }

        private sealed class TestServerOptions : IServerOptions
        {
            #region Properties

            public Dictionary<string, string> Data { get; } = new();

            public string Transport => "Test";

            #endregion
        }

        private sealed class TestTransportServer : ITransportServer
        {
            #region Fields

            private readonly ConcurrentQueue<byte[]> m_sentData = new();

            #endregion

            #region ITransport

            public event TransportDataEventHandler Callback = delegate { };

            public event TransportEventHandler Disconnected = delegate { };

            public async Task SendBytesAsync(byte[] data)
            {
                m_sentData.Enqueue(data);
            }

            public Guid Id { get; } = Guid.NewGuid();

            #endregion

            #region ITransportServer

            public bool CanReinitialize => false;

            public Task<bool> InitializeConnectionAsync(CancellationToken token)
            {
                return Task.FromResult(true);
            }

            #endregion

            #region IDisposable

            public void Dispose()
            {
            }

            #endregion

            #region Functions

            public void RaiseDataReceived(byte[] data)
            {
                Callback(Id, data);
            }

            public void RaiseDisconnected()
            {
                Disconnected(Id);
            }

            #endregion

            #region Properties

            public IReadOnlyCollection<byte[]> SentData => m_sentData.ToArray();

            #endregion
        }

        private sealed class TestTransportServerFactory : ITransportServerFactory
        {
            #region ITransportServerFactory

            public event TransportFactoryEventHandler NewClientConnected = delegate { };

            public void StartWaitingForConnection(ILogger? logger)
            {
            }

            public void StopWaitingForConnection()
            {
            }

            public IServerOptions Options { get; } = new TestServerOptions();

            #endregion

            #region Functions

            public void ConnectClient(ITransportServer transport)
            {
                NewClientConnected(transport);
            }

            #endregion
        }

        private sealed class NullScope : IDisposable
        {
            #region Constructors

            private NullScope()
            {
            }

            #endregion

            #region Static

            public static NullScope Instance { get; } = new();

            #endregion

            #region IDisposable

            public void Dispose()
            {
            }

            #endregion
        }

        #endregion
    }
}

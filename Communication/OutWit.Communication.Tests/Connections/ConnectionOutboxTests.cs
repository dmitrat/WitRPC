using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Callbacks;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Tests.Mock.Transports;

namespace OutWit.Communication.Tests.Connections
{
    /// <summary>
    /// The one outbound queue per connection (3.2): frames leave in the order they were
    /// enqueued, a callback raised inside a handler precedes that handler's response, a
    /// connection whose transport is stuck holds only its own queue, and the per-connection
    /// bounds and the overflow policy do what they say. Observed on the transport's byte
    /// sequence, not through a client.
    /// </summary>
    [TestFixture]
    public sealed class ConnectionOutboxTests
    {
        #region Constants

        private static readonly TimeSpan WAIT = TimeSpan.FromSeconds(5);

        #endregion

        #region Ordering Tests

        [Test]
        public void CallbackRaisedInsideAHandlerPrecedesTheResponseTest()
        {
            using var context = CreateContext(raiseCallbackInsideHandler: true);
            var transport = context.Connect();

            transport.RaiseDataReceived(context.RequestFrame("Work"));

            Assert.That(WaitUntil(() => transport.WrittenFrames == 4), Is.True, "handshake x2, callback, response");

            var types = transport.Sent.Select(context.TypeOf).ToArray();
            Assert.That(types, Is.EqualTo(new[]
            {
                WitMessageType.Initialization, WitMessageType.Authorization, WitMessageType.Callback, WitMessageType.Request
            }));
        }

        [Test]
        public void OneThousandCallbacksKeepTheirOrderTest()
        {
            using var context = CreateContext();
            var transport = context.Connect();

            for (var i = 0; i < 1000; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });

            Assert.That(WaitUntil(() => transport.WrittenFrames == 1002), Is.True);

            var names = transport.Sent.Skip(2).Select(context.CallbackNameOf).ToArray();
            Assert.That(names, Is.EqualTo(Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArray()));
        }

        [Test]
        public void ResponsesAndCallbacksShareOneOrderTest()
        {
            using var context = CreateContext();
            var transport = context.Connect();

            // A request whose response is enqueued while callbacks are still flowing in
            // behind it: the response keeps its place, nothing overtakes it.
            transport.RaiseDataReceived(context.RequestFrame("First"));
            Assert.That(WaitUntil(() => transport.WrittenFrames == 3), Is.True);

            transport.Block();
            context.Processor.InvokeCallback(new WitRequest { MethodName = "a" });
            transport.RaiseDataReceived(context.RequestFrame("Second"));
            Assert.That(WaitUntil(() => context.Processor.ProcessCalls == 2), Is.True);
            context.Processor.InvokeCallback(new WitRequest { MethodName = "b" });
            transport.Release();

            Assert.That(WaitUntil(() => transport.WrittenFrames == 6), Is.True);

            var tail = transport.Sent.Skip(3).Select(context.TypeOf).ToArray();
            Assert.That(tail, Is.EqualTo(new[] { WitMessageType.Callback, WitMessageType.Request, WitMessageType.Callback }));
        }

        #endregion

        #region Isolation Tests

        [Test]
        public void SlowConnectionDoesNotDelayItsNeighbourTest()
        {
            using var context = CreateContext();
            var slow = context.Connect();
            var neighbour = context.Connect();

            slow.Block();

            for (var i = 0; i < 50; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });

            // The neighbour gets all fifty while the slow one still holds its first write.
            Assert.That(WaitUntil(() => neighbour.WrittenFrames == 52), Is.True, "the neighbour must not wait for the slow connection");
            Assert.That(slow.WrittenFrames, Is.EqualTo(2), "the slow connection is stuck on its first callback");
            Assert.That(context.Server.AuthorizedConnectionCount, Is.EqualTo(2));
            Assert.That(slow.WaitForBlockedWrites(1, WAIT), Is.True);

            var pending = context.PendingCallbacksOf(slow);
            Assert.That(pending, Is.EqualTo(49), "one callback is inside the blocked write, the rest wait in the queue");

            slow.Release();
            Assert.That(WaitUntil(() => slow.WrittenFrames == 52), Is.True);
            Assert.That(context.PendingCallbacksOf(slow), Is.EqualTo(0));
        }

        [Test]
        public void DisconnectDuringASendDropsTheQueueQuietlyTest()
        {
            using var context = CreateContext();
            var slow = context.Connect();
            var neighbour = context.Connect();

            slow.Block();
            for (var i = 0; i < 10; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });

            Assert.That(WaitUntil(() => neighbour.WrittenFrames == 12), Is.True);

            // The stuck client goes away: nothing hangs, nothing throws, the neighbour keeps working.
            slow.Dispose();
            Assert.That(WaitUntil(() => context.Server.AuthorizedConnectionCount == 1), Is.True);

            context.Processor.InvokeCallback(new WitRequest { MethodName = "after" });
            Assert.That(WaitUntil(() => neighbour.WrittenFrames == 13), Is.True);
        }

        #endregion

        #region Bound Tests

        [Test]
        public void QueueFullClosesOnlyTheOffendingConnectionTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions
            {
                MaxPendingCallbacks = 2,
                OverflowPolicy = CallbackOverflowPolicy.CloseConnection
            });
            var slow = context.Connect();
            var neighbour = context.Connect();

            slow.Block();

            // 1 inside the blocked write, 2 in the queue, the 4th is one too many. The raises
            // are paced by the neighbour's writes: the bound counts frames not yet written, and
            // an idle connection must not trip it just because a burst outran its writer.
            context.Processor.InvokeCallback(new WitRequest { MethodName = "0" });
            Assert.That(slow.WaitForBlockedWrites(1, WAIT), Is.True, "the first callback reached the transport and blocked");
            for (var i = 1; i < 4; i++)
            {
                Assert.That(WaitUntil(() => neighbour.WrittenFrames == 2 + i), Is.True);
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });
            }

            Assert.That(WaitUntil(() => slow.IsDisposed), Is.True, "the slow connection is closed by the policy");
            Assert.That(WaitUntil(() => neighbour.WrittenFrames == 6), Is.True, "the neighbour gets all four");
            Assert.That(WaitUntil(() => context.Server.AuthorizedConnectionCount == 1), Is.True);
            Assert.That(context.Logger.Contains(LogLevel.Warning, "callback queue full"), Is.True);
        }

        [Test]
        public void QueueFullLogsAndKeepsQueuingByDefaultTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions
            {
                MaxPendingCallbacks = 2
            });
            var slow = context.Connect();

            slow.Block();
            context.Processor.InvokeCallback(new WitRequest { MethodName = "0" });
            Assert.That(slow.WaitForBlockedWrites(1, WAIT), Is.True, "the first callback reached the transport and blocked");
            for (var i = 1; i < 6; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });

            Thread.Sleep(200);
            Assert.That(slow.IsDisposed, Is.False, "the default policy never closes");
            Assert.That(context.PendingCallbacksOf(slow), Is.EqualTo(5));
            Assert.That(context.Logger.Contains(LogLevel.Warning, "passed its bound"), Is.True);

            slow.Release();
            Assert.That(WaitUntil(() => slow.WrittenFrames == 8), Is.True, "everything queued is delivered");
        }

        [Test]
        public void DropNewestDropsBeyondTheBoundTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions
            {
                MaxPendingCallbacks = 2,
                OverflowPolicy = CallbackOverflowPolicy.DropNewest
            });
            var slow = context.Connect();

            slow.Block();
            context.Processor.InvokeCallback(new WitRequest { MethodName = "0" });
            Assert.That(slow.WaitForBlockedWrites(1, WAIT), Is.True, "the first callback reached the transport and blocked");
            for (var i = 1; i < 6; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });

            Assert.That(context.PendingCallbacksOf(slow), Is.EqualTo(2));
            slow.Release();

            Assert.That(WaitUntil(() => slow.WrittenFrames == 5), Is.True, "handshake x2, the blocked one, the two queued");
            Thread.Sleep(100);
            Assert.That(slow.WrittenFrames, Is.EqualTo(5));
            Assert.That(slow.IsDisposed, Is.False);

            var names = slow.Sent.Skip(2).Select(context.CallbackNameOf).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "0", "1", "2" }), "the oldest survive, the newest are dropped");
        }

        [Test]
        public void ByteBudgetCountsFrameSizesTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions
            {
                MaxPendingCallbackBytes = 1024,
                OverflowPolicy = CallbackOverflowPolicy.DropNewest
            });
            var slow = context.Connect();

            slow.Block();
            var big = new string('x', 600);
            context.Processor.InvokeCallback(new WitRequest { MethodName = big + 0 });
            Assert.That(slow.WaitForBlockedWrites(1, WAIT), Is.True, "the first callback reached the transport and blocked");
            for (var i = 1; i < 5; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = big + i });

            // One frame is inside the blocked write; the queue holds one more 600+ byte frame
            // before the second would exceed 1024 bytes.
            Assert.That(context.PendingCallbacksOf(slow), Is.EqualTo(1));
            slow.Release();
            Assert.That(WaitUntil(() => slow.WrittenFrames == 4), Is.True);
        }

        [Test]
        public void ResponsesAreNeverCountedOrDroppedTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions
            {
                MaxPendingCallbacks = 1,
                OverflowPolicy = CallbackOverflowPolicy.DropNewest
            });
            var transport = context.Connect();

            transport.Block();
            for (var i = 0; i < 5; i++)
                transport.RaiseDataReceived(context.RequestFrame($"Request{i}"));

            // The connection loop handles one request at a time and waits for its response
            // to be written, so only one response is queued at once; none is refused.
            context.Processor.InvokeCallback(new WitRequest { MethodName = "c1" });
            context.Processor.InvokeCallback(new WitRequest { MethodName = "c2" });
            transport.Release();

            Assert.That(WaitUntil(() => transport.WrittenFrames == 8), Is.True, "handshake x2, five responses, one callback");
            Assert.That(transport.Sent.Select(context.TypeOf).Count(type => type == WitMessageType.Request), Is.EqualTo(5));
        }

        #endregion

        #region Timeout Tests

        [Test]
        public void SendTimeoutClosesTheConnectionUnderClosePolicyTest()
        {
            using var context = CreateContext(
                options: new CallbackDeliveryOptions { OverflowPolicy = CallbackOverflowPolicy.CloseConnection },
                timeout: TimeSpan.FromMilliseconds(200));
            var slow = context.Connect();

            slow.Block();
            context.Processor.InvokeCallback(new WitRequest { MethodName = "stuck" });

            Assert.That(WaitUntil(() => slow.IsDisposed), Is.True, "a write that does not finish within the timeout closes the connection");
            Assert.That(context.Logger.Contains(LogLevel.Warning, "timed out"), Is.True);
        }

        [Test]
        public void SendTimeoutOnlyWarnsByDefaultTest()
        {
            using var context = CreateContext(timeout: TimeSpan.FromMilliseconds(200));
            var slow = context.Connect();

            slow.Block();
            context.Processor.InvokeCallback(new WitRequest { MethodName = "stuck" });

            Assert.That(WaitUntil(() => context.Logger.Contains(LogLevel.Warning, "timed out")), Is.True);
            Thread.Sleep(200);
            Assert.That(slow.IsDisposed, Is.False, "the default policy warns and waits");

            slow.Release();
            Assert.That(WaitUntil(() => slow.WrittenFrames == 3), Is.True);
        }

        #endregion

        #region Lifecycle Tests

        [Test]
        public void DisposeWithPendingSendsDoesNotThrowTest()
        {
            var context = CreateContext();
            var slow = context.Connect();

            slow.Block();
            for (var i = 0; i < 20; i++)
                context.Processor.InvokeCallback(new WitRequest { MethodName = i.ToString() });

            Assert.DoesNotThrow(() => context.Dispose());
            Assert.That(WaitUntil(() => slow.IsDisposed), Is.True);
        }

        [Test]
        public void TargetedOnlyServerRefusesABroadcastTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions { Mode = CallbackDeliveryMode.TargetedOnly });
            var transport = context.Connect();

            context.Processor.InvokeCallback(new WitRequest { MethodName = "untargeted" });
            Thread.Sleep(200);

            Assert.That(transport.WrittenFrames, Is.EqualTo(2), "nothing beyond the handshake");
            Assert.That(context.Logger.Contains(LogLevel.Error, "without a target"), Is.True);

            using (CallbackScope.Target(transport.Id))
                context.Processor.InvokeCallback(new WitRequest { MethodName = "targeted" });

            Assert.That(WaitUntil(() => transport.WrittenFrames == 3), Is.True);
        }

        [Test]
        public void ScopeReportsPerConnectionOutcomesTest()
        {
            using var context = CreateContext(options: new CallbackDeliveryOptions
            {
                MaxPendingCallbacks = 1,
                OverflowPolicy = CallbackOverflowPolicy.DropNewest
            });
            var first = context.Connect();
            var second = context.Connect();
            var unknown = Guid.NewGuid();

            // The second connection ends up with one callback stuck in its write and one in its
            // queue (the bound); the first has written everything before the probe.
            second.Block();
            context.Processor.InvokeCallback(new WitRequest { MethodName = "fill" });
            Assert.That(second.WaitForBlockedWrites(1, WAIT), Is.True);
            Assert.That(WaitUntil(() => context.PendingCallbacksOf(first) == 0), Is.True);
            context.Processor.InvokeCallback(new WitRequest { MethodName = "fill" });
            Assert.That(WaitUntil(() => context.PendingCallbacksOf(first) == 0), Is.True);
            Assert.That(context.PendingCallbacksOf(second), Is.EqualTo(1));

            CallbackDeliveryReport report;
            using (var scope = CallbackScope.Target(new[] { first.Id, second.Id, unknown }))
            {
                context.Processor.InvokeCallback(new WitRequest { MethodName = "probe" });
                second.Release();
                report = scope.Completion.GetAwaiter().GetResult();
            }

            Assert.That(report.Raised, Is.EqualTo(1));
            Assert.That(report.Outcomes.Single(outcome => outcome.ConnectionId == first.Id).Status, Is.EqualTo(CallbackDeliveryStatus.Sent));
            Assert.That(report.Outcomes.Single(outcome => outcome.ConnectionId == second.Id).Status, Is.EqualTo(CallbackDeliveryStatus.QueueFull));
            Assert.That(report.Outcomes.Single(outcome => outcome.ConnectionId == unknown).Status, Is.EqualTo(CallbackDeliveryStatus.UnknownConnection));
            Assert.That(report.Outcomes.Select(outcome => outcome.ServerId).Distinct().Single(), Is.EqualTo(context.Server.Id));
        }

        #endregion

        #region Helpers

        private static TestContext CreateContext(bool raiseCallbackInsideHandler = false, CallbackDeliveryOptions? options = null, TimeSpan? timeout = null)
        {
            var messageSerializer = new MessageSerializerMemoryPack();
            var logger = new CapturingLogger();
            var factory = new MockTransportServerFactory();
            var processor = new CallbackProcessor(raiseCallbackInsideHandler);

            var server = new WitServer(
                factory,
                new EncryptorServerFactory<EncryptorServerPlain>(),
                new AccessTokenValidatorPlain(),
                new MessageSerializerJson(),
                messageSerializer,
                processor,
                discoveryServer: null,
                logger,
                timeout,
                name: null,
                description: null,
                int.MaxValue,
                handshakeTimeout: null,
                options);

            return new TestContext(server, factory, processor, logger, messageSerializer);
        }

        private static bool WaitUntil(Func<bool> condition)
        {
            return SpinWait.SpinUntil(condition, WAIT);
        }

        private sealed class TestContext : IDisposable
        {
            public TestContext(WitServer server, MockTransportServerFactory factory, CallbackProcessor processor, CapturingLogger logger, IMessageSerializer messageSerializer)
            {
                Server = server;
                Factory = factory;
                Processor = processor;
                Logger = logger;
                MessageSerializer = messageSerializer;
            }

            public WitServer Server { get; }

            public MockTransportServerFactory Factory { get; }

            public CallbackProcessor Processor { get; }

            public CapturingLogger Logger { get; }

            public IMessageSerializer MessageSerializer { get; }

            /// <summary>
            /// Connects a transport and drives it through the handshake.
            /// </summary>
            public MockTransportServer Connect()
            {
                var transport = Factory.Connect();

                transport.RaiseDataReceived(Frame(WitMessageType.Initialization, new WitRequestInitialization
                {
                    PublicKey = new byte[] { 1, 2, 3 },
                    ProtocolVersion = WitProtocol.VERSION
                }));
                transport.RaiseDataReceived(Frame(WitMessageType.Authorization, new WitRequestAuthorization { Token = string.Empty }));

                Assert.That(WaitUntil(() => transport.WrittenFrames == 2), Is.True, "handshake");
                return transport;
            }

            public byte[] RequestFrame(string methodName)
            {
                return Frame(WitMessageType.Request, new WitRequest { Token = string.Empty, MethodName = methodName });
            }

            public WitMessageType TypeOf(byte[] frame)
            {
                return MessageSerializer.Deserialize<WitMessage>(frame)!.Type;
            }

            public string CallbackNameOf(byte[] frame)
            {
                var message = MessageSerializer.Deserialize<WitMessage>(frame)!;
                Assert.That(message.Type, Is.EqualTo(WitMessageType.Callback));
                return MessageSerializer.Deserialize<WitRequest>(message.Data!)!.MethodName;
            }

            public long PendingCallbacksOf(MockTransportServer transport)
            {
                return Server.GetPendingCallbacks(transport.Id);
            }

            private byte[] Frame(WitMessageType type, object payload)
            {
                return MessageSerializer.Serialize(new WitMessage
                {
                    Id = Guid.NewGuid(),
                    Type = type,
                    Data = MessageSerializer.Serialize(payload, payload.GetType())
                });
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private sealed class CallbackProcessor : IRequestProcessor
        {
            private readonly bool m_raiseInsideHandler;

            private int m_processCalls;

            private int m_raised;

            public CallbackProcessor(bool raiseInsideHandler)
            {
                m_raiseInsideHandler = raiseInsideHandler;
            }

            public event RequestProcessorEventHandler Callback = delegate { };

            public Task<WitResponse> Process(WitRequest? request)
            {
                Interlocked.Increment(ref m_processCalls);

                if (m_raiseInsideHandler)
                    InvokeCallback(new WitRequest { MethodName = "inside" });

                return Task.FromResult(WitResponse.Success(Array.Empty<byte>()));
            }

            public void ResetSerializer(IMessageSerializer serializer)
            {
            }

            public void InvokeCallback(WitRequest request)
            {
                Interlocked.Increment(ref m_raised);
                Callback(request);
            }

            public int ProcessCalls => Volatile.Read(ref m_processCalls);

            public int RaisedCallbacks => Volatile.Read(ref m_raised);
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<(LogLevel Level, string Message)> m_entries = new();

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
                lock (m_entries)
                    m_entries.Add((logLevel, formatter(state, exception)));
            }

            public bool Contains(LogLevel level, string part)
            {
                lock (m_entries)
                    return m_entries.Any(entry => entry.Level == level && entry.Message.Contains(part, StringComparison.OrdinalIgnoreCase));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }

        #endregion
    }
}

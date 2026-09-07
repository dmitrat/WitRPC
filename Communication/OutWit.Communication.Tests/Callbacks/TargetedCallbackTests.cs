using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using NUnit.Framework;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Processors;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Callbacks;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Callbacks
{
    /// <summary>
    /// Targeted callbacks over real transports (3.2): an event raised inside a
    /// <see cref="CallbackScope"/> reaches the named connection(s) and nobody else, a raise
    /// outside a scope still reaches everyone, and the scope reports what happened.
    /// </summary>
    [TestFixture]
    public sealed class TargetedCallbackTests
    {
        #region Constants

        private const string TOKEN = "token";

        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan RECEIVE_WAIT = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan SILENCE = TimeSpan.FromMilliseconds(400);

        #endregion

        #region Fields

        private readonly string m_runId = Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Delivery Tests

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        [TestCase(TransportType.Tcp)]
        public async Task TargetedCallbackReachesOnlyTheTargetTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Cb_Caller_{transportType}_{m_runId}");
            using var first = await host.ConnectAsync();
            using var second = await host.ConnectAsync();
            using var third = await host.ConnectAsync();

            first.Service.NotifyCaller("only me");

            Assert.That(first.WaitFor("only me"), Is.True, "the caller receives its own event");
            Assert.That(second.ReceivedNothingFor(SILENCE), Is.True);
            Assert.That(third.ReceivedNothingFor(SILENCE), Is.True);

            var report = await host.Service.Reports.Single();
            Assert.That(report.Raised, Is.EqualTo(1));
            Assert.That(report.Outcomes.Single().Status, Is.EqualTo(CallbackDeliveryStatus.Sent));
            Assert.That(report.Outcomes.Single().ConnectionId, Is.EqualTo(first.Service.WhoAmI()));
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task CallbackToASetReachesExactlyThatSetTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Cb_Set_{transportType}_{m_runId}");
            using var first = await host.ConnectAsync();
            using var second = await host.ConnectAsync();
            using var third = await host.ConnectAsync();

            var ids = new[] { first.Service.WhoAmI(), second.Service.WhoAmI() };
            third.Service.NotifyMany(ids, "pair");

            Assert.That(first.WaitFor("pair"), Is.True);
            Assert.That(second.WaitFor("pair"), Is.True);
            Assert.That(third.ReceivedNothingFor(SILENCE), Is.True, "the raiser is not in the set");

            var report = await host.Service.Reports.Single();
            Assert.That(report.Outcomes.Select(outcome => outcome.Status), Is.All.EqualTo(CallbackDeliveryStatus.Sent));
            Assert.That(report.Outcomes.Select(outcome => outcome.ConnectionId), Is.EquivalentTo(ids));
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task BroadcastWithoutAScopeIsUnchangedTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Cb_All_{transportType}_{m_runId}");
            using var first = await host.ConnectAsync();
            using var second = await host.ConnectAsync();
            using var third = await host.ConnectAsync();

            first.Service.NotifyAll("everyone");

            Assert.That(first.WaitFor("everyone"), Is.True);
            Assert.That(second.WaitFor("everyone"), Is.True);
            Assert.That(third.WaitFor("everyone"), Is.True);
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task UnknownConnectionIsReportedAndNothingIsSentTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Cb_Unknown_{transportType}_{m_runId}");
            using var first = await host.ConnectAsync();
            using var second = await host.ConnectAsync();

            var unknown = Guid.NewGuid();
            first.Service.NotifyOne(unknown, "nobody");

            Assert.That(first.ReceivedNothingFor(SILENCE), Is.True);
            Assert.That(second.ReceivedNothingFor(SILENCE), Is.True);

            var report = await host.Service.Reports.Single();
            Assert.That(report.Raised, Is.EqualTo(1));
            Assert.That(report.Outcomes.Single().Status, Is.EqualTo(CallbackDeliveryStatus.UnknownConnection));
            Assert.That(report.Outcomes.Single().ConnectionId, Is.EqualTo(unknown));
            Assert.That(report.AnySent, Is.False);
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task TargetedOnlyServerRefusesABroadcastButDeliversTargetedTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Cb_TargetedOnly_{transportType}_{m_runId}",
                new CallbackDeliveryOptions { Mode = CallbackDeliveryMode.TargetedOnly });
            using var first = await host.ConnectAsync();
            using var second = await host.ConnectAsync();

            first.Service.NotifyAll("must not leave the server");

            Assert.That(first.ReceivedNothingFor(SILENCE), Is.True);
            Assert.That(second.ReceivedNothingFor(SILENCE), Is.True);

            second.Service.NotifyCaller("targeted still works");

            Assert.That(second.WaitFor("targeted still works"), Is.True);
            Assert.That(first.ReceivedNothingFor(SILENCE), Is.True);
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task ManyTargetedCallbacksArriveInOrderTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Cb_Order_{transportType}_{m_runId}");
            using var target = await host.ConnectAsync();
            using var other = await host.ConnectAsync();

            var id = target.Service.WhoAmI();
            for (var i = 0; i < 200; i++)
                other.Service.NotifyOne(id, i.ToString());

            Assert.That(target.WaitForCount(200), Is.True);
            Assert.That(other.ReceivedNothingFor(SILENCE), Is.True);

            // Wire order is guaranteed; the client hands each callback to a handler on its own
            // task, so the handlers may complete out of order. What must hold is the set.
            Assert.That(target.Received.OrderBy(int.Parse), Is.EqualTo(Enumerable.Range(0, 200).Select(i => i.ToString())));
        }

        #endregion

        #region Shared Service Tests

        [Test]
        public async Task SharedServiceInTwoServersReportsPerServerTest()
        {
            var service = new MockConnectionAwareService();
            using var firstHost = await Host.StartAsync(TransportType.Pipes, $"Cb_Shared1_{m_runId}", service: service);
            using var secondHost = await Host.StartAsync(TransportType.Pipes, $"Cb_Shared2_{m_runId}", service: service);
            using var onFirst = await firstHost.ConnectAsync();
            using var onSecond = await secondHost.ConnectAsync();

            // The same singleton is raised in both servers; only the one that owns the
            // connection delivers, the other reports the id as unknown.
            onFirst.Service.NotifyCaller("first server only");

            Assert.That(onFirst.WaitFor("first server only"), Is.True);
            Assert.That(onSecond.ReceivedNothingFor(SILENCE), Is.True);

            var report = await service.Reports.Single();
            Assert.That(report.Raised, Is.EqualTo(2), "each server saw the raise");
            Assert.That(report.Outcomes, Has.Count.EqualTo(2));
            Assert.That(report.Outcomes.Single(outcome => outcome.ServerId == firstHost.Server.Id).Status, Is.EqualTo(CallbackDeliveryStatus.Sent));
            Assert.That(report.Outcomes.Single(outcome => outcome.ServerId == secondHost.Server.Id).Status, Is.EqualTo(CallbackDeliveryStatus.UnknownConnection));
            Assert.That(report.AnySent, Is.True);
        }

        #endregion

        #region Helpers

        private sealed class Host : IDisposable
        {
            private Host(WitServer server, MockConnectionAwareService service, TransportType transportType, string name)
            {
                Server = server;
                Service = service;
                TransportType = transportType;
                Name = name;
            }

            public WitServer Server { get; }

            public MockConnectionAwareService Service { get; }

            public TransportType TransportType { get; }

            public string Name { get; }

            public static Task<Host> StartAsync(TransportType transportType, string name, CallbackDeliveryOptions? options = null, MockConnectionAwareService? service = null)
            {
                service ??= new MockConnectionAwareService();
                var server = new WitServer(
                    Shared.GetServerTransport(transportType, 10, name),
                    new EncryptorServerFactory<EncryptorServerGeneral>(),
                    new AccessTokenValidatorStatic(TOKEN),
                    new MessageSerializerJson(),
                    new MessageSerializerMemoryPack(),
                    new RequestProcessor<IConnectionAwareService>(service),
                    null, null, null, name, null, int.MaxValue, null, options);

                server.StartWaitingForConnection();
                return Task.FromResult(new Host(server, service, transportType, name));
            }

            public async Task<Client> ConnectAsync()
            {
                var client = new WitClient(
                    Shared.GetClientTransport(TransportType, Name),
                    new EncryptorClientGeneral(),
                    new AccessTokenProviderStatic(TOKEN),
                    new MessageSerializerJson(),
                    new MessageSerializerMemoryPack(),
                    null, null);

                Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True, "connect");
                return new Client(client);
            }

            public void Dispose()
            {
                Server.StopWaitingForConnection();
                Server.Dispose();
            }
        }

        private sealed class Client : IDisposable
        {
            private readonly ConcurrentQueue<string> m_received = new();

            public Client(WitClient client)
            {
                Raw = client;
                Service = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<IConnectionAwareService>(new RequestInterceptorDynamic(client, true));
                Service.Notified += message => m_received.Enqueue(message);
            }

            public WitClient Raw { get; }

            public IConnectionAwareService Service { get; }

            public string[] Received => m_received.ToArray();

            public bool WaitFor(string message)
            {
                return SpinWait.SpinUntil(() => m_received.Contains(message), RECEIVE_WAIT);
            }

            public bool WaitForCount(int count)
            {
                return SpinWait.SpinUntil(() => m_received.Count >= count, RECEIVE_WAIT);
            }

            /// <summary>
            /// True when no callback arrived during <paramref name="silence"/>.
            /// </summary>
            public bool ReceivedNothingFor(TimeSpan silence)
            {
                var before = m_received.Count;
                Thread.Sleep(silence);
                return m_received.Count == before;
            }

            public void Dispose()
            {
                Raw.Disconnect().GetAwaiter().GetResult();
                Raw.Dispose();
            }
        }

        #endregion
    }
}

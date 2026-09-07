using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using NUnit.Framework;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Processors;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Connections;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Connections
{
    /// <summary>
    /// The connection context a service reads during a request (3.2): the id of the connection
    /// the request arrived on, the server, and the principal the connection authorized with. Per
    /// async flow, never leaking across connections, gone outside a request.
    /// </summary>
    [TestFixture]
    public sealed class ConnectionContextTests
    {
        #region Constants

        private const string TOKEN = "user:alice";

        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(30);

        #endregion

        #region Fields

        private readonly string m_runId = Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Context Tests

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        [TestCase(TransportType.Tcp)]
        public async Task ServiceSeesTheConnectionIdOfTheRequestTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Ctx_WhoAmI_{transportType}_{m_runId}");
            using var first = await host.ConnectAsync();
            using var second = await host.ConnectAsync();

            var firstId = first.Service.WhoAmI();
            var secondId = second.Service.WhoAmI();

            Assert.That(firstId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(secondId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(firstId, Is.Not.EqualTo(secondId), "each connection has its own id");
            Assert.That(first.Service.WhoAmI(), Is.EqualTo(firstId), "the id is stable across calls on one connection");
            Assert.That(first.Service.WhichServer(), Is.EqualTo(host.Server.Id));
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task ContextFlowsAcrossAnAwaitInsideTheMethodTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Ctx_Await_{transportType}_{m_runId}");
            using var client = await host.ConnectAsync();

            Assert.That(client.Service.WhoAmIAfterAwait(), Is.EqualTo(client.Service.WhoAmI()));
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task ConcurrentRequestsDoNotLeakContextAcrossConnectionsTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Ctx_Concurrent_{transportType}_{m_runId}");

            var clients = new Client[6];
            try
            {
                for (var i = 0; i < clients.Length; i++)
                    clients[i] = await host.ConnectAsync();

                var expected = clients.Select(client => client.Service.WhoAmI()).ToArray();
                Assert.That(expected.Distinct().Count(), Is.EqualTo(clients.Length));

                // Every client fires a burst at once; every answer must be that client's own id.
                var work = clients.Select((client, index) => Task.Run(() =>
                {
                    for (var call = 0; call < 20; call++)
                        Assert.That(client.Service.WhoAmI(), Is.EqualTo(expected[index]), $"client {index}, call {call}");
                })).ToArray();

                await Task.WhenAll(work);
            }
            finally
            {
                foreach (var client in clients)
                    client?.Dispose();
            }
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task ReconnectedClientGetsANewConnectionIdTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Ctx_Reconnect_{transportType}_{m_runId}");

            Guid firstId;
            using (var first = await host.ConnectAsync())
                firstId = first.Service.WhoAmI();

            using var second = await host.ConnectAsync();
            Assert.That(second.Service.WhoAmI(), Is.Not.EqualTo(firstId));
        }

        [Test]
        public void ContextIsNullOutsideARequestTest()
        {
            var service = new MockConnectionAwareService();

            Assert.That(ConnectionContext.Current, Is.Null);
            Assert.That(service.WhoAmI(), Is.EqualTo(Guid.Empty));
            Assert.That(service.WhoAmIPrincipal(), Is.Null);
        }

        [Test]
        public void BeginScopeMakesAContextVisibleAndRestoresThePreviousOneTest()
        {
            var outer = new ConnectionContext(Guid.NewGuid(), Guid.NewGuid(), "outer", "Test", null, DateTimeOffset.UtcNow);
            var inner = new ConnectionContext(Guid.NewGuid(), Guid.NewGuid(), "inner", "Test", null, DateTimeOffset.UtcNow);
            var service = new MockConnectionAwareService();

            using (ConnectionContext.BeginScope(outer))
            {
                Assert.That(service.WhoAmI(), Is.EqualTo(outer.ConnectionId));

                using (ConnectionContext.BeginScope(inner))
                    Assert.That(service.WhoAmI(), Is.EqualTo(inner.ConnectionId));

                Assert.That(service.WhoAmI(), Is.EqualTo(outer.ConnectionId), "the inner scope restored the outer context");
            }

            Assert.That(ConnectionContext.Current, Is.Null);
        }

        #endregion

        #region Principal Tests

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task PrincipalIsSetWhenTheValidatorAuthenticatesTest(TransportType transportType)
        {
            var authenticator = new MockConnectionAuthenticator();
            using var host = await Host.StartAsync(transportType, $"Ctx_Principal_{transportType}_{m_runId}", authenticator);
            using var client = await host.ConnectAsync();

            Assert.That(client.Service.WhoAmIPrincipal(), Is.EqualTo("alice"));
            Assert.That(client.Service.WhoAmIPrincipal(), Is.EqualTo("alice"), "established once, read on every request");
            Assert.That(authenticator.AuthenticateCalls, Is.EqualTo(1), "the principal is established at authorization, not per request");
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task PrincipalIsNullForAPlainValidatorTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Ctx_NoPrincipal_{transportType}_{m_runId}", new AccessTokenValidatorStatic(TOKEN));
            using var client = await host.ConnectAsync();

            Assert.That(client.Service.WhoAmI(), Is.Not.EqualTo(Guid.Empty));
            Assert.That(client.Service.WhoAmIPrincipal(), Is.Null);
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task AuthenticatorRefusalClosesTheConnectionTest(TransportType transportType)
        {
            using var host = await Host.StartAsync(transportType, $"Ctx_Refused_{transportType}_{m_runId}", new MockConnectionAuthenticator());

            var client = host.CreateClient("not-a-user");
            try
            {
                Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.False);
                Assert.That(client.IsAuthorized, Is.False);
            }
            finally
            {
                await client.Disconnect();
                client.Dispose();
            }
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

            public static Task<Host> StartAsync(TransportType transportType, string name, IAccessTokenValidator? validator = null)
            {
                var service = new MockConnectionAwareService();
                var server = new WitServer(
                    Shared.GetServerTransport(transportType, 10, name),
                    new EncryptorServerFactory<EncryptorServerGeneral>(),
                    validator ?? new AccessTokenValidatorStatic(TOKEN),
                    new MessageSerializerJson(),
                    new MessageSerializerMemoryPack(),
                    new RequestProcessor<IConnectionAwareService>(service),
                    null, null, null, name, null);

                server.StartWaitingForConnection();
                return Task.FromResult(new Host(server, service, transportType, name));
            }

            public WitClient CreateClient(string token)
            {
                return new WitClient(
                    Shared.GetClientTransport(TransportType, Name),
                    new EncryptorClientGeneral(),
                    new AccessTokenProviderStatic(token),
                    new MessageSerializerJson(),
                    new MessageSerializerMemoryPack(),
                    null, null);
            }

            public async Task<Client> ConnectAsync()
            {
                var client = CreateClient(TOKEN);
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
            public Client(WitClient client)
            {
                Raw = client;
                Service = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<IConnectionAwareService>(new RequestInterceptorDynamic(client, true));
            }

            public WitClient Raw { get; }

            public IConnectionAwareService Service { get; }

            public void Dispose()
            {
                Raw.Disconnect().GetAwaiter().GetResult();
                Raw.Dispose();
            }
        }

        #endregion
    }
}

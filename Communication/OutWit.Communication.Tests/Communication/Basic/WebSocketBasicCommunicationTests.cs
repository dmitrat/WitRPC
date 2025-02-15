using System.Net;
using OutWit.Communication.Converters;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Server;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using System.Runtime.CompilerServices;
using OutWit.Communication.Client.WebSocket;
using OutWit.Communication.Server.Discovery;
using OutWit.Communication.Server.WebSocket;

namespace OutWit.Communication.Tests.Communication.Basic
{
    [TestFixture]
    public class WebSocketBasicCommunicationTests
    {
        private const string AUTHORIZATION_TOKEN = "token";

        [Test]
        public async Task ConnectionTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);
        }

        [Test]
        public async Task ConnectDisconnectTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            await client.Disconnect();
            Assert.That(client.IsInitialized, Is.False);
            Assert.That(client.IsAuthorized, Is.False);

            Thread.Sleep(500);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var request = new WitComRequest
            {
                MethodName = "Test"
            };

            WitComResponse? response = await client.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo("Test"));
        }

        [Test]
        public async Task ReconnectTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            Assert.That(await client.ReconnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var request = new WitComRequest
            {
                MethodName = "Test"
            };

            WitComResponse? response = await client.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo("Test"));
        }

        [Test]
        public async Task StartStopWaitingForConnectionTest()
        {
            var server = GetServer(5);
            server.StartWaitingForConnection();

            var client1 = GetClient();

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            server.StopWaitingForConnection();

            Thread.Sleep(500);

            var client2 = GetClient();

            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client2.IsInitialized, Is.False);
            Assert.That(client2.IsAuthorized, Is.False);

            server.StartWaitingForConnection();

            var client3 = GetClient();

            Assert.That(await client3.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client3.IsInitialized, Is.True);
            Assert.That(client3.IsAuthorized, Is.True);

            var request = new WitComRequest
            {
                MethodName = "Test"
            };

            WitComResponse? response = await client3.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo("Test"));

            response = await client1.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo("Test"));
        }

        [Test]
        public async Task TooManyClientsSingleClientAllowedConnectionTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client1 = GetClient();

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var client2 = GetClient();
            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client2.IsInitialized, Is.False);
            Assert.That(client2.IsAuthorized, Is.False);

            await client1.Disconnect();

            Thread.Sleep(500);

            var client3 = GetClient();
            Assert.That(await client3.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client3.IsInitialized, Is.True);
            Assert.That(client3.IsAuthorized, Is.True);
        }

        [Test]
        public async Task TooManyClientsMultiClientsAllowedConnectionTest()
        {
            var server = GetServer(3);
            server.StartWaitingForConnection();

            var client1 = GetClient();

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var client2 = GetClient();
            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);

            var client3 = GetClient();
            Assert.That(await client3.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client3.IsInitialized, Is.True);
            Assert.That(client3.IsAuthorized, Is.True);

            var client4 = GetClient();
            Assert.That(await client4.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client4.IsInitialized, Is.False);
            Assert.That(client4.IsAuthorized, Is.False);

            await client2.Disconnect();

            var client5 = GetClient();
            Assert.That(await client5.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client5.IsInitialized, Is.True);
            Assert.That(client5.IsAuthorized, Is.True);
        }

        [Test]
        public async Task SingleClientBasicCommunicationTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var request = new WitComRequest
            {
                MethodName = "Test"
            };

            WitComResponse? response = await client.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo("Test"));
        }

        [Test]
        public async Task MultiClientBasicCommunicationTest()
        {
            var server = GetServer(11);
            server.StartWaitingForConnection();

            var clients = new List<WitComClient>
            {
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
                GetClient(),
            };

            var start = DateTime.Now;

            Parallel.For(0, clients.Count, index =>
            {
                var client = clients[index];
                Assert.That(client.ConnectAsync(TimeSpan.Zero, CancellationToken.None).Result, Is.True);

                Assert.That(client.IsInitialized, Is.True);
                Assert.That(client.IsAuthorized, Is.True);
            });
            var end = DateTime.Now;
            Console.WriteLine($"Clients initialization duration: {(end - start).TotalMilliseconds} ms");

            start = DateTime.Now;
            Parallel.For(0, clients.Count, index =>
            {
                var client = clients[index];
                WitComResponse? response = client.SendRequest(new WitComRequest { MethodName = $"Test{index}" }).Result;
                Assert.That(response, Is.Not.Null);
                Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
                Assert.That(response.Data, Is.EqualTo($"Test{index}"));
            });

            end = DateTime.Now;
            Console.WriteLine($"Clients communication duration: {(end - start).TotalMilliseconds} ms");
        }

        private WitComServer GetServer(int maxNumberOfClients, [CallerMemberName] string callerMember = "")
        {
            var serverTransport = new WebSocketServerTransportFactory(new WebSocketServerTransportOptions
            {
                Host = (HostInfo?)$"http://localhost:5000/{callerMember}/",
                MaxNumberOfClients = maxNumberOfClients
            });
            return new WitComServer(serverTransport, 
                new EncryptorServerFactory<EncryptorServerGeneral>(), 
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN), 
                new MessageSerializerJson(), 
                new ValueConverterJson(), 
                new MockRequestProcessor(),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null);
        }

        private WitComClient GetClient([CallerMemberName] string callerMember = "")
        {
            var clientTransport = new WebSocketClientTransport(new WebSocketClientTransportOptions
            {
                Url = $"ws://localhost:5000/{callerMember}/",
            });

            return new WitComClient(clientTransport, 
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(),
                new ValueConverterJson(), null, null);
        }
    }
}

using System.Net;
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
using OutWit.Communication.Client.MMF;
using OutWit.Communication.Server.Discovery;
using OutWit.Communication.Server.MMF;

namespace OutWit.Communication.Tests.Communication.Basic
{
    [TestFixture]
    public class MMFBasicCommunicationTests
    {
        private const string MEMORY_MAPPED_FILE_NAME = "TestMMF";
        private const string AUTHORIZATION_TOKEN = "token";

        [Test]
        public async Task ConnectionTest()
        {
            var server = GetServer();
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);
        }

        [Test]
        public async Task ConnectDisconnectTest()
        {
            var server = GetServer();
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
            var server = GetServer();
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
        public async Task TooManyClientsSingleClientAllowedConnectionTest()
        {
            var server = GetServer();
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

            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(10), CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);
        }

        [Test]
        public async Task SingleClientBasicCommunicationTest()
        {
            var server = GetServer();
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

        private WitComServer GetServer([CallerMemberName] string pipeName = MEMORY_MAPPED_FILE_NAME)
        {
            var serverTransport = new MemoryMappedFileServerTransportFactory(new MemoryMappedFileServerTransportOptions()
            {
                Name = pipeName,
                Size = 1024*1024
            });
            return new WitComServer(serverTransport, 
                new EncryptorServerFactory<EncryptorServerGeneral>(), 
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN), 
                new MessageSerializerJson(), 
                new MockRequestProcessor(), 
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }), 
                null, null, null, null);
        }

        private WitComClient GetClient([CallerMemberName] string pipeName = MEMORY_MAPPED_FILE_NAME)
        {
            var clientTransport = new MemoryMappedFileClientTransport(new MemoryMappedFileClientTransportOptions()
            {
                Name = pipeName
            });

            return new WitComClient(clientTransport, 
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(), null, null);
        }
    }
}

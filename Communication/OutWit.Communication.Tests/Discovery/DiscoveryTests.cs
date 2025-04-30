using System.Net;
using System.Runtime.CompilerServices;
using OutWit.Communication.Model;
using OutWit.Communication.Processors;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Discovery;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Server.WebSocket;
using OutWit.Communication.Server;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Client.Discovery;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;

namespace OutWit.Communication.Tests.Discovery
{
    [TestFixture]
    public class DiscoveryTests
    {
        private const string AUTHORIZATION_TOKEN = "token";

        [Test]
        public async Task ServerStarStopEventsTest()
        {
            var messages = new List<DiscoveryMessage>();

            var discoveryClient = GetDiscoveryClient();
            discoveryClient.MessageReceived += (s, m) =>
            {
                messages.Add(m);
            };
            await discoveryClient.Start();

            var server = GetServer(DiscoveryServerMode.StartStop);
            server.StartWaitingForConnection();

            await Task.Delay(500);

            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages[0].Type, Is.EqualTo(DiscoveryMessageType.Hello));

            server.StopWaitingForConnection();

            await Task.Delay(500);

            Assert.That(messages.Count, Is.EqualTo(2));
            Assert.That(messages[1].Type, Is.EqualTo(DiscoveryMessageType.Goodbye));
        }

        [Test]
        public async Task ServerContinuousEventsTest()
        {
            var messages = new List<DiscoveryMessage>();

            var discoveryClient = GetDiscoveryClient();
            discoveryClient.MessageReceived += (s, m) =>
            {
                messages.Add(m);
            };
            await discoveryClient.Start();

            var server = GetServer(DiscoveryServerMode.Continuous, TimeSpan.FromSeconds(1));
            server.StartWaitingForConnection();

            await Task.Delay(500);

            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages.Last().Type, Is.EqualTo(DiscoveryMessageType.Hello));
            

            await Task.Delay(2600);

            Assert.That(messages.Count, Is.EqualTo(4));
            Assert.That(messages.Last().Type, Is.EqualTo(DiscoveryMessageType.Heartbeat));

            server.StopWaitingForConnection();

            await Task.Delay(500);

            Assert.That(messages.Count, Is.EqualTo(5));
            Assert.That(messages.Last().Type, Is.EqualTo(DiscoveryMessageType.Goodbye));

            await Task.Delay(1100);

            Assert.That(messages.Count, Is.EqualTo(5));
            Assert.That(messages.Last().Type, Is.EqualTo(DiscoveryMessageType.Goodbye));
        }

        private WitComServer GetServer(DiscoveryServerMode mode = DiscoveryServerMode.StartStop, TimeSpan? period = null, [CallerMemberName] string callerMember = "")
        {

            var service = new MockService();
            var serverTransport = new WebSocketServerTransportFactory(new WebSocketServerTransportOptions
            {
                Host = (HostInfo?)$"http://localhost:5000/{callerMember}/",
                MaxNumberOfClients = 10,
                BufferSize = 4096
            });
            return new WitComServer(serverTransport,
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(),
                new MessageSerializerMemoryPack(),
                new RequestProcessor<IService>(service),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = mode,
                    Period = period
                }),
                null, null, null, null);
        }

        private IDiscoveryClient GetDiscoveryClient()
        {
            return new DiscoveryClient(new DiscoveryClientOptions
            {
                IpAddress = IPAddress.Parse("239.255.255.250"),
                Port = 3702,
                Serializer = new MessageSerializerJson()
            });
        }
    }
}

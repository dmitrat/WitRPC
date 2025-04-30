using System;
using OutWit.Communication.Client;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Common.Json;

namespace OutWit.Communication.Tests.Communication
{
    [TestFixture]
    public class CommunicationTestsBasic
    {
        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task ConnectionTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServerBasic(transportType, serializerType, 1, testName);
            
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task ConnectDisconnectTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";
            
            var server = Shared.GetServerBasic(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

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
            Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo("Test"));
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task ReconnectTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServerBasic(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

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
            Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo("Test"));
        }
        
        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        //[TestCase(TransportType.TcpSecure, SerializerType.Json)]
        //[TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        //[TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task StartStopWaitingForConnectionTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";
            
            var server = Shared.GetServerBasic(transportType, serializerType, 5, testName);
            server.StartWaitingForConnection();

            var client1 = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            server.StopWaitingForConnection();

            Thread.Sleep(500);

            var client2 = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client2.IsInitialized, Is.False);
            Assert.That(client2.IsAuthorized, Is.False);

            server.StartWaitingForConnection();

            var client3 = Shared.GetClient(transportType, serializerType, testName);

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
            Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo("Test"));

            response = await client1.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo("Test"));
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task TooManyClientsSingleClientAllowedConnectionTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServerBasic(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client1 = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var client2 = Shared.GetClient(transportType, serializerType, testName);
            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client2.IsInitialized, Is.False);
            Assert.That(client2.IsAuthorized, Is.False);

            await client1.Disconnect();

            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        //[TestCase(TransportType.TcpSecure, SerializerType.Json)]
        //[TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        //[TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task TooManyClientsMultiClientsAllowedConnectionTest(TransportType transportType, SerializerType serializerType)
        {

            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServerBasic(transportType, serializerType, 3, testName);
            server.StartWaitingForConnection();

            var client1 = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var client2 = Shared.GetClient(transportType, serializerType, testName);
            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);

            var client3 = Shared.GetClient(transportType, serializerType, testName);
            Assert.That(await client3.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client3.IsInitialized, Is.True);
            Assert.That(client3.IsAuthorized, Is.True);

            var client4 = Shared.GetClient(transportType, serializerType, testName);
            Assert.That(await client4.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client4.IsInitialized, Is.False);
            Assert.That(client4.IsAuthorized, Is.False);

            await client2.Disconnect();

            Assert.That(await client4.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client4.IsInitialized, Is.True);
            Assert.That(client4.IsAuthorized, Is.True);
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task SingleClientBasicCommunicationTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServerBasic(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

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
            Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo("Test"));
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        public async Task MultiClientBasicCommunicationTest(TransportType transportType, SerializerType serializerType)
        {

            var testName = $"{nameof(ConnectionTest)}_{transportType}_{serializerType}";
            
            var server = Shared.GetServerBasic(transportType, serializerType, 11, testName);
            server.StartWaitingForConnection();

            var clients = new List<WitComClient>
            {
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
                Shared.GetClient(transportType, serializerType, testName),
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
                Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo($"Test{index}"));
            });

            end = DateTime.Now;
            Console.WriteLine($"Clients communication duration: {(end - start).TotalMilliseconds} ms");
        }
    }
}

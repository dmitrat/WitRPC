using System;
using OutWit.Communication.Server;
using System.Collections.Generic;
using System.Linq;
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

        // ConnectAsync(TimeSpan.Zero) waits indefinitely -- a deliberate
        // option of the API, and the wrong one for a test. A test that cannot
        // connect must fail and say so, not park the run and leave a testhost
        // behind holding bin/ against the next build.
        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(30);
        #region Constants

        /// <summary>How long teardown waits for one object to release itself.</summary>
        private const int DISPOSE_TIMEOUT_MS = 5000;

        /// <summary>How long a test waits for a server to stop accepting.</summary>
        private const int STOP_TIMEOUT_MS = 10000;

        #endregion

        #region Fields

        /// <summary>
        /// Everything a test creates, newest first, so teardown closes clients
        /// before the servers they are attached to.
        /// </summary>
        private readonly List<IDisposable> m_disposables = new();

        #endregion

        #region Initialization

        /// <summary>
        /// Nothing here used to be released. Each of the 117 cases left its
        /// server, and its clients, alive for the rest of the process -- named
        /// pipes, sockets and memory-mapped files all still held. By the time the
        /// multi-client cases ran there were dozens of live servers competing for
        /// the same resources, and the suite stopped being able to finish.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            m_disposables.Reverse();

            foreach (var disposable in m_disposables)
            {
                // Bounded on purpose. WitServer.Dispose calls
                // StopWaitingForConnection, which cancels the accept loop and then
                // blocks on it -- and a loop parked in a pending named-pipe accept
                // does not observe that cancellation. Waiting for it without a
                // limit would hang the whole run on teardown, which is precisely
                // what a teardown must never do. Whatever refuses to close in time
                // is left to the process exit.
                var release = Task.Run(() =>
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception)
                    {
                        // A failure to release must not replace the verdict the
                        // test already reached.
                    }
                });

                release.Wait(DISPOSE_TIMEOUT_MS);
            }

            m_disposables.Clear();
        }

        /// <summary>
        /// Stops the server without waiting for it indefinitely.
        /// <para>
        /// Not because the transport is broken -- an earlier version of this
        /// comment claimed it was, and that was wrong. Every server factory does
        /// wake its accept loop before waiting on it: WebSocket closes the
        /// HttpListener, because GetContextAsync cannot be cancelled; pipes pass
        /// the token into WaitForConnectionAsync; MMF waits on the token's handle
        /// alongside the connection slot. The blocking wait afterwards is what
        /// gives callers the guarantee that once Stop returns, nothing else will
        /// be accepted.
        /// </para>
        /// <para>
        /// The bound is here for the same reason every other wait in these tests
        /// has one: a test must fail and say so rather than park the run and leave
        /// an orphaned testhost holding bin/ against the next build. If this ever
        /// trips, it is news -- and the message should be read as "investigate",
        /// not as a known defect.
        /// </para>
        /// </summary>
        private static void StopWithin(WitServer server, TransportType transportType)
        {
            var stop = Task.Run(() => server.StopWaitingForConnection());

            if (!stop.Wait(STOP_TIMEOUT_MS))
            {
                Assert.Fail($"StopWaitingForConnection did not return within {STOP_TIMEOUT_MS} ms " +
                            $"for {transportType}. This is not a known defect -- investigate.");
            }
        }

        /// <summary>Creates a server and registers it for teardown.</summary>
        private WitServer Server(TransportType transportType, SerializerType serializerType, int maxNumberOfClients, string testName)
        {
            var server = Shared.GetServerBasic(transportType, serializerType, maxNumberOfClients, testName);
            m_disposables.Add(server);

            return server;
        }

        /// <summary>Creates a client and registers it for teardown.</summary>
        /// <summary>
        /// Retries a one-second connect until it succeeds or the window closes.
        /// </summary>
        private static async Task<bool> ConnectWithinAsync(WitClient client, TimeSpan window)
        {
            var deadline = DateTime.UtcNow + window;

            while (true)
            {
                if (await client.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None))
                    return true;

                if (DateTime.UtcNow >= deadline)
                    return false;

                await Task.Delay(200);
            }
        }

        private WitClient Client(TransportType transportType, SerializerType serializerType, string testName)
        {
            var client = Shared.GetClient(transportType, serializerType, testName);
            m_disposables.Add(client);

            return client;
        }

        #endregion

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
            var testName = $"CommunicationTestsBasic_{nameof(ConnectionTest)}_{transportType}_{serializerType}";

            var server = Server(transportType, serializerType, 1, testName);
            
            server.StartWaitingForConnection();

            var client = Client(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
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
            var testName = $"CommunicationTestsBasic_{nameof(ConnectDisconnectTest)}_{transportType}_{serializerType}";
            
            var server = Server(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Client(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            await client.Disconnect();
            Assert.That(client.IsInitialized, Is.False);
            Assert.That(client.IsAuthorized, Is.False);

            Thread.Sleep(500);

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var request = new WitRequest
            {
                MethodName = "Test"
            };

            WitResponse? response = await client.SendRequest(request);
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
            var testName = $"CommunicationTestsBasic_{nameof(ReconnectTest)}_{transportType}_{serializerType}";

            var server = Server(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Client(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            Assert.That(await client.ReconnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var request = new WitRequest
            {
                MethodName = "Test"
            };

            WitResponse? response = await client.SendRequest(request);
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
            var testName = $"CommunicationTestsBasic_{nameof(StartStopWaitingForConnectionTest)}_{transportType}_{serializerType}";
            
            var server = Server(transportType, serializerType, 5, testName);
            server.StartWaitingForConnection();

            var client1 = Client(transportType, serializerType, testName);

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            StopWithin(server, transportType);

            Thread.Sleep(500);

            var client2 = Client(transportType, serializerType, testName);

            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client2.IsInitialized, Is.False);
            Assert.That(client2.IsAuthorized, Is.False);

            server.StartWaitingForConnection();

            var client3 = Client(transportType, serializerType, testName);

            Assert.That(await client3.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client3.IsInitialized, Is.True);
            Assert.That(client3.IsAuthorized, Is.True);

            var request = new WitRequest
            {
                MethodName = "Test"
            };

            WitResponse? response = await client3.SendRequest(request);
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
            var testName = $"CommunicationTestsBasic_{nameof(TooManyClientsSingleClientAllowedConnectionTest)}_{transportType}_{serializerType}";

            var server = Server(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client1 = Client(transportType, serializerType, testName);

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var client2 = Client(transportType, serializerType, testName);
            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client2.IsInitialized, Is.False);
            Assert.That(client2.IsAuthorized, Is.False);

            await client1.Disconnect();

            // The slot frees when the server notices client1 leaving, which on
            // a loaded machine can trail the Disconnect() call; the contract is
            // "the next client gets in", not "within one second".
            Assert.That(await ConnectWithinAsync(client2, TimeSpan.FromSeconds(10)), Is.True);
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

            var testName = $"CommunicationTestsBasic_{nameof(TooManyClientsMultiClientsAllowedConnectionTest)}_{transportType}_{serializerType}";

            var server = Server(transportType, serializerType, 3, testName);
            server.StartWaitingForConnection();

            var client1 = Client(transportType, serializerType, testName);

            Assert.That(await client1.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var client2 = Client(transportType, serializerType, testName);
            Assert.That(await client2.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);

            var client3 = Client(transportType, serializerType, testName);
            Assert.That(await client3.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.True);
            Assert.That(client3.IsInitialized, Is.True);
            Assert.That(client3.IsAuthorized, Is.True);

            var client4 = Client(transportType, serializerType, testName);
            Assert.That(await client4.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None), Is.False);
            Assert.That(client4.IsInitialized, Is.False);
            Assert.That(client4.IsAuthorized, Is.False);

            await client2.Disconnect();

            // Same as the single-slot case: the freed slot shows up when the
            // server notices client2 leaving, not necessarily within a second.
            Assert.That(await ConnectWithinAsync(client4, TimeSpan.FromSeconds(10)), Is.True);
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
            var testName = $"CommunicationTestsBasic_{nameof(SingleClientBasicCommunicationTest)}_{transportType}_{serializerType}";

            var server = Server(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Client(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var request = new WitRequest
            {
                MethodName = "Test"
            };

            WitResponse? response = await client.SendRequest(request);
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
            var testName = $"CommunicationTestsBasic_{nameof(MultiClientBasicCommunicationTest)}_{transportType}_{serializerType}";
            
            var server = Server(transportType, serializerType, 11, testName);
            server.StartWaitingForConnection();

            var clients = new List<WitClient>
            {
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
                Client(transportType, serializerType, testName),
            };

            var start = DateTime.Now;

            // Awaited rather than Parallel.For with .Result: blocking ten thread-pool
            // threads on an asynchronous connect starves the very pool the connect
            // needs to complete, and the test then hangs instead of failing.
            await Task.WhenAll(clients.Select(async client =>
            {
                Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);

                Assert.That(client.IsInitialized, Is.True);
                Assert.That(client.IsAuthorized, Is.True);
            }));
            var end = DateTime.Now;
            Console.WriteLine($"Clients initialization duration: {(end - start).TotalMilliseconds} ms");

            start = DateTime.Now;
            await Task.WhenAll(clients.Select(async (client, index) =>
            {
                WitResponse? response = await client.SendRequest(new WitRequest { MethodName = $"Test{index}" });
                Assert.That(response, Is.Not.Null);
                Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
                Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo($"Test{index}"));
            }));

            end = DateTime.Now;
            Console.WriteLine($"Clients communication duration: {(end - start).TotalMilliseconds} ms");
        }
    }
}

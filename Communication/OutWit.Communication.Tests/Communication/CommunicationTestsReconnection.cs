using System;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Communication
{
    [TestFixture]
    public class CommunicationTestsReconnection
    {
        #region ReconnectionOptions Tests

        [Test]
        public void ReconnectionOptions_DefaultValues()
        {
            var options = new ReconnectionOptions();

            Assert.That(options.Enabled, Is.False);
            Assert.That(options.MaxAttempts, Is.EqualTo(10));
            Assert.That(options.InitialDelay, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(options.MaxDelay, Is.EqualTo(TimeSpan.FromMinutes(2)));
            Assert.That(options.BackoffMultiplier, Is.EqualTo(2.0));
            Assert.That(options.ReconnectOnDisconnect, Is.True);
        }

        [Test]
        public void ReconnectionOptions_GetDelayForAttempt_ExponentialBackoff()
        {
            var options = new ReconnectionOptions
            {
                InitialDelay = TimeSpan.FromSeconds(1),
                BackoffMultiplier = 2.0,
                MaxDelay = TimeSpan.FromMinutes(2)
            };

            // First attempt - initial delay
            Assert.That(options.GetDelayForAttempt(1), Is.EqualTo(TimeSpan.FromSeconds(1)));
            
            // Second attempt - 1 * 2 = 2 seconds
            Assert.That(options.GetDelayForAttempt(2), Is.EqualTo(TimeSpan.FromSeconds(2)));
            
            // Third attempt - 1 * 2^2 = 4 seconds
            Assert.That(options.GetDelayForAttempt(3), Is.EqualTo(TimeSpan.FromSeconds(4)));
            
            // Fourth attempt - 1 * 2^3 = 8 seconds
            Assert.That(options.GetDelayForAttempt(4), Is.EqualTo(TimeSpan.FromSeconds(8)));
        }

        [Test]
        public void ReconnectionOptions_GetDelayForAttempt_CappedAtMaxDelay()
        {
            var options = new ReconnectionOptions
            {
                InitialDelay = TimeSpan.FromSeconds(10),
                BackoffMultiplier = 2.0,
                MaxDelay = TimeSpan.FromSeconds(30)
            };

            // 10 * 2^3 = 80 seconds, but capped at 30
            Assert.That(options.GetDelayForAttempt(4), Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void ReconnectionOptions_Clone()
        {
            var original = new ReconnectionOptions
            {
                Enabled = true,
                MaxAttempts = 5,
                InitialDelay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromMinutes(1),
                BackoffMultiplier = 1.5,
                ReconnectOnDisconnect = false
            };

            var clone = original.Clone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Is(original), Is.True);
        }

        #endregion

        #region Client State Tests

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        public async Task Client_InitialState_IsDisconnected(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_InitialState_IsDisconnected)}_{transportType}_{serializerType}";

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Disconnected));
            Assert.That(client.IsInitialized, Is.False);
            Assert.That(client.IsAuthorized, Is.False);
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        public async Task Client_AfterConnect_IsConnected(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_AfterConnect_IsConnected)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            var connected = await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

            Assert.That(connected, Is.True);
            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Connected));
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            await client.Disconnect();
            server.StopWaitingForConnection();
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        public async Task Client_AfterDisconnect_IsDisconnected(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_AfterDisconnect_IsDisconnected)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await client.Disconnect();

            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Disconnected));
            Assert.That(client.IsInitialized, Is.False);
            Assert.That(client.IsAuthorized, Is.False);

            server.StopWaitingForConnection();
        }

        #endregion

        #region Auto Reconnection Tests

        // Note: Auto-reconnection tests use Pipes and WebSocket transports.
        // TCP sockets don't immediately detect connection loss (they require a send operation to fail).
        // Named Pipes and WebSocket detect disconnection more reliably for these test scenarios.

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        public async Task Client_WithAutoReconnect_ReconnectsAfterServerRestart(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_WithAutoReconnect_ReconnectsAfterServerRestart)}_{transportType}_{serializerType}";

            var reconnectedEvent = new ManualResetEventSlim(false);
            var reconnectingCount = 0;

            // Create server
            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            // Create client with auto-reconnect
            var client = Shared.GetClientWithReconnection(transportType, serializerType, testName, options =>
            {
                options.Enabled = true;
                options.MaxAttempts = 10;
                options.InitialDelay = TimeSpan.FromMilliseconds(500);
                options.BackoffMultiplier = 1.5;
            });

            client.Reconnecting += (sender, attempt, delay) =>
            {
                reconnectingCount++;
            };

            client.Reconnected += (sender) =>
            {
                reconnectedEvent.Set();
            };

            // Connect
            var connected = await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            Assert.That(connected, Is.True);

            // Verify service works
            var service = Shared.GetServiceDynamic(client);
            var result = service.RequestData("test");
            Assert.That(result, Is.EqualTo("test"));

            // Stop server (simulates disconnect)
            server.StopWaitingForConnection();
            server.Dispose();

            // Wait a bit for disconnect to be detected
            await Task.Delay(1000);

            // Restart server
            server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            // Wait for reconnection
            var reconnected = reconnectedEvent.Wait(TimeSpan.FromSeconds(15));

            Assert.That(reconnected, Is.True, "Client should have reconnected");
            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Connected));
            Assert.That(reconnectingCount, Is.GreaterThan(0), "At least one reconnection attempt should have been made");

            // Verify service works after reconnection
            service = Shared.GetServiceDynamic(client);
            result = service.RequestData("after_reconnect");
            Assert.That(result, Is.EqualTo("after_reconnect"));

            // Cleanup
            await client.Disconnect();
            server.StopWaitingForConnection();
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        public async Task Client_WithAutoReconnect_FailsAfterMaxAttempts(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_WithAutoReconnect_FailsAfterMaxAttempts)}_{transportType}_{serializerType}";

            var reconnectionFailedEvent = new ManualResetEventSlim(false);
            var reconnectingCount = 0;

            // Create server
            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            // Create client with auto-reconnect (only 2 attempts)
            var client = Shared.GetClientWithReconnection(transportType, serializerType, testName, options =>
            {
                options.Enabled = true;
                options.MaxAttempts = 2;
                options.InitialDelay = TimeSpan.FromMilliseconds(200);
                options.BackoffMultiplier = 1.0;
            });

            client.Reconnecting += (sender, attempt, delay) =>
            {
                reconnectingCount++;
            };

            client.ReconnectionFailed += (sender, exception) =>
            {
                reconnectionFailedEvent.Set();
            };

            // Connect
            await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

            // Stop server permanently
            server.StopWaitingForConnection();
            server.Dispose();

            // Wait for reconnection to fail
            var failed = reconnectionFailedEvent.Wait(TimeSpan.FromSeconds(15));

            Assert.That(failed, Is.True, "Reconnection should have failed");
            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Failed));
            Assert.That(reconnectingCount, Is.EqualTo(2), "Should have made exactly 2 reconnection attempts");
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        public async Task Client_WithoutAutoReconnect_DoesNotReconnect(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_WithoutAutoReconnect_DoesNotReconnect)}_{transportType}_{serializerType}";

            var disconnectedEvent = new ManualResetEventSlim(false);
            var reconnectingCalled = false;

            // Create server
            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            // Create client WITHOUT auto-reconnect
            var client = Shared.GetClient(transportType, serializerType, testName);

            client.Disconnected += (sender) =>
            {
                disconnectedEvent.Set();
            };

            client.Reconnecting += (sender, attempt, delay) =>
            {
                reconnectingCalled = true;
            };

            // Connect
            await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

            // Stop server
            server.StopWaitingForConnection();
            server.Dispose();

            // Wait for disconnect
            disconnectedEvent.Wait(TimeSpan.FromSeconds(5));

            // Wait a bit to ensure no reconnection attempts
            await Task.Delay(1000);

            Assert.That(reconnectingCalled, Is.False, "Reconnection should not have been attempted");
            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Disconnected));
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        public async Task Client_StopReconnection_StopsReconnectionAttempts(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(Client_StopReconnection_StopsReconnectionAttempts)}_{transportType}_{serializerType}";

            var reconnectingCount = 0;

            // Create server
            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            // Create client with auto-reconnect (many attempts)
            var client = Shared.GetClientWithReconnection(transportType, serializerType, testName, options =>
            {
                options.Enabled = true;
                options.MaxAttempts = 100;
                options.InitialDelay = TimeSpan.FromMilliseconds(200);
            });

            client.Reconnecting += (sender, attempt, delay) =>
            {
                reconnectingCount++;
            };

            // Connect
            await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

            // Stop server
            server.StopWaitingForConnection();
            server.Dispose();

            // Wait for some reconnection attempts
            await Task.Delay(2000);

            var attemptsBeforeStop = reconnectingCount;
            Assert.That(attemptsBeforeStop, Is.GreaterThan(0));

            // Stop reconnection
            await client.StopReconnectionAsync();

            // Wait a bit
            await Task.Delay(500);

            // Verify no more attempts were made
            Assert.That(reconnectingCount, Is.EqualTo(attemptsBeforeStop));
            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Disconnected));
        }

        #endregion

        #region Builder Tests

        [Test]
        public void WitClientBuilder_WithAutoReconnect_EnablesReconnection()
        {
            var options = new WitClientBuilderOptions();
            
            options.WithAutoReconnect();

            Assert.That(options.ReconnectionOptions.Enabled, Is.True);
        }

        [Test]
        public void WitClientBuilder_WithAutoReconnect_ConfiguresOptions()
        {
            var options = new WitClientBuilderOptions();
            
            options.WithAutoReconnect(reconnect =>
            {
                reconnect.MaxAttempts = 5;
                reconnect.InitialDelay = TimeSpan.FromSeconds(2);
            });

            Assert.That(options.ReconnectionOptions.Enabled, Is.True);
            Assert.That(options.ReconnectionOptions.MaxAttempts, Is.EqualTo(5));
            Assert.That(options.ReconnectionOptions.InitialDelay, Is.EqualTo(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void WitClientBuilder_WithoutAutoReconnect_DisablesReconnection()
        {
            var options = new WitClientBuilderOptions();
            options.WithAutoReconnect();
            
            options.WithoutAutoReconnect();

            Assert.That(options.ReconnectionOptions.Enabled, Is.False);
        }

        #endregion
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OutWit.Communication.Client;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Server;

namespace OutWit.Communication.Tests.Communication
{
    /// <summary>
    /// The authorisation boundary added in 3.0: a client that fails authorisation
    /// is disconnected rather than left holding an open, keyed channel, and never
    /// gets to send a request.
    /// </summary>
    [TestFixture]
    public class AuthorizationBoundaryTests
    {
        #region Constants

        private const int WAIT_MS = 5000;

        #endregion

        #region Fields

        private readonly string m_runId = Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Tests

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task WrongTokenClientIsRejectedTest(TransportType transportType)
        {
            var name = $"AuthBoundary_WrongTokenClientIsRejectedTest_{transportType}_{m_runId}";

            WitServer? server = null;
            WitClient? client = null;
            try
            {
                server = Shared.GetServerBasic(transportType, SerializerType.Json, 5, name);
                server.StartWaitingForConnection();

                client = Shared.GetClientWithToken(transportType, SerializerType.Json, name, "wrong-token");

                // A wrong token fails authorisation, so connect fails.
                Assert.That(await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.False);
                Assert.That(client.IsAuthorized, Is.False);
            }
            finally
            {
                if (client != null)
                    await client.Disconnect();

                if (server != null)
                {
                    server.StopWaitingForConnection();
                    server.Dispose();
                }
            }
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task WrongTokenClientIsDisconnectedTest(TransportType transportType)
        {
            var name = $"AuthBoundary_WrongTokenClientIsDisconnectedTest_{transportType}_{m_runId}";

            WitServer? server = null;
            WitClient? client = null;
            try
            {
                server = Shared.GetServerBasic(transportType, SerializerType.Json, 5, name);
                server.StartWaitingForConnection();

                client = Shared.GetClientWithToken(transportType, SerializerType.Json, name, "wrong-token");

                var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                client.Disconnected += _ => disconnected.TrySetResult(true);

                Assert.That(await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.False);

                // The server closes the transport of a client it refused, rather
                // than leaving it open, so the client sees the disconnect.
                Assert.That(await WaitAsync(disconnected.Task), Is.True,
                    "the server did not close the connection of a client it refused");
            }
            finally
            {
                if (client != null)
                    await client.Disconnect();

                if (server != null)
                {
                    server.StopWaitingForConnection();
                    server.Dispose();
                }
            }
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task AuthorizedClientStillWorksTest(TransportType transportType)
        {
            var name = $"AuthBoundary_AuthorizedClientStillWorksTest_{transportType}_{m_runId}";

            WitServer? server = null;
            WitClient? client = null;
            try
            {
                server = Shared.GetServerBasic(transportType, SerializerType.Json, 5, name);
                server.StartWaitingForConnection();

                client = Shared.GetClient(transportType, SerializerType.Json, name);

                Assert.That(await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.True);
                Assert.That(client.IsAuthorized, Is.True);

                WitResponse response = await client.SendRequest(new WitRequest { MethodName = "Test" });
                Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            }
            finally
            {
                if (client != null)
                    await client.Disconnect();

                if (server != null)
                {
                    server.StopWaitingForConnection();
                    server.Dispose();
                }
            }
        }

        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.WebSocket)]
        public async Task ConnectionThatNeverHandshakesIsClosedTest(TransportType transportType)
        {
            var name = $"AuthBoundary_ConnectionThatNeverHandshakesIsClosedTest_{transportType}_{m_runId}";

            WitServer? server = null;
            OutWit.Communication.Interfaces.ITransportClient? transport = null;
            try
            {
                server = Shared.GetServerBasicWithHandshakeTimeout(transportType, SerializerType.Json, 5, name, TimeSpan.FromMilliseconds(500));
                server.StartWaitingForConnection();

                // A raw transport that connects but never initializes or authorizes.
                transport = Shared.GetClientTransport(transportType, name);

                var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                transport.Disconnected += _ => disconnected.TrySetResult(true);

                Assert.That(await transport.ConnectAsync(TimeSpan.FromSeconds(2), CancellationToken.None), Is.True);

                // The server closes a connection that has not finished the
                // handshake within its window.
                Assert.That(await WaitAsync(disconnected.Task), Is.True,
                    "the server did not close a connection that never handshaked");
            }
            finally
            {
                if (transport != null)
                    await transport.Disconnect();

                if (server != null)
                {
                    server.StopWaitingForConnection();
                    server.Dispose();
                }
            }
        }

        #endregion

        #region Tools

        private static async Task<bool> WaitAsync(Task<bool> task)
        {
            try
            {
                return await task.WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS));
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        #endregion
    }
}

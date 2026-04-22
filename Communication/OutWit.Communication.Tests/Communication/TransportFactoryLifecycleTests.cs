using OutWit.Common.Json;
using OutWit.Communication.Client;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Server;

namespace OutWit.Communication.Tests.Communication
{
    [TestFixture]
    public sealed class TransportFactoryLifecycleTests
    {
        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task ServerCanBeRecreatedOnSameEndpointAfterDisposeTest(TransportType transportType)
        {
            var testName = $"{nameof(ServerCanBeRecreatedOnSameEndpointAfterDisposeTest)}_{transportType}";

            WitServer? firstServer = null;
            WitServer? secondServer = null;
            WitClient? firstClient = null;
            WitClient? secondClient = null;

            try
            {
                firstServer = Shared.GetServerBasic(transportType, SerializerType.Json, 1, testName);
                firstServer.StartWaitingForConnection();

                firstClient = Shared.GetClient(transportType, SerializerType.Json, testName);
                Assert.That(await firstClient.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.True);

                await AssertSuccessfulRequest(firstClient);
                await firstClient.Disconnect();
                firstClient = null;

                firstServer.StopWaitingForConnection();
                firstServer.Dispose();
                firstServer = null;

                await Task.Delay(300);

                secondServer = Shared.GetServerBasic(transportType, SerializerType.Json, 1, testName);
                secondServer.StartWaitingForConnection();

                secondClient = Shared.GetClient(transportType, SerializerType.Json, testName);
                Assert.That(await secondClient.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None), Is.True);

                await AssertSuccessfulRequest(secondClient);
            }
            finally
            {
                if (firstClient != null)
                    await firstClient.Disconnect();

                if (secondClient != null)
                    await secondClient.Disconnect();

                if (firstServer != null)
                {
                    firstServer.StopWaitingForConnection();
                    firstServer.Dispose();
                }

                if (secondServer != null)
                {
                    secondServer.StopWaitingForConnection();
                    secondServer.Dispose();
                }
            }
        }

        [TestCase(TransportType.WebSocket)]
        public void StartingSecondListenerOnSameEndpointThrowsSynchronouslyTest(TransportType transportType)
        {
            var testName = $"{nameof(StartingSecondListenerOnSameEndpointThrowsSynchronouslyTest)}_{transportType}";

            WitServer? firstServer = null;
            WitServer? secondServer = null;

            try
            {
                firstServer = Shared.GetServerBasic(transportType, SerializerType.Json, 1, testName);
                firstServer.StartWaitingForConnection();

                secondServer = Shared.GetServerBasic(transportType, SerializerType.Json, 1, testName);

                Assert.That(() => secondServer.StartWaitingForConnection(), Throws.Exception);
            }
            finally
            {
                if (secondServer != null)
                {
                    secondServer.StopWaitingForConnection();
                    secondServer.Dispose();
                }

                if (firstServer != null)
                {
                    firstServer.StopWaitingForConnection();
                    firstServer.Dispose();
                }
            }
        }

        private static async Task AssertSuccessfulRequest(WitClient client)
        {
            var request = new WitRequest
            {
                MethodName = "Test"
            };

            WitResponse? response = await client.SendRequest(request);
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data.FromJsonBytes<string>(), Is.EqualTo("Test"));
        }
    }
}

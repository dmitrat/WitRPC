using System.Collections.Concurrent;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Resilience;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Tests.Communication
{
    /// <summary>
    /// Retry semantics of 3.0: a client-local timeout is a
    /// <see cref="CommunicationStatus.Timeout"/> (not a server fault), retry is
    /// restricted to methods the consumer declared idempotent, and every retry
    /// attempt of one call carries the same <see cref="WitRequest.InvocationId"/>.
    /// </summary>
    [TestFixture]
    public sealed class WitClientRetryTests
    {
        #region Tests

        [Test]
        public async Task UnmarkedMethodIsNotRetriedTest()
        {
            var transport = new SilentTransport();
            using var client = CreateClient(transport, options =>
            {
                options.Enabled = true;
                options.MaxRetries = 2;
                options.InitialDelay = TimeSpan.FromMilliseconds(10);
            });

            var response = await client.SendRequest(new WitRequest { MethodName = "DoWork" });

            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Timeout));
            Assert.That(transport.SentFrames.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task IdempotentMethodIsRetriedWithSameInvocationIdTest()
        {
            var transport = new SilentTransport();
            using var client = CreateClient(transport, options =>
            {
                options.Enabled = true;
                options.MaxRetries = 2;
                options.InitialDelay = TimeSpan.FromMilliseconds(10);
                options.MarkIdempotent("DoWork");
            });

            var response = await client.SendRequest(new WitRequest { MethodName = "DoWork" });

            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Timeout));
            Assert.That(transport.SentFrames.Count, Is.EqualTo(3), "one attempt plus two retries");

            var serializer = new MessageSerializerMemoryPack();
            var invocationIds = transport.SentFrames
                .Select(frame => serializer.Deserialize<WitMessage>(frame))
                .Select(message => serializer.Deserialize<WitRequest>(message!.Data!)!.InvocationId)
                .Distinct()
                .ToArray();

            Assert.That(invocationIds, Has.Length.EqualTo(1));
            Assert.That(invocationIds[0], Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public async Task RetryAllMethodsOverridesDeclarationsTest()
        {
            var transport = new SilentTransport();
            using var client = CreateClient(transport, options =>
            {
                options.Enabled = true;
                options.MaxRetries = 1;
                options.InitialDelay = TimeSpan.FromMilliseconds(10);
                options.RetryAllMethods = true;
            });

            var response = await client.SendRequest(new WitRequest { MethodName = "UndeclaredCommand" });

            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Timeout));
            Assert.That(transport.SentFrames.Count, Is.EqualTo(2));
        }

        #endregion

        #region Helpers

        private static WitClient CreateClient(SilentTransport transport, Action<RetryOptions> configureRetry)
        {
            var retryOptions = new RetryOptions();
            configureRetry(retryOptions);

            return new WitClient(
                transport,
                new EncryptorClientPlain(),
                new AccessTokenProviderStatic(string.Empty),
                new MessageSerializerJson(),
                new MessageSerializerMemoryPack(),
                new ReconnectionOptions(),
                retryOptions,
                logger: null,
                timeout: TimeSpan.FromMilliseconds(150));
        }

        /// <summary>A transport that records every send and never answers.</summary>
        private sealed class SilentTransport : ITransportClient
        {
            public event TransportDataEventHandler Callback = delegate { };

            public event TransportEventHandler Disconnected = delegate { };

            public Guid Id { get; } = Guid.NewGuid();

            public string? Address => "test://silent";

            public ConcurrentQueue<byte[]> SentFrames { get; } = new();

            public Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }

            public Task<bool> ConnectAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }

            public Task Disconnect()
            {
                return Task.CompletedTask;
            }

            public Task SendBytesAsync(byte[] data)
            {
                SentFrames.Enqueue(data);
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }
}

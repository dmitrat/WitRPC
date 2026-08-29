using OutWit.Communication.Interfaces;
using OutWit.Communication.Server.Connections;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Tests.Connections
{
    /// <summary>
    /// The per-connection invocation cache is bounded two ways: at most 64
    /// entries (oldest evicted first) and no entry above 256 KB. Both bounds
    /// exist so de-duplication can never grow into a memory problem.
    /// </summary>
    [TestFixture]
    public sealed class ConnectionInfoTests
    {
        #region Constants

        private const int CACHE_CAPACITY = 64;

        private const int MAX_ENTRY_BYTES = 256 * 1024;

        #endregion

        #region Cache Tests

        [Test]
        public void CacheEvictsOldestBeyondCapacityTest()
        {
            using var connection = CreateConnection();

            var first = Guid.NewGuid();
            connection.CacheResponse(first, new byte[] { 1 });

            Guid last = Guid.Empty;
            for (int i = 0; i < CACHE_CAPACITY; i++)
            {
                last = Guid.NewGuid();
                connection.CacheResponse(last, new byte[] { 2 });
            }

            // The first entry fell out of the window; the newest stays.
            Assert.That(connection.TryGetCachedResponse(first, out _), Is.False);
            Assert.That(connection.TryGetCachedResponse(last, out var cached), Is.True);
            Assert.That(cached, Is.EqualTo(new byte[] { 2 }));
        }

        [Test]
        public void OversizedResponseIsNotCachedTest()
        {
            using var connection = CreateConnection();

            var oversized = Guid.NewGuid();
            connection.CacheResponse(oversized, new byte[MAX_ENTRY_BYTES + 1]);

            var atLimit = Guid.NewGuid();
            connection.CacheResponse(atLimit, new byte[MAX_ENTRY_BYTES]);

            Assert.That(connection.TryGetCachedResponse(oversized, out _), Is.False);
            Assert.That(connection.TryGetCachedResponse(atLimit, out _), Is.True);
        }

        [Test]
        public void DuplicateCacheEntryKeepsTheFirstResponseTest()
        {
            using var connection = CreateConnection();

            var invocationId = Guid.NewGuid();
            connection.CacheResponse(invocationId, new byte[] { 1 });
            connection.CacheResponse(invocationId, new byte[] { 2 });

            Assert.That(connection.TryGetCachedResponse(invocationId, out var cached), Is.True);
            Assert.That(cached, Is.EqualTo(new byte[] { 1 }));
        }

        #endregion

        #region Helpers

        private static ConnectionInfo CreateConnection()
        {
            return new ConnectionInfo(new StubTransport(), new EncryptorServerFactory<EncryptorServerPlain>());
        }

        private sealed class StubTransport : ITransportServer
        {
            public event TransportDataEventHandler Callback = delegate { };

            public event TransportEventHandler Disconnected = delegate { };

            public Guid Id { get; } = Guid.NewGuid();

            public bool CanReinitialize => false;

            public Task<bool> InitializeConnectionAsync(CancellationToken token)
            {
                return Task.FromResult(true);
            }

            public Task SendBytesAsync(byte[] data)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }
}

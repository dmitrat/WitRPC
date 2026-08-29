using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Channels;
using OutWit.Communication.Interfaces;
using OutWit.Communication.MMF;

namespace OutWit.Communication.Tests.Transports
{
    /// <summary>
    /// The contract every transport pair has to meet at the
    /// <see cref="ITransportClient"/>/<see cref="ITransportServerFactory"/> level,
    /// with no <c>WitServer</c> or <c>WitClient</c> involved. Frames are opaque
    /// bytes here; the only things that matter are that every frame arrives
    /// intact, that both ends notice when the other leaves, and that a name or
    /// port can be reused after a stop.
    /// <para>
    /// Every wait is bounded. A transport that hangs fails the test instead of
    /// parking the run.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class TransportConformanceTests
    {
        #region Constants

        private const int WAIT_MS = 5000;

        // Generous on purpose: a connect that races a server restart legitimately
        // polls while the outgoing instance finishes releasing its named objects,
        // and the whole suite runs back-to-back on one machine.
        private const int CONNECT_TIMEOUT_MS = 15000;

        #endregion

        #region Fields

        private readonly List<IDisposable> m_disposables = new();

        // Unique per test instance so one test's kernel-object names (mmf, mutexes,
        // events, ports) can never collide with, or linger into, another's.
        private readonly string m_runId = Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Setup

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ThreadPool.GetMinThreads(out int worker, out int io);
            ThreadPool.SetMinThreads(Math.Max(worker, 200), Math.Max(io, 200));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_disposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_disposables[i].Dispose();
                }
                catch (Exception)
                {
                }
            }

            m_disposables.Clear();
        }

        #endregion

        #region Delivery Tests

        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task EchoRoundTripTest(TransportType transportType)
        {
            var server = Server(transportType, Name(transportType), echo: true);
            server.Start();

            var client = Client(transportType, server.Name);
            var sink = new TransportSink(client);

            Assert.That(await ConnectAsync(client), Is.True);

            // Wait until the harness has wired its echo for this client, so the
            // first frame is not sent into a not-yet-echoing server.
            await server.WaitForClientAsync();

            for (int i = 0; i < 50; i++)
            {
                byte[] payload = Frame(i, i * 37 + 1);

                await client.SendBytesAsync(payload);

                byte[] echoed = await sink.NextAsync();
                Assert.That(echoed, Is.EqualTo(payload), $"frame {i}");
            }
        }

        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task LargeFrameRoundTripTest(TransportType transportType)
        {
            var server = Server(transportType, Name(transportType), echo: true);
            server.Start();

            var client = Client(transportType, server.Name);
            var sink = new TransportSink(client);

            Assert.That(await ConnectAsync(client), Is.True);

            await server.WaitForClientAsync();

            // Bigger than the 1 MB MMF file and the 1 MB WebSocket buffer the
            // shared helpers configure, so every transport has to split it.
            byte[] payload = Frame(7, 3 * 1024 * 1024 + 13);

            await client.SendBytesAsync(payload);

            byte[] echoed = await sink.NextAsync(WAIT_MS * 4);
            Assert.That(echoed, Is.EqualTo(payload));
        }

        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task BidirectionalConcurrentTrafficTest(TransportType transportType)
        {
            const int count = 200;

            var server = Server(transportType, Name(transportType), echo: false);
            server.Start();

            var client = Client(transportType, server.Name);
            var clientSink = new TransportSink(client);

            Assert.That(await ConnectAsync(client), Is.True);

            ITransportServer serverTransport = await server.WaitForClientAsync();
            TransportSink serverSink = server.Sink!;

            var clientSends = Task.Run(async () =>
            {
                for (int i = 0; i < count; i++)
                    await client.SendBytesAsync(Frame(i, i * 13 % 4096 + 1));
            });

            var serverSends = Task.Run(async () =>
            {
                for (int i = 0; i < count; i++)
                    await serverTransport.SendBytesAsync(Frame(count + i, i * 17 % 4096 + 1));
            });

            await Task.WhenAll(clientSends, serverSends).WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS * 4));

            await AssertFramesAsync(serverSink, Enumerable.Range(0, count), i => i * 13 % 4096 + 1);
            await AssertFramesAsync(clientSink, Enumerable.Range(count, count), i => (i - count) * 17 % 4096 + 1);
        }

        // MMF serializes concurrent sends internally (its channel holds a send
        // lock). The stream transports (TCP/Pipes/WebSocket) do not yet — the
        // WitClient/WitServer layer above them serializes sends today — so a
        // per-connection outbound lock for them is deferred to the framing and
        // lifecycle stage. Until then this contract is asserted for MMF only.
        [TestCase(TransportType.MMF)]
        public async Task ConcurrentSendsDeliverEveryFrameTest(TransportType transportType)
        {
            const int writers = 8;
            const int perWriter = 50;

            var server = Server(transportType, Name(transportType), echo: false);
            server.Start();

            var client = Client(transportType, server.Name);
            m_disposables.Add(new TransportSink(client));

            Assert.That(await ConnectAsync(client), Is.True);

            await server.WaitForClientAsync();
            TransportSink serverSink = server.Sink!;

            var tasks = Enumerable.Range(0, writers).Select(writer => Task.Run(async () =>
            {
                for (int i = 0; i < perWriter; i++)
                {
                    int index = writer * perWriter + i;
                    await client.SendBytesAsync(Frame(index, index * 11 % 2048 + 1));
                }
            }));

            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS * 4));

            await AssertFramesAsync(serverSink, Enumerable.Range(0, writers * perWriter), i => i * 11 % 2048 + 1);
        }

        #endregion

        #region Lifecycle Tests

        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task ClientDisconnectIsSeenByServerTest(TransportType transportType)
        {
            var server = Server(transportType, Name(transportType), echo: false);
            server.Start();

            var client = Client(transportType, server.Name);
            m_disposables.Add(new TransportSink(client));

            Assert.That(await ConnectAsync(client), Is.True);

            await server.WaitForClientAsync();

            await client.Disconnect();

            Assert.That(await server.Sink!.WaitForDisconnectAsync(), Is.True, "server transport did not see the client leave");
        }

        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task ServerDisposeIsSeenByClientTest(TransportType transportType)
        {
            var server = Server(transportType, Name(transportType), echo: false);
            server.Start();

            var client = Client(transportType, server.Name);
            var sink = new TransportSink(client);

            Assert.That(await ConnectAsync(client), Is.True);

            ITransportServer serverTransport = await server.WaitForClientAsync();

            serverTransport.Dispose();

            Assert.That(await sink.WaitForDisconnectAsync(), Is.True, "client transport did not see the server leave");
        }

        [TestCase(TransportType.MMF)]
        [TestCase(TransportType.Pipes)]
        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpSecure)]
        [TestCase(TransportType.WebSocket)]
        public async Task RestartOnSameNameTest(TransportType transportType)
        {
            string name = Name(transportType);

            var first = Server(transportType, name, echo: true);
            first.Start();

            var firstClient = Client(transportType, name);
            var firstSink = new TransportSink(firstClient);

            Assert.That(await ConnectAsync(firstClient), Is.True);
            await first.WaitForClientAsync();
            await firstClient.SendBytesAsync(Frame(1, 64));
            Assert.That(await firstSink.NextAsync(), Is.EqualTo(Frame(1, 64)));

            await firstClient.Disconnect();
            first.Dispose();

            var second = Server(transportType, name, echo: true);
            second.Start();

            var secondClient = Client(transportType, name);
            var secondSink = new TransportSink(secondClient);

            Assert.That(await ConnectAsync(secondClient), Is.True, "could not connect to a server restarted on the same name");
            await second.WaitForClientAsync();
            await secondClient.SendBytesAsync(Frame(2, 64));
            Assert.That(await secondSink.NextAsync(), Is.EqualTo(Frame(2, 64)));
        }

        #endregion

        #region MMF Tests

        [Test]
        public async Task MmfSecondClientIsRefusedWhileFirstIsConnectedTest()
        {
            var server = Server(TransportType.MMF, Name(TransportType.MMF), echo: true);
            server.Start();

            var first = Client(TransportType.MMF, server.Name);
            var firstSink = new TransportSink(first);
            Assert.That(await ConnectAsync(first), Is.True);
            await server.WaitForClientAsync();

            // Baseline: the first client echoes before any second client appears.
            await first.SendBytesAsync(Frame(9, 32));
            Assert.That(await firstSink.NextAsync(), Is.EqualTo(Frame(9, 32)), "baseline echo failed before any second client");

            var second = Client(TransportType.MMF, server.Name);
            var secondSink = new TransportSink(second);
            bool secondConnected = await second.ConnectAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
            Assert.That(secondConnected, Is.False, "a second client attached to a one-to-one channel");

            await first.SendBytesAsync(Frame(1, 32));
            Assert.That(await firstSink.NextAsync(), Is.EqualTo(Frame(1, 32)), "the first client was disturbed by the refused one");

            await first.Disconnect();

            server.ExpectNextClient();

            Assert.That(await ConnectAsync(second), Is.True, "the seat was not freed after the first client left");

            // The client is connected once it has the server's ack; the harness
            // wires its echo when the factory reports the new client. Wait for
            // that so the echo is armed before the frame is sent.
            await server.WaitForClientAsync();

            await second.SendBytesAsync(Frame(2, 32));
            Assert.That(await secondSink.NextAsync(), Is.EqualTo(Frame(2, 32)));
        }

        [Test]
        public async Task MmfClientDeathIsSeenByServerTest()
        {
            var server = Server(TransportType.MMF, Name(TransportType.MMF), echo: false);
            server.Start();

            // A client that takes the seat and says hello, held alive so the
            // server attaches (and the harness wires its sink) before it dies.
            // The raw client opens the kernel objects directly, so it needs the
            // per-process channel name the factory actually published under.
            var raw = await AttachRawClientAsync(Shared.ChannelName(server.Name));
            m_disposables.Add(raw);

            await server.WaitForClientAsync();

            // Now it dies without saying goodbye: its thread exits still owning
            // the seat, so the seat is abandoned — the process-crash case.
            raw.Die();

            Assert.That(await server.Sink!.WaitForDisconnectAsync(), Is.True, "server did not notice an abandoned client");

            // And the channel is usable again afterwards.
            server.ExpectNextClient();

            var client = Client(TransportType.MMF, server.Name);
            m_disposables.Add(new TransportSink(client));

            Assert.That(await ConnectAsync(client), Is.True, "channel was not republished after the dead client");
        }

        [Test]
        public async Task MmfServerDeathIsSeenByClientTest()
        {
            string name = Name(TransportType.MMF);

            // A raw server that publishes a channel and then dies still owning its
            // presence mutex. It publishes the kernel objects directly, so it uses
            // the per-process channel name the real client will resolve.
            var raw = await PublishRawServerAndDieAsync(Shared.ChannelName(name));
            m_disposables.Add(raw);

            var client = Client(TransportType.MMF, name);
            var sink = new TransportSink(client);

            Assert.That(await ConnectAsync(client), Is.True);

            raw.Die();

            Assert.That(await sink.WaitForDisconnectAsync(), Is.True, "client did not notice an abandoned server");
        }

        [Test]
        public async Task MmfConnectDisconnectStressTest()
        {
            const int iterations = 100;
            const int frames = 10;

            var server = Server(TransportType.MMF, Name(TransportType.MMF), echo: true);
            server.Start();

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                server.ExpectNextClient();

                using var client = Shared.GetClientTransport(TransportType.MMF, server.Name);
                using var sink = new TransportSink(client);

                Assert.That(await ConnectAsync(client), Is.True, $"iteration {iteration}: connect");

                ITransportServer serverTransport = await server.WaitForClientAsync();

                var pushes = Task.Run(async () =>
                {
                    for (int i = 0; i < frames; i++)
                        await serverTransport.SendBytesAsync(Frame(1000 + i, 256));
                });

                for (int i = 0; i < frames; i++)
                    await client.SendBytesAsync(Frame(i, 256));

                await pushes.WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS));

                var expected = new HashSet<int>(Enumerable.Range(0, frames).Concat(Enumerable.Range(1000, frames)));
                await AssertFramesAsync(sink, expected, _ => 256);

                await client.Disconnect();

                Assert.That(await server.Sink!.WaitForDisconnectAsync(), Is.True, $"iteration {iteration}: server did not see disconnect");
            }
        }

        #endregion

        #region Tools

        private string Name(TransportType transportType)
        {
            return $"Conformance_{TestContext.CurrentContext.Test.MethodName}_{transportType}_{m_runId}";
        }

        private ServerHarness Server(TransportType transportType, string name, bool echo)
        {
            var harness = new ServerHarness(Shared.GetServerTransport(transportType, 1, name), name, echo);
            m_disposables.Add(harness);
            return harness;
        }

        private ITransportClient Client(TransportType transportType, string name)
        {
            var client = Shared.GetClientTransport(transportType, name);
            m_disposables.Add(client);
            return client;
        }

        private static Task<bool> ConnectAsync(ITransportClient client)
        {
            return client.ConnectAsync(TimeSpan.FromMilliseconds(CONNECT_TIMEOUT_MS), CancellationToken.None)
                .WaitAsync(TimeSpan.FromMilliseconds(CONNECT_TIMEOUT_MS + WAIT_MS));
        }

        /// <summary>A frame that carries its own index and a fill derived from it, so a corrupted or misrouted frame is caught.</summary>
        private static byte[] Frame(int index, int length)
        {
            var frame = new byte[4 + length];
            BitConverter.GetBytes(index).CopyTo(frame, 0);

            for (int i = 0; i < length; i++)
                frame[4 + i] = (byte)((index + i) % 251);

            return frame;
        }

        private static int IndexOf(byte[] frame)
        {
            Assert.That(frame.Length, Is.GreaterThanOrEqualTo(4), "frame too short to carry an index");
            return BitConverter.ToInt32(frame, 0);
        }

        private static void AssertFrame(byte[] frame, int index, int length)
        {
            Assert.That(frame.Length, Is.EqualTo(4 + length), $"frame {index}: length");

            for (int i = 0; i < length; i++)
            {
                if (frame[4 + i] != (byte)((index + i) % 251))
                    Assert.Fail($"frame {index}: corrupted at byte {i}");
            }
        }

        private static async Task AssertFramesAsync(TransportSink sink, IEnumerable<int> expectedIndices, Func<int, int> lengthOf)
        {
            var expected = new HashSet<int>(expectedIndices);
            var seen = new HashSet<int>();

            while (seen.Count < expected.Count)
            {
                byte[] frame = await sink.NextAsync();
                int index = IndexOf(frame);

                Assert.That(expected, Does.Contain(index), $"unexpected frame index {index}");
                Assert.That(seen.Add(index), Is.True, $"frame {index} delivered twice");

                AssertFrame(frame, index, lengthOf(index));
            }
        }

        private static async Task<RawClient> AttachRawClientAsync(string name)
        {
            var raw = new RawClient(name);
            await raw.AttachAsync();
            return raw;
        }

        private static async Task<RawServer> PublishRawServerAndDieAsync(string name)
        {
            var raw = new RawServer(name);
            await raw.PublishAsync();
            return raw;
        }

        #endregion

        #region Nested Types

        /// <summary>Collects a transport's frames and its disconnect, with bounded reads.</summary>
        private sealed class TransportSink : IDisposable
        {
            private readonly Channel<byte[]> m_frames = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();

            private readonly TaskCompletionSource<bool> m_disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public TransportSink(ITransport transport)
            {
                transport.Callback += OnCallback;
                transport.Disconnected += OnDisconnected;
            }

            public async Task<byte[]> NextAsync(int timeoutMs = WAIT_MS)
            {
                using var cts = new CancellationTokenSource(timeoutMs);

                try
                {
                    return await m_frames.Reader.ReadAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Assert.Fail($"no frame arrived within {timeoutMs} ms");
                    throw;
                }
            }

            public async Task<bool> WaitForDisconnectAsync(int timeoutMs = WAIT_MS)
            {
                try
                {
                    return await m_disconnected.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
                }
                catch (TimeoutException)
                {
                    return false;
                }
            }

            private void OnCallback(Guid sender, byte[] data)
            {
                m_frames.Writer.TryWrite(data);
            }

            private void OnDisconnected(Guid sender)
            {
                m_disconnected.TrySetResult(true);
            }

            public void Dispose()
            {
                m_frames.Writer.TryComplete();
            }
        }

        /// <summary>Wraps a server factory: starts it, captures the attached transport and its sink, optionally echoes.</summary>
        private sealed class ServerHarness : IDisposable
        {
            private readonly bool m_echo;

            private readonly ConcurrentBag<ITransportServer> m_transports = new();

            private TaskCompletionSource<ITransportServer> m_connected = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ServerHarness(ITransportServerFactory factory, string name, bool echo)
            {
                Factory = factory;
                Name = name;
                m_echo = echo;

                factory.NewClientConnected += OnNewClientConnected;
            }

            public ITransportServerFactory Factory { get; }

            public string Name { get; }

            public TransportSink? Sink { get; private set; }

            public void Start()
            {
                Factory.StartWaitingForConnection(null);
            }

            public Task<ITransportServer> WaitForClientAsync()
            {
                return m_connected.Task.WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS));
            }

            /// <summary>Arms the harness for the next client after the current one has gone.</summary>
            public void ExpectNextClient()
            {
                m_connected = new TaskCompletionSource<ITransportServer>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            private void OnNewClientConnected(ITransportServer transport)
            {
                m_transports.Add(transport);

                var sink = new TransportSink(transport);
                Sink = sink;

                if (m_echo)
                    transport.Callback += (sender, data) => _ = transport.SendBytesAsync(data);

                m_connected.TrySetResult(transport);
            }

            public void Dispose()
            {
                Factory.NewClientConnected -= OnNewClientConnected;

                try
                {
                    Factory.StopWaitingForConnection();
                }
                catch (Exception)
                {
                }

                foreach (var transport in m_transports)
                {
                    try
                    {
                        transport.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }

                Factory.Dispose();
            }
        }

        /// <summary>
        /// A hand-rolled MMF client whose seat mutex is held by a thread that can
        /// be told to exit without releasing — the client-process-crash case,
        /// reproduced inside one process. It attaches (claim slot, take seat, say
        /// hello) and then holds until told to die.
        /// </summary>
        private sealed class RawClient : IDisposable
        {
            private readonly string m_name;

            private readonly ManualResetEvent m_die = new(false);

            private readonly TaskCompletionSource<bool> m_attached = new(TaskCreationOptions.RunContinuationsAsynchronously);

            private Thread? m_thread;

            public RawClient(string name)
            {
                m_name = name;
            }

            public Task AttachAsync()
            {
                m_thread = new Thread(Run)
                {
                    IsBackground = true
                };

                m_thread.Start();

                return m_attached.Task.WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS));
            }

            public void Die()
            {
                m_die.Set();
                m_thread?.Join(WAIT_MS);
            }

            private void Run()
            {
                MmfChannel? channel = null;
                Mutex? seat = null;

                try
                {
                    using (var slot = Semaphore.OpenExisting(MmfChannelLayout.SlotName(m_name)))
                        Assert.That(slot.WaitOne(WAIT_MS), Is.True, "raw client could not claim the slot");

                    using var file = MemoryMappedFile.OpenExisting(MmfChannelLayout.FileName(m_name), MemoryMappedFileRights.ReadWrite);
                    var accessor = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
                    long size = accessor.ReadInt64(MmfChannelLayout.FILE_OFFSET_SIZE);

                    seat = Mutex.OpenExisting(MmfChannelLayout.ClientAliveName(m_name));
                    Assert.That(seat.WaitOne(0), Is.True, "raw client could not take the seat");

                    channel = MmfChannel.ForClient(accessor, size,
                        EventWaitHandle.OpenExisting(MmfChannelLayout.ClientToServerReadyName(m_name)),
                        EventWaitHandle.OpenExisting(MmfChannelLayout.ClientToServerFreeName(m_name)),
                        EventWaitHandle.OpenExisting(MmfChannelLayout.ServerToClientReadyName(m_name)),
                        EventWaitHandle.OpenExisting(MmfChannelLayout.ServerToClientFreeName(m_name)));

                    channel.SendAsync(Array.Empty<byte>(), MmfFrameFlags.Hello).GetAwaiter().GetResult();

                    m_attached.TrySetResult(true);

                    // Hold the seat until told to die. Dying sends no goodbye frame:
                    // the seat handle just closes, as a crashing process would drop
                    // it, which both frees the seat (the server sees the client
                    // gone) and frees the name (the channel can be rebuilt).
                    m_die.WaitOne();
                }
                catch (Exception e)
                {
                    m_attached.TrySetException(e);
                }
                finally
                {
                    channel?.Dispose();
                    seat?.Dispose();
                }
            }

            public void Dispose()
            {
                Die();
                m_die.Dispose();
            }
        }

        /// <summary>
        /// A hand-rolled MMF server whose presence mutex is held by a thread that
        /// can be told to exit without releasing — the process-crash case,
        /// reproduced inside one process.
        /// </summary>
        private sealed class RawServer : IDisposable
        {
            private readonly string m_name;

            private readonly ManualResetEvent m_die = new(false);

            private readonly TaskCompletionSource<bool> m_published = new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly List<IDisposable> m_handles = new();

            private Thread? m_thread;

            private Thread? m_reader;

            private MmfChannel? m_channel;

            public RawServer(string name)
            {
                m_name = name;
            }

            public Task PublishAsync()
            {
                m_thread = new Thread(Run)
                {
                    IsBackground = true
                };

                m_thread.Start();

                return m_published.Task.WaitAsync(TimeSpan.FromMilliseconds(WAIT_MS));
            }

            public void Die()
            {
                m_die.Set();
                m_thread?.Join(WAIT_MS);
                m_channel?.Stop();
                m_reader?.Join(WAIT_MS);
            }

            private void Run()
            {
                try
                {
                    long size = 1024 * 1024;

                    var file = MemoryMappedFile.CreateNew(MmfChannelLayout.FileName(m_name), size, MemoryMappedFileAccess.ReadWrite);
                    m_handles.Add(file);

                    // The accessor is handed to the channel below, which owns and
                    // disposes it, so it is not tracked here separately.
                    var accessor = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);

                    accessor.Write(MmfChannelLayout.FILE_OFFSET_MAGIC, MmfChannelLayout.MAGIC);
                    accessor.Write(MmfChannelLayout.FILE_OFFSET_VERSION, MmfChannelLayout.LAYOUT_VERSION);
                    accessor.Write(MmfChannelLayout.FILE_OFFSET_SIZE, size);
                    accessor.Write(MmfChannelLayout.FILE_OFFSET_CAPACITY, MmfChannelLayout.Capacity(size));

                    var c2sReady = new EventWaitHandle(false, EventResetMode.AutoReset, MmfChannelLayout.ClientToServerReadyName(m_name));
                    var c2sFree = new EventWaitHandle(true, EventResetMode.AutoReset, MmfChannelLayout.ClientToServerFreeName(m_name));
                    var s2cReady = new EventWaitHandle(false, EventResetMode.AutoReset, MmfChannelLayout.ServerToClientReadyName(m_name));
                    var s2cFree = new EventWaitHandle(true, EventResetMode.AutoReset, MmfChannelLayout.ServerToClientFreeName(m_name));

                    m_handles.Add(new Mutex(false, MmfChannelLayout.ClientAliveName(m_name)));

                    m_channel = MmfChannel.ForServer(accessor, size, c2sReady, c2sFree, s2cReady, s2cFree);
                    m_handles.Add(m_channel);

                    // Owned by this thread, never released.
                    var presence = new Mutex(true, MmfChannelLayout.ServerAliveName(m_name), out bool createdNew);
                    m_handles.Add(presence);
                    Assert.That(createdNew, Is.True, "raw server name already in use");

                    var slot = new Semaphore(0, 1, MmfChannelLayout.SlotName(m_name));
                    m_handles.Add(slot);
                    slot.Release();

                    // Answer the client's hello so its connect completes, then wait
                    // to be told to die (which abandons the presence mutex above).
                    m_reader = new Thread(() =>
                    {
                        try
                        {
                            MmfReceiveResult result = m_channel.Receive(CancellationToken.None);
                            if (result.Kind == MmfReceiveKind.Message && result.Flags == MmfFrameFlags.Hello)
                                m_channel.SendAsync(Array.Empty<byte>(), MmfFrameFlags.HelloAck).GetAwaiter().GetResult();
                        }
                        catch (Exception)
                        {
                        }
                    })
                    {
                        IsBackground = true
                    };

                    m_reader.Start();

                    m_published.TrySetResult(true);

                    m_die.WaitOne();
                }
                catch (Exception e)
                {
                    m_published.TrySetException(e);
                }
            }

            public void Dispose()
            {
                Die();

                foreach (var handle in m_handles)
                {
                    try
                    {
                        handle.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }

                m_die.Dispose();
            }
        }

        #endregion
    }
}

using System.Diagnostics;
using OutWit.Communication.LoadTests.Client;
using OutWit.Communication.LoadTests.Metrics;
using OutWit.Communication.LoadTests.Server;
using OutWit.Communication.Server.Callbacks;

namespace OutWit.Communication.LoadTests
{
    /// <summary>
    /// One run: start the server(s), connect the nodes, dispatch tasks at the requested rate for
    /// the requested time, collect the report.
    /// </summary>
    public sealed class LoadRunner
    {
        #region Constants

        private const string TOKEN = "load";

        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan DRAIN = TimeSpan.FromSeconds(5);

        #endregion

        #region Fields

        private readonly LoadOptions m_options;

        private readonly List<LoadHost> m_hosts = new();

        private readonly List<LoadNode> m_nodes = new();

        private readonly Dictionary<string, int> m_outcomes = new();

        private long m_slowPendingMax;

        private long m_anyPendingMax;

        #endregion

        #region Constructors

        public LoadRunner(LoadOptions options)
        {
            m_options = options;
        }

        #endregion

        #region Functions

        public async Task<LoadReport> RunAsync(TextWriter log)
        {
            var report = new LoadReport { Options = m_options.ToString() };
            var process = Process.GetCurrentProcess();
            var cpuBefore = process.TotalProcessorTime;

            try
            {
                StartHosts();
                report.ConnectSeconds = await ConnectNodesAsync(log).ConfigureAwait(false);
                report.Nodes = m_nodes.Count;

                log.WriteLine($"dispatching {m_options.Rate} tasks/s for {m_options.DurationSeconds} s ...");
                var (dispatched, seconds) = await DispatchAsync(log).ConfigureAwait(false);

                await DrainAsync().ConfigureAwait(false);

                report.Dispatched = dispatched;
                report.AchievedRatePerSecond = dispatched / seconds;
                Collect(report);
            }
            finally
            {
                // Teardown is not part of the measurement: a client whose close handshake never
                // completes must not hold the run.
                foreach (var node in m_nodes)
                {
                    try
                    {
                        await node.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                        log.WriteLine($"  node {node.Index} did not disconnect within 3 s; abandoned");
                    }
                }

                foreach (var host in m_hosts)
                {
                    try
                    {
                        await Task.Run(host.Dispose).WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                        log.WriteLine("  a server did not dispose within 3 s; abandoned");
                    }
                }
            }

            process.Refresh();
            report.ServerCpuSeconds = (process.TotalProcessorTime - cpuBefore).TotalSeconds;
            report.WorkingSetMb = process.WorkingSet64 / (1024 * 1024);
            return report;
        }

        private void StartHosts()
        {
            var delivery = m_options.ToDelivery();
            var timeout = TimeSpan.FromMilliseconds(m_options.TimeoutMs);

            Func<int, (TimeSpan, bool)?>? ThrottleFor(int slowArrivalIndex)
            {
                if (m_options.SlowNode < 0)
                    return null;

                return arrival => arrival == slowArrivalIndex
                    ? (TimeSpan.FromMilliseconds(m_options.SlowDelayMs), m_options.Stall)
                    : null;
            }

            if (m_options.Mode == "per-client")
            {
                var watch = Stopwatch.StartNew();
                for (var i = 0; i < m_options.Nodes; i++)
                    m_hosts.Add(LoadHost.Start(m_options.WebSocket, 4, TOKEN, timeout, delivery, i == m_options.SlowNode ? ThrottleFor(0) : null));

                Console.WriteLine($"{m_hosts.Count} per-client servers started in {watch.Elapsed.TotalSeconds:F1} s");
                return;
            }

            m_hosts.Add(LoadHost.Start(m_options.WebSocket, m_options.Nodes + 8, TOKEN, timeout, delivery, ThrottleFor(m_options.SlowNode)));
        }

        private async Task<double> ConnectNodesAsync(TextWriter log)
        {
            var watch = Stopwatch.StartNew();

            // Sequential on purpose: the throttle is chosen by arrival order, so node K must be
            // the K-th connection of the shared server.
            for (var i = 0; i < m_options.Nodes; i++)
            {
                var host = m_options.Mode == "per-client" ? m_hosts[i] : m_hosts[0];
                var node = new LoadNode(i, host.ClientEndpoint, m_options.WebSocket, TOKEN, CONNECT_TIMEOUT);

                using var connectCancellation = new CancellationTokenSource(CONNECT_TIMEOUT);
                if (!await node.ConnectAsync(connectCancellation.Token).ConfigureAwait(false))
                    throw new InvalidOperationException($"node {i} could not connect");

                m_nodes.Add(node);

                if ((i + 1) % 10 == 0)
                    log.WriteLine($"  {i + 1} nodes connected ({watch.Elapsed.TotalSeconds:F1} s)");
            }

            watch.Stop();
            log.WriteLine($"{m_nodes.Count} nodes connected in {watch.Elapsed.TotalSeconds:F2} s");
            return watch.Elapsed.TotalSeconds;
        }

        private async Task<(int Dispatched, double Seconds)> DispatchAsync(TextWriter log)
        {
            var payload = new byte[m_options.PayloadBytes];
            Random.Shared.NextBytes(payload);

            var interval = TimeSpan.FromSeconds(1.0 / m_options.Rate);
            var deadline = Stopwatch.StartNew();
            var dispatched = 0;
            var inFlight = new List<Task>();
            var nextAt = TimeSpan.Zero;
            var sampler = SampleSlowPendingAsync(deadline);

            while (deadline.Elapsed < TimeSpan.FromSeconds(m_options.DurationSeconds))
            {
                var wait = nextAt - deadline.Elapsed;
                if (wait > TimeSpan.Zero)
                    await Task.Delay(wait).ConfigureAwait(false);

                nextAt += interval;
                var node = dispatched % m_options.Nodes;
                dispatched++;

                inFlight.Add(DispatchOneAsync(node, payload));

                if (inFlight.Count >= 1024)
                {
                    inFlight.RemoveAll(task => task.IsCompleted);
                    if (inFlight.Count >= 4096)
                        await Task.WhenAny(inFlight).ConfigureAwait(false);
                }

                if (dispatched % (m_options.Rate * 5) == 0)
                    log.WriteLine($"  {deadline.Elapsed.TotalSeconds:F0} s: {dispatched} dispatched, {TotalAcked()} acked");
            }

            var seconds = deadline.Elapsed.TotalSeconds;
            await Task.WhenAll(inFlight).ConfigureAwait(false);
            await sampler.ConfigureAwait(false);
            return (dispatched, seconds);
        }

        private async Task DispatchOneAsync(int node, byte[] payload)
        {
            switch (m_options.Mode)
            {
                case "per-client":
                    m_hosts[node].Service.DispatchToOnlyConnection(node, payload);
                    break;

                case "broadcast":
                    m_hosts[0].Service.DispatchBroadcast(node, payload);
                    break;

                default:
                    var status = await m_hosts[0].Service.DispatchAsync(node, payload).ConfigureAwait(false);
                    lock (m_outcomes)
                        m_outcomes[status.ToString()] = m_outcomes.GetValueOrDefault(status.ToString()) + 1;
                    break;
            }
        }

        private async Task SampleSlowPendingAsync(Stopwatch clock)
        {
            while (clock.Elapsed < TimeSpan.FromSeconds(m_options.DurationSeconds))
            {
                for (var i = 0; i < m_nodes.Count; i++)
                {
                    var host = m_options.Mode == "per-client" ? m_hosts[i] : m_hosts[0];
                    var pending = host.Server.GetPendingCallbacks(m_nodes[i].ConnectionId);

                    if (i == m_options.SlowNode && pending > m_slowPendingMax)
                        m_slowPendingMax = pending;

                    if (i != m_options.SlowNode && pending > m_anyPendingMax)
                        m_anyPendingMax = pending;
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        private async Task DrainAsync()
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < DRAIN && m_hosts.Any(host => host.Service.InFlight > 0))
                await Task.Delay(50).ConfigureAwait(false);
        }

        private int TotalAcked()
        {
            return m_hosts.Sum(host => host.Service.Latency.Count);
        }

        private void Collect(LoadReport report)
        {
            var all = new LatencyHistogram();
            var neighbours = new LatencyHistogram();
            var slow = new LatencyHistogram();

            foreach (var host in m_hosts)
            {
                foreach (var (node, histogram) in host.Service.PerNode)
                {
                    var summary = histogram.Summarize();
                    _ = summary;

                    foreach (var sample in Samples(histogram))
                    {
                        all.Add(sample);
                        if (m_options.SlowNode >= 0)
                            (node == m_options.SlowNode ? slow : neighbours).Add(sample);
                    }
                }
            }

            report.Acked = all.Count;
            report.AllNodes = all.Summarize();
            report.AnyNodePendingMax = m_anyPendingMax;

            if (m_options.SlowNode >= 0)
            {
                report.NeighboursOfSlow = neighbours.Summarize();
                report.SlowNode = slow.Summarize();
                report.SlowNodePendingMax = m_slowPendingMax;

                var throttled = m_hosts.Select(host => host.Throttling).FirstOrDefault(factory => factory != null)?.Throttled.Values.FirstOrDefault();
                if (throttled?.DisconnectedAt != null)
                {
                    report.SlowNodeClosed = true;
                    report.SlowNodeClosedAfterSeconds = (throttled.DisconnectedAt.Value - RunStartedAt).TotalSeconds;
                }
            }

            lock (m_outcomes)
                report.Outcomes = new Dictionary<string, int>(m_outcomes);

            if (m_options.Mode == "broadcast")
                report.BroadcastFramesPerNode = (long)m_nodes.Average(node => node.Received);
        }

        private static IEnumerable<double> Samples(LatencyHistogram histogram)
        {
            return histogram.Drain();
        }

        #endregion

        #region Properties

        private DateTime RunStartedAt { get; } = DateTime.UtcNow;

        #endregion
    }
}

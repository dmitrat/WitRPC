using System.Collections.Concurrent;
using System.Diagnostics;
using OutWit.Communication.LoadTests.Contracts;
using OutWit.Communication.LoadTests.Metrics;
using OutWit.Communication.Server.Callbacks;
using OutWit.Communication.Server.Connections;

namespace OutWit.Communication.LoadTests.Server
{
    /// <summary>
    /// The service the harness hosts: nodes attach (the connection id comes from
    /// <see cref="ConnectionContext"/>), tasks are pushed through the event inside a
    /// <see cref="CallbackScope"/> (or without one, for the broadcast comparison), and each
    /// acknowledgement closes the dispatch-to-ack latency of its task.
    /// </summary>
    public sealed class LoadService : ILoadService
    {
        #region Events

        public event Action<LoadTask> TaskReceived = delegate { };

        #endregion

        #region Constants

        private static readonly TimeSpan COMPLETION_WAIT = TimeSpan.FromSeconds(10);

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<long, long> m_inFlight = new();

        private long m_nextTaskId;

        #endregion

        #region ILoadService

        public Guid Attach(int nodeIndex)
        {
            var context = ConnectionContext.Current ?? throw new InvalidOperationException("Attach outside a request");
            Connections[nodeIndex] = context.ConnectionId;
            return context.ConnectionId;
        }

        public Task AckAsync(long taskId, int nodeIndex)
        {
            if (!m_inFlight.TryRemove(taskId, out var dispatchedAt))
                return Task.CompletedTask;

            var elapsedUs = (Stopwatch.GetTimestamp() - dispatchedAt) * 1_000_000.0 / Stopwatch.Frequency;
            Latency.Add(elapsedUs);
            PerNode.GetOrAdd(nodeIndex, _ => new LatencyHistogram()).Add(elapsedUs);
            return Task.CompletedTask;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Pushes one task to one node through a targeted callback and returns what the server did.
        /// </summary>
        public async Task<CallbackDeliveryStatus> DispatchAsync(int nodeIndex, byte[] payload)
        {
            if (!Connections.TryGetValue(nodeIndex, out var connectionId))
                return CallbackDeliveryStatus.UnknownConnection;

            var task = NewTask(nodeIndex, payload);

            using var scope = CallbackScope.Target(connectionId);
            TaskReceived(task);

            // Under the Log policy a stalled socket's write never completes, by design; the
            // harness gives up waiting after a while and reports the callback as still queued.
            CallbackDeliveryReport report;
            try
            {
                report = await scope.Completion.WaitAsync(COMPLETION_WAIT).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                report = scope.Report;
            }

            var status = report.Outcomes.Count == 0 ? CallbackDeliveryStatus.Refused : report.Outcomes[0].Status;

            if (status != CallbackDeliveryStatus.Sent)
                m_inFlight.TryRemove(task.Id, out _);

            return status;
        }

        /// <summary>
        /// Pushes one task to every connection (the pre-3.2 shape) and lets one node ack it.
        /// </summary>
        public void DispatchBroadcast(int nodeIndex, byte[] payload)
        {
            TaskReceived(NewTask(nodeIndex, payload));
        }

        /// <summary>
        /// Pushes a task without a scope: what a per-client server does (its only connection is the node).
        /// </summary>
        public void DispatchToOnlyConnection(int nodeIndex, byte[] payload)
        {
            TaskReceived(NewTask(nodeIndex, payload));
        }

        private LoadTask NewTask(int nodeIndex, byte[] payload)
        {
            var task = new LoadTask
            {
                Id = Interlocked.Increment(ref m_nextTaskId),
                NodeIndex = nodeIndex,
                DispatchedAtTicks = Stopwatch.GetTimestamp(),
                Payload = payload
            };

            m_inFlight[task.Id] = task.DispatchedAtTicks;
            return task;
        }

        #endregion

        #region Properties

        public ConcurrentDictionary<int, Guid> Connections { get; } = new();

        public LatencyHistogram Latency { get; } = new();

        public ConcurrentDictionary<int, LatencyHistogram> PerNode { get; } = new();

        public int InFlight => m_inFlight.Count;

        #endregion
    }
}

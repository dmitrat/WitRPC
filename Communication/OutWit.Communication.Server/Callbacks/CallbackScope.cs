using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Server.Connections;

namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// Names the connections an event raised inside the scope is delivered to. A service opens the
    /// scope, raises its ordinary C# event, and the server that hosts the service reads the target at
    /// the moment the callback reaches it (the raise chain is synchronous, so an
    /// <see cref="AsyncLocal{T}"/> set here is visible there; it also flows into tasks started inside
    /// the scope). Without a scope the server delivers to every authorized connection, as it always
    /// did. The scope collects what happened: <see cref="Report"/> for the state now,
    /// <see cref="Completion"/> for the state once every accepted send finished.
    /// </summary>
    /// <example>
    /// <code>
    /// using var scope = CallbackScope.Target(connectionId);
    /// TaskReceived(delivery);                       // the contract's event
    /// var report = await scope.Completion;          // Sent, or UnknownConnection / QueueFull / ...
    /// </code>
    /// </example>
    public sealed class CallbackScope : IDisposable
    {
        #region Fields

        private static readonly AsyncLocal<CallbackScope?> CURRENT = new();

        private readonly CallbackScope? m_previous;

        private readonly object m_gate = new();

        private readonly List<PendingOutcome> m_outcomes = new();

        private int m_raised;

        private bool m_disposed;

        #endregion

        #region Constructors

        private CallbackScope(CallbackTarget recipients)
        {
            Recipients = recipients;
            m_previous = CURRENT.Value;
            CURRENT.Value = this;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens a scope whose events go to one connection.
        /// </summary>
        /// <param name="connectionId">The connection (see <see cref="ConnectionContext.ConnectionId"/>).</param>
        /// <returns>The scope; dispose it when the raise is done.</returns>
        public static CallbackScope Target(Guid connectionId)
        {
            return new CallbackScope(CallbackTarget.Connection(connectionId));
        }

        /// <summary>
        /// Opens a scope whose events go to a set of connections.
        /// </summary>
        /// <param name="connectionIds">The connections; an empty set targets nobody.</param>
        /// <returns>The scope; dispose it when the raise is done.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionIds"/> is null.</exception>
        public static CallbackScope Target(IReadOnlyCollection<Guid> connectionIds)
        {
            return new CallbackScope(CallbackTarget.Connections(connectionIds));
        }

        /// <summary>
        /// Opens a scope whose events go to the connection whose request is being processed:
        /// "reply to the caller" without knowing its id.
        /// </summary>
        /// <returns>The scope; dispose it when the raise is done.</returns>
        /// <exception cref="InvalidOperationException">No request is being processed on this async flow.</exception>
        public static CallbackScope TargetCaller()
        {
            var context = ConnectionContext.Current
                          ?? throw new InvalidOperationException("No request is being processed on this async flow; there is no caller to target");

            return Target(context.ConnectionId);
        }

        /// <summary>
        /// Counts one event raise seen by a server.
        /// </summary>
        internal void RecordRaise()
        {
            Interlocked.Increment(ref m_raised);
        }

        /// <summary>
        /// Records what a server did with the callback for one target connection.
        /// </summary>
        /// <param name="serverId">The server.</param>
        /// <param name="connectionId">The target; <see cref="Guid.Empty"/> for a refused broadcast.</param>
        /// <param name="status">The status at enqueue time.</param>
        /// <param name="completion">The send's final status, when the callback was queued.</param>
        internal void Record(Guid serverId, Guid connectionId, CallbackDeliveryStatus status, Task<CallbackDeliveryStatus>? completion)
        {
            lock (m_gate)
                m_outcomes.Add(new PendingOutcome(serverId, connectionId, status, completion));
        }

        public override string ToString()
        {
            return $"callback scope: {Recipients}";
        }

        private CallbackDeliveryReport TakeReport()
        {
            PendingOutcome[] pending;
            lock (m_gate)
                pending = m_outcomes.ToArray();

            var outcomes = pending
                .Select(outcome => new CallbackDeliveryOutcome(outcome.ServerId, outcome.ConnectionId, outcome.StatusNow))
                .ToArray();

            return new CallbackDeliveryReport(Volatile.Read(ref m_raised), outcomes);
        }

        private async Task<CallbackDeliveryReport> WaitForCompletionAsync()
        {
            Task[] sends;
            lock (m_gate)
                sends = m_outcomes.Where(outcome => outcome.Completion != null).Select(outcome => (Task)outcome.Completion!).ToArray();

            if (sends.Length > 0)
                await Task.WhenAll(sends).ConfigureAwait(false);

            return TakeReport();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Closes the scope: events raised afterwards on this flow are no longer targeted by it.
        /// The report stays readable.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;
            CURRENT.Value = m_previous;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The target of the scope open on this async flow, or <see cref="CallbackTarget.Broadcast"/>
        /// when none is.
        /// </summary>
        public static CallbackTarget Current => CURRENT.Value?.Recipients ?? CallbackTarget.Broadcast;

        /// <summary>
        /// The scope open on this async flow, or null.
        /// </summary>
        internal static CallbackScope? CurrentScope => CURRENT.Value;

        /// <summary>
        /// Who the events raised inside this scope go to.
        /// </summary>
        public CallbackTarget Recipients { get; }

        /// <summary>
        /// What has happened so far: a callback still in a queue reads as <see cref="CallbackDeliveryStatus.Queued"/>.
        /// </summary>
        public CallbackDeliveryReport Report => TakeReport();

        /// <summary>
        /// The report once every callback accepted so far has been written or has failed. Await it
        /// after the raise, inside or after the <c>using</c>.
        /// </summary>
        public Task<CallbackDeliveryReport> Completion => WaitForCompletionAsync();

        #endregion

        #region Nested Types

        private sealed class PendingOutcome
        {
            public PendingOutcome(Guid serverId, Guid connectionId, CallbackDeliveryStatus status, Task<CallbackDeliveryStatus>? completion)
            {
                ServerId = serverId;
                ConnectionId = connectionId;
                Status = status;
                Completion = completion;
            }

            public Guid ServerId { get; }

            public Guid ConnectionId { get; }

            public CallbackDeliveryStatus Status { get; }

            public Task<CallbackDeliveryStatus>? Completion { get; }

            public CallbackDeliveryStatus StatusNow =>
                Completion is { IsCompletedSuccessfully: true } ? Completion.Result : Status;
        }

        #endregion
    }
}

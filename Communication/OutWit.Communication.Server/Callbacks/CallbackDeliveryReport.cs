using System.Collections.Generic;
using System.Linq;

namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// What became of the events raised inside a <see cref="CallbackScope"/>: how many were raised
    /// and, per server and target connection, what happened to each. A snapshot; read
    /// <see cref="CallbackScope.Report"/> for the current state or await
    /// <see cref="CallbackScope.Completion"/> for the final one.
    /// </summary>
    public sealed class CallbackDeliveryReport
    {
        #region Constructors

        public CallbackDeliveryReport(int raised, IReadOnlyList<CallbackDeliveryOutcome> outcomes)
        {
            Raised = raised;
            Outcomes = outcomes;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Raised} raised: {string.Join(", ", Outcomes.Select(outcome => outcome.ToString()))}";
        }

        #endregion

        #region Properties

        /// <summary>
        /// How many event raises the scope saw (each server that hosts the service counts the raise once).
        /// </summary>
        public int Raised { get; }

        /// <summary>
        /// One line per (server, target connection); a refused broadcast is one line with an empty connection id.
        /// </summary>
        public IReadOnlyList<CallbackDeliveryOutcome> Outcomes { get; }

        /// <summary>
        /// True when at least one callback reached a transport.
        /// </summary>
        public bool AnySent => Outcomes.Any(outcome => outcome.Status == CallbackDeliveryStatus.Sent);

        /// <summary>
        /// True when at least one callback was accepted (queued or sent) somewhere.
        /// </summary>
        public bool AnyAccepted => Outcomes.Any(outcome => outcome.Status is CallbackDeliveryStatus.Queued or CallbackDeliveryStatus.Sent);

        #endregion
    }
}

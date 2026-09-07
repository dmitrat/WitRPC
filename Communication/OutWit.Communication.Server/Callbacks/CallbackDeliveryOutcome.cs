using System;

namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// One (server, connection) line of a <see cref="CallbackDeliveryReport"/>.
    /// </summary>
    public sealed class CallbackDeliveryOutcome
    {
        #region Constructors

        public CallbackDeliveryOutcome(Guid serverId, Guid connectionId, CallbackDeliveryStatus status)
        {
            ServerId = serverId;
            ConnectionId = connectionId;
            Status = status;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Status} (connection {ConnectionId} on server {ServerId})";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The server that handled the raise (a service registered in several servers is raised in each).
        /// </summary>
        public Guid ServerId { get; }

        /// <summary>
        /// The targeted connection; <see cref="Guid.Empty"/> for a <see cref="CallbackDeliveryStatus.Refused"/> raise.
        /// </summary>
        public Guid ConnectionId { get; }

        /// <summary>
        /// The status at the time the report was taken.
        /// </summary>
        public CallbackDeliveryStatus Status { get; }

        #endregion
    }
}

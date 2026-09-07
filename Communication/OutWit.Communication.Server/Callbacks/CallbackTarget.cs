using System;
using System.Collections.Generic;

namespace OutWit.Communication.Server.Callbacks
{
    /// <summary>
    /// Who receives a callback: every authorized connection of the server (a broadcast), one
    /// connection, or a set of connections.
    /// </summary>
    public readonly struct CallbackTarget
    {
        #region Constants

        private static readonly Guid[] NO_CONNECTIONS = Array.Empty<Guid>();

        #endregion

        #region Constructors

        private CallbackTarget(IReadOnlyCollection<Guid>? connectionIds)
        {
            ConnectionIds = connectionIds ?? NO_CONNECTIONS;
            IsBroadcast = connectionIds == null;
        }

        #endregion

        #region Functions

        /// <summary>
        /// A callback for one connection.
        /// </summary>
        /// <param name="connectionId">The connection (see <see cref="Connections.ConnectionContext.ConnectionId"/>).</param>
        /// <returns>The target.</returns>
        public static CallbackTarget Connection(Guid connectionId)
        {
            return new CallbackTarget(new[] { connectionId });
        }

        /// <summary>
        /// A callback for a set of connections. An empty set targets nobody (and is not a broadcast).
        /// </summary>
        /// <param name="connectionIds">The connections.</param>
        /// <returns>The target.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionIds"/> is null.</exception>
        public static CallbackTarget Connections(IReadOnlyCollection<Guid> connectionIds)
        {
            if (connectionIds == null)
                throw new ArgumentNullException(nameof(connectionIds));

            return new CallbackTarget(connectionIds);
        }

        /// <summary>
        /// Whether <paramref name="connectionId"/> is among the targets (always true for a broadcast).
        /// </summary>
        /// <param name="connectionId">The connection.</param>
        /// <returns>True when the connection should receive the callback.</returns>
        public bool Includes(Guid connectionId)
        {
            if (IsBroadcast)
                return true;

            foreach (var id in ConnectionIds)
            {
                if (id == connectionId)
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            return IsBroadcast ? "broadcast" : $"{ConnectionIds.Count} connection(s)";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Every authorized connection of the server; what an event raised outside a
        /// <see cref="CallbackScope"/> targets.
        /// </summary>
        public static CallbackTarget Broadcast => new(null);

        /// <summary>
        /// True for <see cref="Broadcast"/>.
        /// </summary>
        public bool IsBroadcast { get; }

        /// <summary>
        /// The targeted connections; empty for a broadcast.
        /// </summary>
        public IReadOnlyCollection<Guid> ConnectionIds { get; }

        #endregion
    }
}

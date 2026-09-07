using System;
using System.Security.Claims;
using System.Threading;

namespace OutWit.Communication.Server.Connections
{
    /// <summary>
    /// What a service can know about the connection a request arrived on: the connection id, the
    /// server that accepted it, its transport, and the principal established when that connection
    /// authorized. An immutable snapshot, taken per request; a service never sees the mutable
    /// <see cref="ConnectionInfo"/>. Read it through <see cref="Current"/> (an
    /// <see cref="AsyncLocal{T}"/> the server sets around every request and around the authorization
    /// handshake) or through an <see cref="IConnectionContextAccessor"/>.
    /// </summary>
    public sealed class ConnectionContext
    {
        #region Fields

        private static readonly AsyncLocal<ConnectionContext?> CURRENT = new();

        #endregion

        #region Constructors

        public ConnectionContext(Guid connectionId, Guid serverId, string? serverName, string transport,
            ClaimsPrincipal? principal, DateTimeOffset authorizedAtUtc)
        {
            ConnectionId = connectionId;
            ServerId = serverId;
            ServerName = serverName;
            Transport = transport;
            Principal = principal;
            AuthorizedAtUtc = authorizedAtUtc;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Makes <paramref name="context"/> the current one until the returned scope is disposed,
        /// then restores what was current before. The server uses it around each request; a unit
        /// test of a service uses it to simulate a connection.
        /// </summary>
        /// <param name="context">The context to expose; null clears the current one.</param>
        /// <returns>A scope that restores the previous context on dispose.</returns>
        public static IDisposable BeginScope(ConnectionContext? context)
        {
            var previous = CURRENT.Value;
            CURRENT.Value = context;
            return new Scope(previous);
        }

        public override string ToString()
        {
            return $"connection {ConnectionId} on server {ServerName ?? ServerId.ToString()} ({Transport})";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The context of the request being processed on this async flow, or null outside a
        /// request (a timer, a background task, a raise from another thread).
        /// </summary>
        public static ConnectionContext? Current => CURRENT.Value;

        /// <summary>
        /// The connection the request arrived on; unique per accepted transport for its lifetime.
        /// </summary>
        public Guid ConnectionId { get; }

        /// <summary>
        /// The <see cref="WitServer.Id"/> of the server that accepted the connection.
        /// </summary>
        public Guid ServerId { get; }

        /// <summary>
        /// The <see cref="WitServer.Name"/> of that server, when it has one.
        /// </summary>
        public string? ServerName { get; }

        /// <summary>
        /// The transport name the server listens on (<c>IServerOptions.Transport</c>).
        /// </summary>
        public string Transport { get; }

        /// <summary>
        /// The principal established at authorization when the token validator implements
        /// <see cref="Authorization.IConnectionAuthenticator"/>; null otherwise, and null during
        /// the authorization handshake itself.
        /// </summary>
        public ClaimsPrincipal? Principal { get; }

        /// <summary>
        /// When the connection authorized; <see cref="DateTimeOffset.MinValue"/> during the handshake.
        /// </summary>
        public DateTimeOffset AuthorizedAtUtc { get; }

        #endregion

        #region Nested Types

        private sealed class Scope : IDisposable
        {
            private readonly ConnectionContext? m_previous;

            private bool m_disposed;

            public Scope(ConnectionContext? previous)
            {
                m_previous = previous;
            }

            public void Dispose()
            {
                if (m_disposed)
                    return;

                m_disposed = true;
                CURRENT.Value = m_previous;
            }
        }

        #endregion
    }
}

using System;

namespace OutWit.Communication.Tests.Mock.Interfaces
{
    /// <summary>
    /// A contract for the connection-context and targeted-callback tests: the service
    /// answers who is calling and raises its one event to whoever the method names.
    /// </summary>
    public interface IConnectionAwareService
    {
        /// <summary>Raised by the Notify methods; the payload is the message given.</summary>
        event Action<string> Notified;

        /// <summary>The id of the connection the call arrived on, or <see cref="Guid.Empty"/> without a context.</summary>
        Guid WhoAmI();

        /// <summary>The name of the principal the connection authorized with, or null.</summary>
        string? WhoAmIPrincipal();

        /// <summary>The id of the server the call arrived on.</summary>
        Guid WhichServer();

        /// <summary>Whether a context is visible after an await inside the method.</summary>
        Guid WhoAmIAfterAwait();

        /// <summary>Raises <see cref="Notified"/> to every authorized connection.</summary>
        void NotifyAll(string message);

        /// <summary>Raises <see cref="Notified"/> to the calling connection only.</summary>
        void NotifyCaller(string message);

        /// <summary>Raises <see cref="Notified"/> to one connection.</summary>
        void NotifyOne(Guid connectionId, string message);

        /// <summary>Raises <see cref="Notified"/> to a set of connections.</summary>
        void NotifyMany(Guid[] connectionIds, string message);
    }
}

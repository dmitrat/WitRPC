using System;

namespace OutWit.Communication.Server.DependencyInjection.Interfaces
{
    /// <summary>
    /// Factory for creating and managing WitRPC server instances.
    /// </summary>
    public interface IWitServerFactory
    {
        /// <summary>
        /// Gets a configured WitServer by name.
        /// </summary>
        /// <param name="name">The name of the server configuration.</param>
        /// <returns>The configured WitServer instance.</returns>
        WitServer GetServer(string name);
    }
}

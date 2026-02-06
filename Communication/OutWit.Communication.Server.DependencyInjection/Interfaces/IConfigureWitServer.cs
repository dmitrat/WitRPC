using System;

namespace OutWit.Communication.Server.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for WitServer configuration.
    /// </summary>
    public interface IConfigureWitServer
    {
        /// <summary>
        /// Gets the name of the server configuration.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Configures the server options.
        /// </summary>
        /// <param name="context">The server builder context containing options and service provider.</param>
        void Configure(WitServerBuilderContext context);
    }
}

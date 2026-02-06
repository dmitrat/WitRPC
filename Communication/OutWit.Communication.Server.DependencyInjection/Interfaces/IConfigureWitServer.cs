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
        /// <param name="options">The server builder options to configure.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        void Configure(WitServerBuilderOptions options, IServiceProvider serviceProvider);
    }
}

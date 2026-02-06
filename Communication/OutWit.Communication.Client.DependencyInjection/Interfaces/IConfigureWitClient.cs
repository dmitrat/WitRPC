using System;

namespace OutWit.Communication.Client.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for WitClient configuration.
    /// </summary>
    public interface IConfigureWitClient
    {
        /// <summary>
        /// Gets the name of the client configuration.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Configures the client options.
        /// </summary>
        /// <param name="options">The client builder options to configure.</param>
        void Configure(WitClientBuilderOptions options);
    }
}

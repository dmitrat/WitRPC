using System;

namespace OutWit.Communication.Client.DependencyInjection.Interfaces
{
    /// <summary>
    /// Factory for creating and managing WitRPC client instances.
    /// </summary>
    public interface IWitClientFactory
    {
        /// <summary>
        /// Gets a configured WitClient by name.
        /// </summary>
        /// <param name="name">The name of the client configuration.</param>
        /// <returns>The configured WitClient instance.</returns>
        WitClient GetClient(string name);

        /// <summary>
        /// Gets a service proxy for the specified interface from a named client.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="strongAssemblyMatch">Whether to use strong assembly matching.</param>
        /// <returns>A proxy implementing the service interface.</returns>
        TService GetService<TService>(string name, bool strongAssemblyMatch = true) where TService : class;
    }
}

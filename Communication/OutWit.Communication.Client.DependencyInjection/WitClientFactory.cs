using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Client;

namespace OutWit.Communication.Client.DependencyInjection
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

    /// <summary>
    /// Default implementation of IWitClientFactory.
    /// </summary>
    public class WitClientFactory : IWitClientFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, WitClient> _clients = new();
        private readonly ConcurrentDictionary<string, IConfigureWitClient> _configurations = new();
        private readonly object _lock = new();

        public WitClientFactory(IEnumerable<IConfigureWitClient> configurations)
        {
            foreach (var config in configurations)
            {
                _configurations[config.Name] = config;
            }
        }

        /// <inheritdoc />
        public WitClient GetClient(string name)
        {
            if (_clients.TryGetValue(name, out var existingClient))
                return existingClient;

            lock (_lock)
            {
                if (_clients.TryGetValue(name, out existingClient))
                    return existingClient;

                if (!_configurations.TryGetValue(name, out var configure))
                    throw new InvalidOperationException($"No WitRPC client configuration found for name '{name}'. Make sure to register it using AddWitRpcClient.");

                var options = new WitClientBuilderOptions();
                configure.Configure(options);
                var client = WitClientBuilder.Build(options);
                
                _clients[name] = client;
                return client;
            }
        }

        /// <inheritdoc />
        public TService GetService<TService>(string name, bool strongAssemblyMatch = true) where TService : class
        {
            var client = GetClient(name);
            return client.GetService<TService>(strongAssemblyMatch);
        }

        /// <summary>
        /// Gets all registered client names.
        /// </summary>
        public IEnumerable<string> GetClientNames() => _configurations.Keys;

        /// <summary>
        /// Connects all registered clients.
        /// </summary>
        public async Task ConnectAllAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            foreach (var name in _configurations.Keys)
            {
                var client = GetClient(name);
                await client.ConnectAsync(timeout, cancellationToken);
            }
        }

        /// <summary>
        /// Disconnects all clients.
        /// </summary>
        public async Task DisconnectAllAsync()
        {
            foreach (var client in _clients.Values)
            {
                await client.Disconnect();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
            _clients.Clear();
        }
    }
}

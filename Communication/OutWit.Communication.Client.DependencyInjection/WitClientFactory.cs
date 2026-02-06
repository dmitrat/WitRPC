using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Default implementation of <see cref="IWitClientFactory"/>.
    /// </summary>
    public sealed class WitClientFactory : IWitClientFactory, IDisposable
    {
        #region Fields

        private readonly ConcurrentDictionary<string, WitClient> m_clients = new();
        private readonly ConcurrentDictionary<string, IConfigureWitClient> m_configurations = new();
        private readonly object m_lock = new();

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitClientFactory"/>.
        /// </summary>
        /// <param name="configurations">The registered client configurations.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurations"/> is null.</exception>
        public WitClientFactory(IEnumerable<IConfigureWitClient> configurations)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            foreach (var config in configurations)
            {
                m_configurations[config.Name] = config;
            }
        }

        #endregion

        #region IWitClientFactory

        /// <inheritdoc />
        public WitClient GetClient(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (m_disposed)
                throw new ObjectDisposedException(nameof(WitClientFactory));

            if (m_clients.TryGetValue(name, out var existingClient))
                return existingClient;

            lock (m_lock)
            {
                if (m_clients.TryGetValue(name, out existingClient))
                    return existingClient;

                if (!m_configurations.TryGetValue(name, out var configure))
                    throw new InvalidOperationException($"No WitRPC client configuration found for name '{name}'. Make sure to register it using AddWitRpcClient.");

                var options = new WitClientBuilderOptions();
                configure.Configure(options);
                var client = WitClientBuilder.Build(options);
                
                m_clients[name] = client;
                return client;
            }
        }

        /// <inheritdoc />
        public TService GetService<TService>(string name, bool strongAssemblyMatch = true) where TService : class
        {
            var client = GetClient(name);
            return client.GetService<TService>(strongAssemblyMatch);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Gets all registered client names.
        /// </summary>
        /// <returns>A collection of registered client names.</returns>
        public IEnumerable<string> GetClientNames() => m_configurations.Keys;

        /// <summary>
        /// Checks whether a client with the given name has been created.
        /// </summary>
        /// <param name="name">The name of the client configuration.</param>
        /// <returns><c>true</c> if the client has been created; otherwise, <c>false</c>.</returns>
        public bool HasClient(string name) => m_clients.ContainsKey(name);

        /// <summary>
        /// Connects all registered clients.
        /// </summary>
        /// <param name="timeout">The connection timeout for each client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ConnectAllAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            foreach (var name in m_configurations.Keys)
            {
                var client = GetClient(name);
                await client.ConnectAsync(timeout, cancellationToken);
            }
        }

        /// <summary>
        /// Disconnects all created clients.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DisconnectAllAsync()
        {
            foreach (var client in m_clients.Values)
            {
                await client.Disconnect();
            }
        }

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            foreach (var client in m_clients.Values)
            {
                client.Dispose();
            }
            m_clients.Clear();
        }

        #endregion
    }
}

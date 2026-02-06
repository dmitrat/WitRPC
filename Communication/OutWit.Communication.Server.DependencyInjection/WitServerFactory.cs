using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OutWit.Communication.Server;
using OutWit.Communication.Server.DependencyInjection.Interfaces;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Default implementation of <see cref="IWitServerFactory"/>.
    /// </summary>
    public sealed class WitServerFactory : IWitServerFactory, IDisposable
    {
        #region Fields

        private readonly ConcurrentDictionary<string, WitServer> m_servers = new();
        private readonly ConcurrentDictionary<string, IConfigureWitServer> m_configurations = new();
        private readonly IServiceProvider m_serviceProvider;
        private readonly object m_lock = new();

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitServerFactory"/>.
        /// </summary>
        /// <param name="configurations">The registered server configurations.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurations"/> or <paramref name="serviceProvider"/> is null.</exception>
        public WitServerFactory(IEnumerable<IConfigureWitServer> configurations, IServiceProvider serviceProvider)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            m_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            foreach (var config in configurations)
            {
                m_configurations[config.Name] = config;
            }
        }

        #endregion

        #region IWitServerFactory

        /// <inheritdoc />
        public WitServer GetServer(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (m_disposed)
                throw new ObjectDisposedException(nameof(WitServerFactory));

            if (m_servers.TryGetValue(name, out var existingServer))
                return existingServer;

            lock (m_lock)
            {
                if (m_servers.TryGetValue(name, out existingServer))
                    return existingServer;

                if (!m_configurations.TryGetValue(name, out var configure))
                    throw new InvalidOperationException($"No WitRPC server configuration found for name '{name}'. Make sure to register it using AddWitRpcServer.");

                var options = new WitServerBuilderOptions();
                var context = new WitServerBuilderContext(options, m_serviceProvider);
                configure.Configure(context);
                var server = WitServerBuilder.Build(options);
                
                m_servers[name] = server;
                return server;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Gets all registered server names.
        /// </summary>
        /// <returns>A collection of registered server names.</returns>
        public IEnumerable<string> GetServerNames() => m_configurations.Keys;

        /// <summary>
        /// Checks whether a server with the given name has been created.
        /// </summary>
        /// <param name="name">The name of the server configuration.</param>
        /// <returns><c>true</c> if the server has been created; otherwise, <c>false</c>.</returns>
        public bool HasServer(string name) => m_servers.ContainsKey(name);

        /// <summary>
        /// Starts all registered servers.
        /// </summary>
        public void StartAll()
        {
            foreach (var name in m_configurations.Keys)
            {
                var server = GetServer(name);
                server.StartWaitingForConnection();
            }
        }

        /// <summary>
        /// Stops all created servers.
        /// </summary>
        public void StopAll()
        {
            foreach (var server in m_servers.Values)
            {
                server.StopWaitingForConnection();
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

            foreach (var server in m_servers.Values)
            {
                server.StopWaitingForConnection();
                server.Dispose();
            }
            m_servers.Clear();
        }

        #endregion
    }
}

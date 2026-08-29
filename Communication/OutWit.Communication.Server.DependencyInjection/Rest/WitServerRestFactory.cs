using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OutWit.Communication.Server.DependencyInjection.Interfaces;
using OutWit.Communication.Server.Rest;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Builds each named REST server once, from its registered configuration,
    /// and owns it for the life of the container.
    /// </summary>
    public sealed class WitServerRestFactory : IWitServerRestFactory, IDisposable
    {
        #region Fields

        private readonly ConcurrentDictionary<string, WitServerRest> m_servers = new();

        private readonly ConcurrentDictionary<string, IConfigureWitServerRest> m_configurations = new();

        private readonly IServiceProvider m_serviceProvider;

        private readonly object m_lock = new();

        private bool m_disposed;

        #endregion

        #region Constructors

        public WitServerRestFactory(IEnumerable<IConfigureWitServerRest> configurations, IServiceProvider serviceProvider)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            m_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            foreach (var configuration in configurations)
                m_configurations[configuration.Name] = configuration;
        }

        #endregion

        #region Functions

        public IEnumerable<string> GetServerNames()
        {
            return m_configurations.Keys;
        }

        public bool HasServer(string name)
        {
            return m_servers.ContainsKey(name);
        }

        public void StartAll()
        {
            foreach (var name in m_configurations.Keys)
                GetServer(name).StartWaitingForConnection();
        }

        public void StopAll()
        {
            foreach (var server in m_servers.Values)
                server.StopWaitingForConnection();
        }

        #endregion

        #region IWitServerRestFactory

        public WitServerRest GetServer(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (m_disposed)
                throw new ObjectDisposedException(nameof(WitServerRestFactory));

            if (m_servers.TryGetValue(name, out var existing))
                return existing;

            lock (m_lock)
            {
                if (m_servers.TryGetValue(name, out existing))
                    return existing;

                if (!m_configurations.TryGetValue(name, out var configuration))
                    throw new InvalidOperationException($"No WitRPC REST server configuration found for name '{name}'. Make sure to register it using AddWitRpcRestServer.");

                var context = new WitServerRestBuilderContext(m_serviceProvider);
                configuration.Configure(context);

                var server = WitServerRestBuilder.Build(context);
                m_servers[name] = server;
                return server;
            }
        }

        #endregion

        #region IDisposable

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

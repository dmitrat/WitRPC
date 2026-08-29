using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Castle.DynamicProxy;
using OutWit.Communication.Client.DependencyInjection.Interfaces;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Interceptors;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Builds each named REST client once, from its registered configuration,
    /// and creates service proxies over it.
    /// </summary>
    public sealed class WitClientRestFactory : IWitClientRestFactory
    {
        #region Fields

        private readonly ConcurrentDictionary<string, WitClientRest> m_clients = new();

        private readonly ConcurrentDictionary<string, IConfigureWitClientRest> m_configurations = new();

        private readonly ProxyGenerator m_proxyGenerator = new();

        private readonly IServiceProvider m_serviceProvider;

        private readonly object m_lock = new();

        #endregion

        #region Constructors

        public WitClientRestFactory(IEnumerable<IConfigureWitClientRest> configurations, IServiceProvider serviceProvider)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            m_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            foreach (var configuration in configurations)
                m_configurations[configuration.Name] = configuration;
        }

        #endregion

        #region Functions

        public IEnumerable<string> GetClientNames()
        {
            return m_configurations.Keys;
        }

        #endregion

        #region IWitClientRestFactory

        public WitClientRest GetClient(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (m_clients.TryGetValue(name, out var existing))
                return existing;

            lock (m_lock)
            {
                if (m_clients.TryGetValue(name, out existing))
                    return existing;

                if (!m_configurations.TryGetValue(name, out var configuration))
                    throw new InvalidOperationException($"No WitRPC REST client configuration found for name '{name}'. Make sure to register it using AddWitRpcRestClient.");

                var context = new WitClientRestBuilderContext(m_serviceProvider);
                configuration.Configure(context);

                var client = WitClientRestBuilder.Build(context);
                m_clients[name] = client;
                return client;
            }
        }

        public TService GetService<TService>(string name, bool strongAssemblyMatch = true) where TService : class
        {
            var client = GetClient(name);

            return m_proxyGenerator.CreateInterfaceProxyWithoutTarget<TService>(
                new RequestInterceptorDynamic(client, strongAssemblyMatch, typeof(TService)));
        }

        #endregion
    }
}

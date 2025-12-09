using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OutWit.Communication.Server;

namespace OutWit.Communication.Server.DependencyInjection
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

    /// <summary>
    /// Default implementation of IWitServerFactory.
    /// </summary>
    public class WitServerFactory : IWitServerFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, WitServer> _servers = new();
        private readonly ConcurrentDictionary<string, IConfigureWitServer> _configurations = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly object _lock = new();

        public WitServerFactory(IEnumerable<IConfigureWitServer> configurations, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            foreach (var config in configurations)
            {
                _configurations[config.Name] = config;
            }
        }

        /// <inheritdoc />
        public WitServer GetServer(string name)
        {
            if (_servers.TryGetValue(name, out var existingServer))
                return existingServer;

            lock (_lock)
            {
                if (_servers.TryGetValue(name, out existingServer))
                    return existingServer;

                if (!_configurations.TryGetValue(name, out var configure))
                    throw new InvalidOperationException($"No WitRPC server configuration found for name '{name}'. Make sure to register it using AddWitRpcServer.");

                var options = new WitServerBuilderOptions();
                configure.Configure(options, _serviceProvider);
                var server = WitServerBuilder.Build(options);
                
                _servers[name] = server;
                return server;
            }
        }

        /// <summary>
        /// Gets all registered server names.
        /// </summary>
        public IEnumerable<string> GetServerNames() => _configurations.Keys;

        /// <summary>
        /// Starts all registered servers.
        /// </summary>
        public void StartAll()
        {
            foreach (var name in _configurations.Keys)
            {
                var server = GetServer(name);
                server.StartWaitingForConnection();
            }
        }

        /// <summary>
        /// Stops all servers.
        /// </summary>
        public void StopAll()
        {
            foreach (var server in _servers.Values)
            {
                server.StopWaitingForConnection();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var server in _servers.Values)
            {
                server.StopWaitingForConnection();
                server.Dispose();
            }
            _servers.Clear();
        }
    }

    /// <summary>
    /// Interface for WitServer configuration.
    /// </summary>
    public interface IConfigureWitServer
    {
        string Name { get; }
        void Configure(WitServerBuilderOptions options, IServiceProvider serviceProvider);
    }

    /// <summary>
    /// Implementation of WitServer configuration.
    /// </summary>
    internal class ConfigureWitServer : IConfigureWitServer
    {
        private readonly Action<WitServerBuilderOptions, IServiceProvider> _configure;

        public ConfigureWitServer(string name, Action<WitServerBuilderOptions, IServiceProvider> configure)
        {
            Name = name;
            _configure = configure;
        }

        public string Name { get; }

        public void Configure(WitServerBuilderOptions options, IServiceProvider serviceProvider)
        {
            _configure(options, serviceProvider);
        }
    }

    /// <summary>
    /// Implementation of WitServer configuration without service provider.
    /// </summary>
    internal class ConfigureWitServerSimple : IConfigureWitServer
    {
        private readonly Action<WitServerBuilderOptions> _configure;

        public ConfigureWitServerSimple(string name, Action<WitServerBuilderOptions> configure)
        {
            Name = name;
            _configure = configure;
        }

        public string Name { get; }

        public void Configure(WitServerBuilderOptions options, IServiceProvider serviceProvider)
        {
            _configure(options);
        }
    }
}

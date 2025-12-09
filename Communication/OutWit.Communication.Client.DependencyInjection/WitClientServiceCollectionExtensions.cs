using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering WitRPC client services.
    /// </summary>
    public static class WitClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the WitRPC client factory to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcClientFactory(this IServiceCollection services)
        {
            services.TryAddSingleton<WitClientFactory>();
            services.TryAddSingleton<IWitClientFactory>(sp => sp.GetRequiredService<WitClientFactory>());
            return services;
        }

        /// <summary>
        /// Adds a named WitRPC client configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configure">Action to configure the client options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcClient(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure)
        {
            services.AddWitRpcClientFactory();

            // Register configuration
            services.AddSingleton<IConfigureWitClient>(new ConfigureWitClient(name, configure));

            return services;
        }

        /// <summary>
        /// Adds a named WitRPC client with auto-connect on startup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configure">Action to configure the client options.</param>
        /// <param name="autoConnect">Whether to auto-connect on application startup.</param>
        /// <param name="connectionTimeout">Connection timeout.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcClient(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure,
            bool autoConnect,
            TimeSpan? connectionTimeout = null)
        {
            services.AddWitRpcClient(name, configure);

            if (autoConnect)
            {
                services.AddSingleton(new WitClientHostedServiceOptions
                {
                    ClientName = name,
                    AutoConnect = true,
                    ConnectionTimeout = connectionTimeout ?? TimeSpan.FromSeconds(30)
                });

                services.AddHostedService<WitClientHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Adds a typed WitRPC client service.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configure">Action to configure the client options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcClient<TService>(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure)
            where TService : class
        {
            services.AddWitRpcClient(name, configure);

            // Register the service interface
            services.AddSingleton<TService>(sp =>
            {
                var factory = sp.GetRequiredService<IWitClientFactory>();
                return factory.GetService<TService>(name);
            });

            return services;
        }

        /// <summary>
        /// Adds a typed WitRPC client service with auto-connect.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configure">Action to configure the client options.</param>
        /// <param name="autoConnect">Whether to auto-connect on application startup.</param>
        /// <param name="connectionTimeout">Connection timeout.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcClient<TService>(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure,
            bool autoConnect,
            TimeSpan? connectionTimeout = null)
            where TService : class
        {
            services.AddWitRpcClient(name, configure, autoConnect, connectionTimeout);

            // Register the service interface
            services.AddSingleton<TService>(sp =>
            {
                var factory = sp.GetRequiredService<IWitClientFactory>();
                return factory.GetService<TService>(name);
            });

            return services;
        }
    }

    /// <summary>
    /// Interface for WitClient configuration.
    /// </summary>
    public interface IConfigureWitClient
    {
        string Name { get; }
        void Configure(WitClientBuilderOptions options);
    }

    /// <summary>
    /// Implementation of WitClient configuration.
    /// </summary>
    internal class ConfigureWitClient : IConfigureWitClient
    {
        private readonly Action<WitClientBuilderOptions> _configure;

        public ConfigureWitClient(string name, Action<WitClientBuilderOptions> configure)
        {
            Name = name;
            _configure = configure;
        }

        public string Name { get; }

        public void Configure(WitClientBuilderOptions options)
        {
            _configure(options);
        }
    }
}

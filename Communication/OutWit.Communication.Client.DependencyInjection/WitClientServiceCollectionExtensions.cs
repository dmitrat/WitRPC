using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddWitRpcClientFactory(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcClient(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.AddWitRpcClientFactory();

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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
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

                services.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, WitClientHostedService>());
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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcClient<TService>(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure)
            where TService : class
        {
            services.AddWitRpcClient(name, configure);

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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcClient<TService>(
            this IServiceCollection services,
            string name,
            Action<WitClientBuilderOptions> configure,
            bool autoConnect,
            TimeSpan? connectionTimeout = null)
            where TService : class
        {
            services.AddWitRpcClient(name, configure, autoConnect, connectionTimeout);

            services.AddSingleton<TService>(sp =>
            {
                var factory = sp.GetRequiredService<IWitClientFactory>();
                return factory.GetService<TService>(name);
            });

            return services;
        }
    }
}

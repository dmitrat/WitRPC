using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering WitRPC server services.
    /// </summary>
    public static class WitServerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the WitRPC server factory to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServerFactory(this IServiceCollection services)
        {
            services.TryAddSingleton<WitServerFactory>();
            services.TryAddSingleton<IWitServerFactory>(sp => sp.GetRequiredService<WitServerFactory>());
            return services;
        }

        /// <summary>
        /// Adds a named WitRPC server configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServer(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions> configure)
        {
            services.AddWitRpcServerFactory();

            // Register configuration
            services.AddSingleton<IConfigureWitServer>(new ConfigureWitServerSimple(name, configure));

            return services;
        }

        /// <summary>
        /// Adds a named WitRPC server configuration with access to service provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options with access to service provider.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServer(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions, IServiceProvider> configure)
        {
            services.AddWitRpcServerFactory();

            // Register configuration
            services.AddSingleton<IConfigureWitServer>(new ConfigureWitServer(name, configure));

            return services;
        }

        /// <summary>
        /// Adds a named WitRPC server with auto-start on startup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServer(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions> configure,
            bool autoStart)
        {
            services.AddWitRpcServer(name, configure);

            if (autoStart)
            {
                services.AddSingleton(new WitServerHostedServiceOptions
                {
                    ServerName = name,
                    AutoStart = true
                });

                services.AddHostedService<WitServerHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Adds a named WitRPC server with service provider access and auto-start.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options with access to service provider.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServer(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions, IServiceProvider> configure,
            bool autoStart)
        {
            services.AddWitRpcServer(name, configure);

            if (autoStart)
            {
                services.AddSingleton(new WitServerHostedServiceOptions
                {
                    ServerName = name,
                    AutoStart = true
                });

                services.AddHostedService<WitServerHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Adds a typed WitRPC server with a service implementation.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServer<TService, TImplementation>(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions> configure)
            where TService : class
            where TImplementation : class, TService
        {
            // Register the service implementation
            services.TryAddSingleton<TImplementation>();
            services.TryAddSingleton<TService>(sp => sp.GetRequiredService<TImplementation>());

            // Register server with service from DI
            services.AddWitRpcServer(name, (options, sp) =>
            {
                configure(options);
                var service = sp.GetRequiredService<TService>();
                options.WithService(service);
            });

            return services;
        }

        /// <summary>
        /// Adds a typed WitRPC server with a service implementation and auto-start.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServer<TService, TImplementation>(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions> configure,
            bool autoStart)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddWitRpcServer<TService, TImplementation>(name, configure);

            if (autoStart)
            {
                services.AddSingleton(new WitServerHostedServiceOptions
                {
                    ServerName = name,
                    AutoStart = true
                });

                services.AddHostedService<WitServerHostedService>();
            }

            return services;
        }
    }
}

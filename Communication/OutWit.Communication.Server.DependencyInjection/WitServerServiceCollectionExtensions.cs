using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Communication.Processors;

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

        /// <summary>
        /// Adds a WitRPC server with composite services (multiple interfaces) with DI support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options.</param>
        /// <param name="configureServices">Action to configure composite services with DI support.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServerWithServices(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions> configure,
            Action<CompositeServiceRegistration> configureServices)
        {
            services.AddWitRpcServerFactory();

            // Collect service registrations
            var registration = new CompositeServiceRegistration(services);
            configureServices(registration);

            // Register server with composite services from DI
            services.AddSingleton<IConfigureWitServer>(sp =>
            {
                return new ConfigureWitServer(name, (options, serviceProvider) =>
                {
                    configure(options);

                    var processor = new CompositeRequestProcessor();
                    foreach (var serviceType in registration.ServiceTypes)
                    {
                        var service = serviceProvider.GetRequiredService(serviceType);
                        var registerMethod = typeof(CompositeRequestProcessor)
                            .GetMethod(nameof(CompositeRequestProcessor.Register))!
                            .MakeGenericMethod(serviceType);
                        registerMethod.Invoke(processor, new[] { service });
                    }

                    options.WithRequestProcessor(processor);
                });
            });

            return services;
        }

        /// <summary>
        /// Adds a WitRPC server with composite services and auto-start.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server options.</param>
        /// <param name="configureServices">Action to configure composite services with DI support.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWitRpcServerWithServices(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderOptions> configure,
            Action<CompositeServiceRegistration> configureServices,
            bool autoStart)
        {
            services.AddWitRpcServerWithServices(name, configure, configureServices);

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

    /// <summary>
    /// Helper class for registering composite services with DI.
    /// </summary>
    public class CompositeServiceRegistration
    {
        private readonly IServiceCollection _services;
        private readonly List<Type> _serviceTypes = new();

        internal CompositeServiceRegistration(IServiceCollection services)
        {
            _services = services;
        }

        internal IReadOnlyList<Type> ServiceTypes => _serviceTypes;

        /// <summary>
        /// Adds a service interface to the composite. The implementation must already be registered in DI.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <returns>This registration for chaining.</returns>
        public CompositeServiceRegistration AddService<TService>()
            where TService : class
        {
            _serviceTypes.Add(typeof(TService));
            return this;
        }

        /// <summary>
        /// Adds a service interface with its implementation to the composite.
        /// Registers the implementation in DI if not already registered.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <returns>This registration for chaining.</returns>
        public CompositeServiceRegistration AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.TryAddSingleton<TImplementation>();
            _services.TryAddSingleton<TService>(sp => sp.GetRequiredService<TImplementation>());
            _serviceTypes.Add(typeof(TService));
            return this;
        }

        /// <summary>
        /// Adds a service interface with a factory function.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="implementationFactory">Factory function to create the service.</param>
        /// <returns>This registration for chaining.</returns>
        public CompositeServiceRegistration AddService<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            _services.TryAddSingleton(implementationFactory);
            _serviceTypes.Add(typeof(TService));
            return this;
        }
    }
}

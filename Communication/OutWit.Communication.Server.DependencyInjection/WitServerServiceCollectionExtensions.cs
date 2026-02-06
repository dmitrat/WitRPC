using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Communication.Processors;
using OutWit.Communication.Server.DependencyInjection.Interfaces;

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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddWitRpcServerFactory(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<WitServerFactory>();
            services.TryAddSingleton<IWitServerFactory>(sp => sp.GetRequiredService<WitServerFactory>());
            return services;
        }

        /// <summary>
        /// Adds a named WitRPC server configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server using builder context.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcServer(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderContext> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.AddWitRpcServerFactory();

            services.AddSingleton<IConfigureWitServer>(new ConfigureWitServer(name, configure));

            return services;
        }

        /// <summary>
        /// Adds a named WitRPC server with auto-start on startup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server using builder context.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcServer(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderContext> configure,
            bool autoStart)
        {
            services.AddWitRpcServer(name, configure);

            if (autoStart)
            {
                RegisterHostedService(services, name);
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
        /// <param name="configure">Action to configure the server using builder context.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcServer<TService, TImplementation>(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderContext> configure)
            where TService : class
            where TImplementation : class, TService
        {
            services.TryAddSingleton<TImplementation>();
            services.TryAddSingleton<TService>(sp => sp.GetRequiredService<TImplementation>());

            services.AddWitRpcServer(name, ctx =>
            {
                configure(ctx);
                var service = ctx.ServiceProvider.GetRequiredService<TService>();
                ctx.WithService(service);
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
        /// <param name="configure">Action to configure the server using builder context.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
        public static IServiceCollection AddWitRpcServer<TService, TImplementation>(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderContext> configure,
            bool autoStart)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddWitRpcServer<TService, TImplementation>(name, configure);

            if (autoStart)
            {
                RegisterHostedService(services, name);
            }

            return services;
        }

        /// <summary>
        /// Adds a WitRPC server with composite services (multiple interfaces) with DI support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server using builder context.</param>
        /// <param name="configureServices">Action to configure composite services with DI support.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public static IServiceCollection AddWitRpcServerWithServices(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderContext> configure,
            Action<CompositeServiceRegistration> configureServices)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            if (configureServices == null)
                throw new ArgumentNullException(nameof(configureServices));

            services.AddWitRpcServerFactory();

            var registration = new CompositeServiceRegistration(services);
            configureServices(registration);

            services.AddSingleton<IConfigureWitServer>(sp =>
            {
                return new ConfigureWitServer(name, ctx =>
                {
                    configure(ctx);

                    var processor = new CompositeRequestProcessor();
                    foreach (var serviceType in registration.ServiceTypes)
                    {
                        var service = ctx.ServiceProvider.GetRequiredService(serviceType);
                        var registerMethod = typeof(CompositeRequestProcessor)
                            .GetMethod(nameof(CompositeRequestProcessor.Register))!
                            .MakeGenericMethod(serviceType);
                        registerMethod.Invoke(processor, new[] { service });
                    }

                    ctx.WithRequestProcessor(processor);
                });
            });

            return services;
        }

        /// <summary>
        /// Adds a WitRPC server with composite services and auto-start.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">Action to configure the server using builder context.</param>
        /// <param name="configureServices">Action to configure composite services with DI support.</param>
        /// <param name="autoStart">Whether to auto-start on application startup.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public static IServiceCollection AddWitRpcServerWithServices(
            this IServiceCollection services,
            string name,
            Action<WitServerBuilderContext> configure,
            Action<CompositeServiceRegistration> configureServices,
            bool autoStart)
        {
            services.AddWitRpcServerWithServices(name, configure, configureServices);

            if (autoStart)
            {
                RegisterHostedService(services, name);
            }

            return services;
        }

        #region Private Methods

        private static void RegisterHostedService(IServiceCollection services, string name)
        {
            services.AddSingleton(new WitServerHostedServiceOptions
            {
                ServerName = name,
                AutoStart = true
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, WitServerHostedService>());
        }

        #endregion
    }
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OutWit.Communication.Server.DependencyInjection.Interfaces;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Registers WitRPC REST servers in the container the same way
    /// <c>AddWitRpcServer</c> registers the persistent ones: by name, built on
    /// first use by <see cref="IWitServerRestFactory"/>, optionally started
    /// with the host.
    /// </summary>
    public static class WitServerRestServiceCollectionExtensions
    {
        #region Factory

        public static IServiceCollection AddWitRpcRestServerFactory(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<WitServerRestFactory>();
            services.TryAddSingleton<IWitServerRestFactory>(sp => sp.GetRequiredService<WitServerRestFactory>());

            return services;
        }

        #endregion

        #region Registration

        /// <summary>
        /// Registers a named REST server; the service it exposes is set inside
        /// <paramref name="configure"/> (<c>ctx.WithService&lt;T&gt;()</c> resolves it from DI).
        /// </summary>
        public static IServiceCollection AddWitRpcRestServer(this IServiceCollection services, string name,
            Action<WitServerRestBuilderContext> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.AddWitRpcRestServerFactory();
            services.AddSingleton<IConfigureWitServerRest>(new ConfigureWitServerRest(name, configure));

            return services;
        }

        /// <summary>
        /// Registers a named REST server and, when <paramref name="autoStart"/> is set,
        /// starts it with the host.
        /// </summary>
        public static IServiceCollection AddWitRpcRestServer(this IServiceCollection services, string name,
            Action<WitServerRestBuilderContext> configure, bool autoStart)
        {
            services.AddWitRpcRestServer(name, configure);

            if (autoStart)
                RegisterHostedService(services, name);

            return services;
        }

        /// <summary>
        /// Registers the service implementation in the container and a named REST
        /// server exposing it.
        /// </summary>
        public static IServiceCollection AddWitRpcRestServer<TService, TImplementation>(this IServiceCollection services, string name,
            Action<WitServerRestBuilderContext> configure)
            where TService : class
            where TImplementation : class, TService
        {
            services.TryAddSingleton<TImplementation>();
            services.TryAddSingleton<TService>(sp => sp.GetRequiredService<TImplementation>());

            services.AddWitRpcRestServer(name, context =>
            {
                configure(context);
                context.WithService<TService>();
            });

            return services;
        }

        /// <summary>
        /// Registers the service implementation and a named REST server exposing it,
        /// started with the host when <paramref name="autoStart"/> is set.
        /// </summary>
        public static IServiceCollection AddWitRpcRestServer<TService, TImplementation>(this IServiceCollection services, string name,
            Action<WitServerRestBuilderContext> configure, bool autoStart)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddWitRpcRestServer<TService, TImplementation>(name, configure);

            if (autoStart)
                RegisterHostedService(services, name);

            return services;
        }

        #endregion

        #region Tools

        private static void RegisterHostedService(IServiceCollection services, string name)
        {
            services.AddSingleton(new WitServerRestHostedServiceOptions
            {
                ServerName = name,
                AutoStart = true
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, WitServerRestHostedService>());
        }

        #endregion
    }
}

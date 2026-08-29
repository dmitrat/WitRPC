using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Registers WitRPC REST clients in the container the same way
    /// <c>AddWitRpcClient</c> registers the persistent ones: by name, built on
    /// first use by <see cref="IWitClientRestFactory"/>. A REST client is
    /// stateless, so there is nothing to connect and no hosted service.
    /// </summary>
    public static class WitClientRestServiceCollectionExtensions
    {
        #region Factory

        public static IServiceCollection AddWitRpcRestClientFactory(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<WitClientRestFactory>();
            services.TryAddSingleton<IWitClientRestFactory>(sp => sp.GetRequiredService<WitClientRestFactory>());

            return services;
        }

        #endregion

        #region Registration

        /// <summary>
        /// Registers a named REST client.
        /// </summary>
        public static IServiceCollection AddWitRpcRestClient(this IServiceCollection services, string name,
            Action<WitClientRestBuilderContext> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.AddWitRpcRestClientFactory();
            services.AddSingleton<IConfigureWitClientRest>(new ConfigureWitClientRest(name, configure));

            return services;
        }

        /// <summary>
        /// Registers a named REST client and <typeparamref name="TService"/> as a
        /// singleton proxy over it, so the service interface can be injected directly.
        /// </summary>
        public static IServiceCollection AddWitRpcRestClient<TService>(this IServiceCollection services, string name,
            Action<WitClientRestBuilderContext> configure, bool strongAssemblyMatch = true)
            where TService : class
        {
            services.AddWitRpcRestClient(name, configure);
            services.AddSingleton<TService>(sp => sp.GetRequiredService<IWitClientRestFactory>().GetService<TService>(name, strongAssemblyMatch));

            return services;
        }

        #endregion
    }
}

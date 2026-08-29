using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Communication.Processors;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Helper class for registering composite services with DI.
    /// </summary>
    public sealed class CompositeServiceRegistration
    {
        #region Fields

        private readonly IServiceCollection m_services;
        private readonly List<Type> m_serviceTypes = new();

        #endregion

        #region Constructors

        internal CompositeServiceRegistration(IServiceCollection services)
        {
            m_services = services;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds a service interface to the composite. The implementation must already be registered in DI.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <returns>This registration for chaining.</returns>
        public CompositeServiceRegistration AddService<TService>()
            where TService : class
        {
            m_serviceTypes.Add(typeof(TService));
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
            m_services.TryAddSingleton<TImplementation>();
            m_services.TryAddSingleton<TService>(sp => sp.GetRequiredService<TImplementation>());
            m_serviceTypes.Add(typeof(TService));
            return this;
        }

        /// <summary>
        /// Adds a service interface with a factory function.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="implementationFactory">Factory function to create the service.</param>
        /// <returns>This registration for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="implementationFactory"/> is null.</exception>
        public CompositeServiceRegistration AddService<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            m_services.TryAddSingleton(implementationFactory);
            m_serviceTypes.Add(typeof(TService));
            return this;
        }

        #endregion

        #region Tools

        /// <summary>
        /// Resolves every registered service from <paramref name="serviceProvider"/>
        /// into one composite processor.
        /// </summary>
        internal CompositeRequestProcessor CreateProcessor(IServiceProvider serviceProvider)
        {
            var processor = new CompositeRequestProcessor();
            var register = typeof(CompositeRequestProcessor)
                .GetMethods()
                .Single(m => m.Name == nameof(CompositeRequestProcessor.Register)
                             && m.IsGenericMethodDefinition
                             && m.GetGenericArguments().Length == 1);

            foreach (var serviceType in m_serviceTypes)
                register.MakeGenericMethod(serviceType).Invoke(processor, new[] { serviceProvider.GetRequiredService(serviceType) });

            return processor;
        }

        #endregion

        #region Properties

        internal IReadOnlyList<Type> ServiceTypes => m_serviceTypes;

        #endregion
    }
}

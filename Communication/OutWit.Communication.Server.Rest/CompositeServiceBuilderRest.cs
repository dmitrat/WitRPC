using System;
using System.Collections.Generic;
using OutWit.Communication.Processors;

namespace OutWit.Communication.Server.Rest
{
    /// <summary>
    /// Composes several service contracts into one REST host, the way
    /// <c>WitServerBuilder.WithServices()</c> does for a persistent server.
    /// Each contract is recorded for the readable-binding catalog as it is added.
    /// </summary>
    public sealed class CompositeServiceBuilderRest
    {
        #region Fields

        private readonly WitServerRestBuilderOptions m_options;
        private readonly CompositeRequestProcessor m_processor;
        private readonly List<Type> m_contracts = new();

        #endregion

        #region Constructors

        internal CompositeServiceBuilderRest(WitServerRestBuilderOptions options, bool isStrongAssemblyMatch)
        {
            m_options = options;
            m_processor = new CompositeRequestProcessor(isStrongAssemblyMatch);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds a service under its contract <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The service interface.</typeparam>
        /// <param name="service">The implementation.</param>
        /// <returns>This builder for chaining.</returns>
        public CompositeServiceBuilderRest AddService<TService>(TService service)
            where TService : class
        {
            m_processor.Register(service);
            m_contracts.Add(typeof(TService));
            return this;
        }

        /// <summary>
        /// Adds a service created by <paramref name="serviceFactory"/>.
        /// </summary>
        /// <typeparam name="TService">The service interface.</typeparam>
        /// <param name="serviceFactory">Creates the implementation.</param>
        /// <returns>This builder for chaining.</returns>
        public CompositeServiceBuilderRest AddService<TService>(Func<TService> serviceFactory)
            where TService : class
        {
            return AddService(serviceFactory());
        }

        /// <summary>
        /// Adds a service with the contract named explicitly.
        /// </summary>
        /// <typeparam name="TInterface">The service interface.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="service">The implementation.</param>
        /// <returns>This builder for chaining.</returns>
        public CompositeServiceBuilderRest AddService<TInterface, TImplementation>(TImplementation service)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            m_processor.Register<TInterface, TImplementation>(service);
            m_contracts.Add(typeof(TInterface));
            return this;
        }

        /// <summary>
        /// Installs the composite processor and its contracts and returns to the options.
        /// </summary>
        /// <returns>The REST server options for continued configuration.</returns>
        public WitServerRestBuilderOptions Build()
        {
            return m_options.WithRequestProcessor(m_processor, m_contracts.ToArray());
        }

        #endregion
    }
}

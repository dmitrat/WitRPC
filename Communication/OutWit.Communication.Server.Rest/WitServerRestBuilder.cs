using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Processors;
using OutWit.Communication.Server.Authorization;

namespace OutWit.Communication.Server.Rest
{
    public static class WitServerRestBuilder
    {
        public static WitServerRest Build(Action<WitServerRestBuilderOptions> optionsBuilder)
        {
            var options = new WitServerRestBuilderOptions();
            optionsBuilder(options);

            return Build(options);
        }

        /// <summary>
        /// Builds a REST server from already-configured options (the DI factory
        /// path, where the options object is the builder context).
        /// </summary>
        public static WitServerRest Build(WitServerRestBuilderOptions options)
        {
            if (options.TransportOptions == null)
                throw new WitException("Transport options cannot be empty");

            if (options.RequestProcessor == null)
                throw new WitException("Request processor cannot be empty");

            if (options.Contracts.Count == 0)
                throw new WitException("REST needs the service contract type(s) to bind readable requests; use WithService<TService> or WithRequestProcessor(processor, contracts)");

            return new WitServerRest(options.TransportOptions, options.TokenValidator, options.RequestProcessor,
                new RestMethodCatalog(options.Contracts), options.Logger, options.Timeout,
                options.MaxBodyBytes, options.MaxConcurrentRequests);
        }

        #region Transport


        public static WitServerRestBuilderOptions WithOptions(this WitServerRestBuilderOptions me, RestServerTransportOptions options)
        {
            me.TransportOptions = options;
            return me;
        }

        public static WitServerRestBuilderOptions WithUrl(this WitServerRestBuilderOptions me, string url)
        {
            me.TransportOptions = new RestServerTransportOptions{Host = (HostInfo)url };
            return me;
        }


        public static WitServerRestBuilderOptions WithHost(this WitServerRestBuilderOptions me, HostInfo hostInfo)
        {
            me.TransportOptions = new RestServerTransportOptions { Host = hostInfo };
            return me;
        }

        #endregion

        #region Processor

        /// <summary>
        /// Uses a custom processor. The contract types are what the REST layer
        /// binds readable calls against, so they must be listed explicitly.
        /// </summary>
        public static WitServerRestBuilderOptions WithRequestProcessor(this WitServerRestBuilderOptions me, IRequestProcessor requestProcessor, params Type[] contracts)
        {
            me.RequestProcessor = requestProcessor;
            me.Contracts.AddRange(contracts);
            return me;
        }

        public static WitServerRestBuilderOptions WithService<TService>(this WitServerRestBuilderOptions me, TService service, bool isStrongAssemblyMatch = true)
            where TService: class
        {
            me.RequestProcessor = new RequestProcessor<TService>(service, isStrongAssemblyMatch);
            me.Contracts.Add(typeof(TService));
            return me;
        }

        public static WitServerRestBuilderOptions WithService<TService>(this WitServerRestBuilderOptions me, bool isStrongAssemblyMatch = true)
            where TService : class, new ()
        {
            me.RequestProcessor = new RequestProcessor<TService>(new TService(), isStrongAssemblyMatch);
            me.Contracts.Add(typeof(TService));
            return me;
        }

        public static WitServerRestBuilderOptions WithService<TService>(this WitServerRestBuilderOptions me, Func<TService> serviceBuilder, bool isStrongAssemblyMatch = true)
            where TService : class, new()
        {
            me.RequestProcessor = new RequestProcessor<TService>(serviceBuilder(), isStrongAssemblyMatch);
            me.Contracts.Add(typeof(TService));
            return me;
        }

        #endregion

        #region Composite Services

        /// <summary>
        /// Starts composing several service contracts into this one REST host;
        /// finish with <c>.Build()</c>.
        /// </summary>
        /// <param name="me">The REST server options.</param>
        /// <param name="isStrongAssemblyMatch">Whether to use strong assembly matching for method resolution.</param>
        /// <returns>A composite service builder for chaining.</returns>
        public static CompositeServiceBuilderRest WithServices(this WitServerRestBuilderOptions me, bool isStrongAssemblyMatch = true)
        {
            return new CompositeServiceBuilderRest(me, isStrongAssemblyMatch);
        }

        #endregion

        #region Authorization

        public static WitServerRestBuilderOptions WithAccessTokenValidator(this WitServerRestBuilderOptions me, IAccessTokenValidator validator)
        {
            me.TokenValidator = validator;
            return me;
        }

        public static WitServerRestBuilderOptions WithAccessToken(this WitServerRestBuilderOptions me, string accessToken)
        {
            me.TokenValidator = new AccessTokenValidatorStatic(accessToken);
            return me;
        }

        public static WitServerRestBuilderOptions WithoutAuthorization(this WitServerRestBuilderOptions me)
        {
            me.TokenValidator = new AccessTokenValidatorPlain();
            return me;
        }

        #endregion

        #region Logger

        public static WitServerRestBuilderOptions WithLogger(this WitServerRestBuilderOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        #endregion

        #region Timeout

        public static WitServerRestBuilderOptions WithTimeout(this WitServerRestBuilderOptions me, TimeSpan timeout)
        {
            me.Timeout = timeout;
            return me;
        }

        #endregion

        #region Limits

        /// <summary>The largest request body accepted, in bytes; a larger one is refused with 413.</summary>
        public static WitServerRestBuilderOptions WithMaxBodyBytes(this WitServerRestBuilderOptions me, long maxBodyBytes)
        {
            me.MaxBodyBytes = maxBodyBytes;
            return me;
        }

        /// <summary>The most requests processed at once; the rest wait.</summary>
        public static WitServerRestBuilderOptions WithMaxConcurrentRequests(this WitServerRestBuilderOptions me, int maxConcurrentRequests)
        {
            me.MaxConcurrentRequests = maxConcurrentRequests;
            return me;
        }

        #endregion
    }
}

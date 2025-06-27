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

            if (options.TransportOptions == null)
                throw new WitException("Transport options cannot be empty");

            if (options.RequestProcessor == null)
                throw new WitException("Request processor cannot be empty");

            return new WitServerRest(options.TransportOptions, options.TokenValidator, options.RequestProcessor, options.Logger, options.Timeout);
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

        public static WitServerRestBuilderOptions WithRequestProcessor(this WitServerRestBuilderOptions me, IRequestProcessor requestProcessor)
        {
            me.RequestProcessor = requestProcessor;
            return me;
        }

        public static WitServerRestBuilderOptions WithService<TService>(this WitServerRestBuilderOptions me, TService service, bool isStrongAssemblyMatch = true)
            where TService: class
        {
            me.RequestProcessor = new RequestProcessor<TService>(service, isStrongAssemblyMatch);
            return me;
        }

        public static WitServerRestBuilderOptions WithService<TService>(this WitServerRestBuilderOptions me, bool isStrongAssemblyMatch = true)
            where TService : class, new ()
        {
            me.RequestProcessor = new RequestProcessor<TService>(new TService(), isStrongAssemblyMatch);
            return me;
        }

        public static WitServerRestBuilderOptions WithService<TService>(this WitServerRestBuilderOptions me, Func<TService> serviceBuilder, bool isStrongAssemblyMatch = true)
            where TService : class, new()
        {
            me.RequestProcessor = new RequestProcessor<TService>(serviceBuilder(), isStrongAssemblyMatch);
            return me;
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
    }
}

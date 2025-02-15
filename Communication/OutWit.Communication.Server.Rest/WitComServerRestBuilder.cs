using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Converters;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Processors;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Server.Rest
{
    public static class WitComServerRestBuilder
    {
        public static WitComServerRest Build(Action<WitComServerRestBuilderOptions> optionsBuilder)
        {
            var options = new WitComServerRestBuilderOptions();
            optionsBuilder(options);

            if (options.TransportOptions == null)
                throw new WitComException("Transport options cannot be empty");

            if (options.RequestProcessor == null)
                throw new WitComException("Request processor cannot be empty");

            return new WitComServerRest(options.TransportOptions, options.TokenValidator, options.RequestProcessor, options.Logger, options.Timeout);
        }

        #region Transport


        public static WitComServerRestBuilderOptions WithOptions(this WitComServerRestBuilderOptions me, RestServerTransportOptions options)
        {
            me.TransportOptions = options;
            return me;
        }

        public static WitComServerRestBuilderOptions WithUrl(this WitComServerRestBuilderOptions me, string url)
        {
            me.TransportOptions = new RestServerTransportOptions{Host = (HostInfo)url };
            return me;
        }


        public static WitComServerRestBuilderOptions WithHost(this WitComServerRestBuilderOptions me, HostInfo hostInfo)
        {
            me.TransportOptions = new RestServerTransportOptions { Host = hostInfo };
            return me;
        }

        #endregion

        #region Processor

        public static WitComServerRestBuilderOptions WithRequestProcessor(this WitComServerRestBuilderOptions me, IRequestProcessor requestProcessor)
        {
            me.RequestProcessor = requestProcessor;
            return me;
        }

        public static WitComServerRestBuilderOptions WithService<TService>(this WitComServerRestBuilderOptions me, TService service, bool isStrongAssemblyMatch = true)
            where TService: class
        {
            me.RequestProcessor = new RequestProcessor<TService>(service, isStrongAssemblyMatch);
            return me;
        }

        public static WitComServerRestBuilderOptions WithService<TService>(this WitComServerRestBuilderOptions me, bool isStrongAssemblyMatch = true)
            where TService : class, new ()
        {
            me.RequestProcessor = new RequestProcessor<TService>(new TService(), isStrongAssemblyMatch);
            return me;
        }

        public static WitComServerRestBuilderOptions WithService<TService>(this WitComServerRestBuilderOptions me, Func<TService> serviceBuilder, bool isStrongAssemblyMatch = true)
            where TService : class, new()
        {
            me.RequestProcessor = new RequestProcessor<TService>(serviceBuilder(), isStrongAssemblyMatch);
            return me;
        }

        #endregion

        #region Authorization

        public static WitComServerRestBuilderOptions WithAccessTokenValidator(this WitComServerRestBuilderOptions me, IAccessTokenValidator validator)
        {
            me.TokenValidator = validator;
            return me;
        }

        public static WitComServerRestBuilderOptions WithAccessToken(this WitComServerRestBuilderOptions me, string accessToken)
        {
            me.TokenValidator = new AccessTokenValidatorStatic(accessToken);
            return me;
        }

        public static WitComServerRestBuilderOptions WithoutAuthorization(this WitComServerRestBuilderOptions me)
        {
            me.TokenValidator = new AccessTokenValidatorPlain();
            return me;
        }

        #endregion

        #region Logger

        public static WitComServerRestBuilderOptions WithLogger(this WitComServerRestBuilderOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        #endregion

        #region Timeout

        public static WitComServerRestBuilderOptions WithTimeout(this WitComServerRestBuilderOptions me, TimeSpan timeout)
        {
            me.Timeout = timeout;
            return me;
        }

        #endregion
    }
}

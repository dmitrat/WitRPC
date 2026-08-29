using System;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.Rest
{
    /// <summary>
    /// Builds a <see cref="WitClientRest"/> the same way <c>WitClientBuilder</c>
    /// builds a persistent client: a fluent set of <c>With*</c> options.
    /// </summary>
    public static class WitClientRestBuilder
    {
        public static WitClientRest Build(Action<WitClientRestBuilderOptions> optionsBuilder)
        {
            var options = new WitClientRestBuilderOptions();
            optionsBuilder(options);

            return Build(options);
        }

        /// <summary>
        /// Builds a REST client from already-configured options (the DI factory
        /// path, where the options object is the builder context).
        /// </summary>
        public static WitClientRest Build(WitClientRestBuilderOptions options)
        {
            if (string.IsNullOrEmpty(options.Host?.Connection))
                throw new WitException("Url cannot be empty");

            return new WitClientRest(new RestClientTransportOptions
            {
                Host = options.Host,
                Mode = options.Mode,
                Timeout = options.Timeout
            }, options.TokenProvider);
        }

        #region Transport

        public static WitClientRestBuilderOptions WithOptions(this WitClientRestBuilderOptions me, RestClientTransportOptions options)
        {
            me.Host = options.Host;
            me.Mode = options.Mode;
            me.Timeout = options.Timeout;
            return me;
        }

        public static WitClientRestBuilderOptions WithUrl(this WitClientRestBuilderOptions me, string url)
        {
            me.Host = (HostInfo)url;
            return me;
        }

        public static WitClientRestBuilderOptions WithHost(this WitClientRestBuilderOptions me, HostInfo hostInfo)
        {
            me.Host = hostInfo;
            return me;
        }

        /// <summary>
        /// When a call may go out as a GET with the arguments in the query string
        /// instead of a POST; <see cref="RestClientRequestModes.PostOnly"/> by default.
        /// </summary>
        public static WitClientRestBuilderOptions WithMode(this WitClientRestBuilderOptions me, RestClientRequestModes mode)
        {
            me.Mode = mode;
            return me;
        }

        #endregion

        #region Authorization

        public static WitClientRestBuilderOptions WithAccessTokenProvider(this WitClientRestBuilderOptions me, IAccessTokenProvider provider)
        {
            me.TokenProvider = provider;
            return me;
        }

        public static WitClientRestBuilderOptions WithAccessToken(this WitClientRestBuilderOptions me, string accessToken)
        {
            me.TokenProvider = new AccessTokenProviderStatic(accessToken);
            return me;
        }

        public static WitClientRestBuilderOptions WithoutAuthorization(this WitClientRestBuilderOptions me)
        {
            me.TokenProvider = new AccessTokenProviderPlain();
            return me;
        }

        #endregion

        #region Timeout

        /// <summary>How long one call may wait for its HTTP response.</summary>
        public static WitClientRestBuilderOptions WithTimeout(this WitClientRestBuilderOptions me, TimeSpan timeout)
        {
            me.Timeout = timeout;
            return me;
        }

        #endregion
    }
}

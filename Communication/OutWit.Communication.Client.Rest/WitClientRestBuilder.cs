using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Proxy.Interfaces;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interceptors;
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
            }, options.TokenProvider, options.Logger, options.HttpClient);
        }

        /// <summary>
        /// Creates a source-generated proxy over the client (the NativeAOT-friendly
        /// path): <c>client.GetService&lt;IExampleService&gt;(i =&gt; new ExampleServiceProxy(i))</c>.
        /// The runtime-generated form, <c>client.GetService&lt;IExampleService&gt;()</c>,
        /// comes with the opt-in OutWit.Communication.Client.DynamicProxy package.
        /// </summary>
        public static TService GetService<TService>(this WitClientRest me, Func<IProxyInterceptor, TService> create, bool strongAssemblyMatch = true)
            where TService : class
        {
            return create(new RequestInterceptor(me, strongAssemblyMatch, typeof(TService)));
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

        /// <summary>
        /// A token fetched on every call -- for tokens that expire and refresh.
        /// </summary>
        public static WitClientRestBuilderOptions WithAccessToken(this WitClientRestBuilderOptions me, Func<Task<string>> getTokenAsync)
        {
            me.TokenProvider = new AccessTokenProviderCallback(getTokenAsync);
            return me;
        }

        /// <summary>
        /// A token fetched on every call -- for tokens that expire and refresh.
        /// </summary>
        public static WitClientRestBuilderOptions WithAccessToken(this WitClientRestBuilderOptions me, Func<string> getToken)
        {
            me.TokenProvider = new AccessTokenProviderCallback(getToken);
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

        #region Logger

        public static WitClientRestBuilderOptions WithLogger(this WitClientRestBuilderOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        #endregion

        #region Http

        /// <summary>
        /// Sends every call through this <see cref="HttpClient"/> -- the place for a
        /// proxy, client certificates, default headers or a test handler. Its own
        /// timeout is honoured and reported as a timeout, like <see cref="WithTimeout"/>.
        /// </summary>
        public static WitClientRestBuilderOptions WithHttpClient(this WitClientRestBuilderOptions me, HttpClient httpClient)
        {
            me.HttpClient = httpClient;
            return me;
        }

        /// <summary>
        /// Sends every call through a client built on this handler chain. The
        /// client's own timeout is disabled; <see cref="WithTimeout"/> bounds each call.
        /// </summary>
        public static WitClientRestBuilderOptions WithHttpMessageHandler(this WitClientRestBuilderOptions me, HttpMessageHandler handler)
        {
            me.HttpClient = new HttpClient(handler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            return me;
        }

        #endregion
    }
}

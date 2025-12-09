using System;
using System.Net;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Common.Proxy.Interfaces;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Discovery;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Resilience;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Client
{
    public static class WitClientBuilder
    {
        public static WitClient Build(WitClientBuilderOptions options)
        {
            if (options.Transport == null)
                throw new WitException("Transport cannot be empty");

            return new WitClient(options.Transport, options.Encryptor, options.TokenProvider, 
                options.ParametersSerializer, options.MessageSerializer,
                options.ReconnectionOptions, options.RetryOptions, options.Logger, options.Timeout);
        }

        public static WitClient Build(Action<WitClientBuilderOptions> optionsBuilder)
        {
            var options = new WitClientBuilderOptions();
            optionsBuilder(options);

            return Build(options);
        }

        public static TService GetService<TService>(this WitClient me, bool strongAssemblyMatch = true)
            where TService : class
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(me, strongAssemblyMatch);

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<TService>(interceptor);
        }

        public static TService GetService<TService>(this WitClient me, Func<IProxyInterceptor, TService> create, bool strongAssemblyMatch = true)
            where TService : class
        {
            return create(new RequestInterceptor(me, strongAssemblyMatch));
        }

        #region Authorization

        public static WitClientBuilderOptions WithAccessTokenProvider(this WitClientBuilderOptions me, IAccessTokenProvider provider)
        {
            me.TokenProvider = provider;
            return me;
        }

        public static WitClientBuilderOptions WithAccessToken(this WitClientBuilderOptions me, string accessToken)
        {
            me.TokenProvider = new AccessTokenProviderStatic(accessToken);
            return me;
        }

        /// <summary>
        /// Configures dynamic token retrieval using a callback function.
        /// Useful for scenarios where the token needs to be refreshed (e.g., OAuth tokens).
        /// The callback is invoked on every authorization request, including reconnections.
        /// </summary>
        /// <param name="me">The builder options.</param>
        /// <param name="getTokenAsync">Async function that returns the current valid token.</param>
        public static WitClientBuilderOptions WithAccessToken(this WitClientBuilderOptions me, Func<Task<string>> getTokenAsync)
        {
            me.TokenProvider = new AccessTokenProviderCallback(getTokenAsync);
            return me;
        }

        /// <summary>
        /// Configures dynamic token retrieval using a callback function.
        /// Useful for scenarios where the token needs to be refreshed (e.g., OAuth tokens).
        /// The callback is invoked on every authorization request, including reconnections.
        /// </summary>
        /// <param name="me">The builder options.</param>
        /// <param name="getToken">Function that returns the current valid token.</param>
        public static WitClientBuilderOptions WithAccessToken(this WitClientBuilderOptions me, Func<string> getToken)
        {
            me.TokenProvider = new AccessTokenProviderCallback(getToken);
            return me;
        }

        public static WitClientBuilderOptions WithoutAuthorization(this WitClientBuilderOptions me)
        {
            me.TokenProvider = new AccessTokenProviderPlain();
            return me;
        }

        #endregion

        #region Encryption

        public static WitClientBuilderOptions WithEncryptor(this WitClientBuilderOptions me, IEncryptorClient encryptor)
        {
            me.Encryptor = encryptor;
            return me;
        }

        public static WitClientBuilderOptions WithEncryption(this WitClientBuilderOptions me)
        {
            me.Encryptor = new EncryptorClientGeneral();
            return me;
        }

        public static WitClientBuilderOptions WithoutEncryption(this WitClientBuilderOptions me)
        {
            me.Encryptor = new EncryptorClientPlain();
            return me;
        }

        #endregion

        #region Serialization

        public static WitClientBuilderOptions WithMessageSerializer(this WitClientBuilderOptions me, IMessageSerializer serializer)
        {
            me.MessageSerializer = serializer;
            return me;
        }


        public static WitClientBuilderOptions WithParametersSerializer(this WitClientBuilderOptions me, IMessageSerializer serializer)
        {
            me.ParametersSerializer = serializer;
            return me;
        }

        public static DiscoveryClientOptions WithSerializer(this DiscoveryClientOptions me, IMessageSerializer serializer)
        {
            me.Serializer = serializer;
            return me;
        }

        #endregion

        #region Json

        public static WitClientBuilderOptions WithJson(this WitClientBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerJson();
            return me;
        }

        public static WitClientBuilderOptions WithJson(this WitClientBuilderOptions me, Action<JsonOptions> options)
        {
            JsonUtils.Register(options);

            return me.WithJson();
        }

        public static DiscoveryClientOptions WithJson(this DiscoveryClientOptions me)
        {
            me.Serializer = new MessageSerializerJson();
            return me;
        }

        public static DiscoveryClientOptions WithJson(this DiscoveryClientOptions me, Action<JsonOptions> options)
        {
            JsonUtils.Register(options);

            return me.WithJson();
        }

        #endregion

        #region MessagePack

        public static WitClientBuilderOptions WithMessagePack(this WitClientBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerMessagePack();
            return me;
        }


        public static WitClientBuilderOptions WithMessagePack(this WitClientBuilderOptions me, Action<MessagePackOptions> options)
        {
            MessagePackUtils.Register(options);

            return me.WithMessagePack();
        }

        public static DiscoveryClientOptions WithMessagePack(this DiscoveryClientOptions me)
        {
            me.Serializer = new MessageSerializerMessagePack();
            return me;
        }

        public static DiscoveryClientOptions WithMessagePack(this DiscoveryClientOptions me, Action<MessagePackOptions> options)
        {
            MessagePackUtils.Register(options);

            return me.WithMessagePack();
        }

        #endregion

        #region MemoryPack

        public static WitClientBuilderOptions WithMemoryPack(this WitClientBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerMemoryPack();
            return me;
        }

        public static WitClientBuilderOptions WithMemoryPack(this WitClientBuilderOptions me, Action<MemoryPackOptions> options)
        {
            MemoryPackUtils.Register(options);
            return me.WithMemoryPack();
        }

        public static DiscoveryClientOptions WithMemoryPack(this DiscoveryClientOptions me)
        {
            me.Serializer = new MessageSerializerMemoryPack();
            return me;
        }

        public static DiscoveryClientOptions WithMemoryPack(this DiscoveryClientOptions me, Action<MemoryPackOptions> options)
        {
            MemoryPackUtils.Register(options);
            return me.WithMemoryPack();
        }

        #endregion

        #region ProtoBuf

        public static WitClientBuilderOptions WithProtoBuf(this WitClientBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerProtoBuf();
            return me;
        }

        public static WitClientBuilderOptions WithProtoBuf(this WitClientBuilderOptions me, Action<ProtoBufOptions> options)
        {
            ProtoBufUtils.Register(options);

            return me.WithProtoBuf();
        }

        public static DiscoveryClientOptions WithProtoBuf(this DiscoveryClientOptions me)
        {
            me.Serializer = new MessageSerializerProtoBuf();
            return me;
        }

        public static DiscoveryClientOptions WithProtoBuf(this DiscoveryClientOptions me, Action<ProtoBufOptions> options)
        {
            ProtoBufUtils.Register(options);

            return me.WithProtoBuf();
        }

        #endregion

        #region Discovery

        public static IDiscoveryClient Discovery(DiscoveryClientOptions options)
        {
            if(options.IpAddress == null)
                throw new WitException("Discovery ip address is empty");

            if(options.Port == null || options.Port == 0)
                throw new WitException("Discovery port is empty");

            if(options.Serializer == null)
                throw new WitException("Serializer os empty");

            return new DiscoveryClient(options);
        }

        public static IDiscoveryClient Discovery(Action<DiscoveryClientOptions> optionsBuilder)
        {
            var options = new DiscoveryClientOptions();
            optionsBuilder(options);

            return Discovery(options);
        }

        public static DiscoveryClientOptions WithAddress(this DiscoveryClientOptions me, IPAddress ipAddress)
        {
            me.IpAddress = ipAddress;
            return me;
        }

        public static DiscoveryClientOptions WithAddress(this DiscoveryClientOptions me, IPAddress ipAddress, int port)
        {
            me.IpAddress = ipAddress;
            me.Port = port;
            return me;
        }

        public static DiscoveryClientOptions WithAddress(this DiscoveryClientOptions me, string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var address))
                return me;

            return me.WithAddress(address);
        }

        public static DiscoveryClientOptions WithAddress(this DiscoveryClientOptions me, string ipAddress, int port)
        {
            if (!IPAddress.TryParse(ipAddress, out var address))
                return me;

            return me.WithAddress(address, port);
        }

        #endregion

        #region Logger

        public static WitClientBuilderOptions WithLogger(this WitClientBuilderOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        public static DiscoveryClientOptions WithLogger(this DiscoveryClientOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        #endregion


        #region Timeout

        public static WitClientBuilderOptions WithTimeout(this WitClientBuilderOptions me, TimeSpan timeout)
        {
            me.Timeout = timeout;
            return me;
        }

        #endregion

        #region Reconnection

        /// <summary>
        /// Configures automatic reconnection for the client.
        /// </summary>
        public static WitClientBuilderOptions WithAutoReconnect(this WitClientBuilderOptions me, Action<ReconnectionOptions> configure)
        {
            me.ReconnectionOptions.Enabled = true;
            configure(me.ReconnectionOptions);
            return me;
        }

        /// <summary>
        /// Enables automatic reconnection with default settings.
        /// </summary>
        public static WitClientBuilderOptions WithAutoReconnect(this WitClientBuilderOptions me)
        {
            me.ReconnectionOptions.Enabled = true;
            return me;
        }

        /// <summary>
        /// Disables automatic reconnection.
        /// </summary>
        public static WitClientBuilderOptions WithoutAutoReconnect(this WitClientBuilderOptions me)
        {
            me.ReconnectionOptions.Enabled = false;
            return me;
        }

        #endregion

        #region Retry

        /// <summary>
        /// Configures retry policy for the client.
        /// </summary>
        public static WitClientBuilderOptions WithRetryPolicy(this WitClientBuilderOptions me, Action<RetryOptions> configure)
        {
            me.RetryOptions.Enabled = true;
            configure(me.RetryOptions);
            return me;
        }

        /// <summary>
        /// Enables retry policy with default settings.
        /// </summary>
        public static WitClientBuilderOptions WithRetryPolicy(this WitClientBuilderOptions me)
        {
            me.RetryOptions.Enabled = true;
            return me;
        }

        /// <summary>
        /// Disables retry policy.
        /// </summary>
        public static WitClientBuilderOptions WithoutRetryPolicy(this WitClientBuilderOptions me)
        {
            me.RetryOptions.Enabled = false;
            return me;
        }

        #endregion
    }
}

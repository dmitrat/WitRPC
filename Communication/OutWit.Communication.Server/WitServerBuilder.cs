using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Processors;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Discovery;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Server
{
    public static class WitServerBuilder
    {
        #region Constants

        private const string DEFAULT_DISCOVERY_IP = "239.255.255.250";
        private const int DEFAULT_DISCOVERY_PORT = 3702;

        #endregion

        public static WitServer Build(Action<WitServerBuilderOptions> optionsBuilder)
        {
            var options = new WitServerBuilderOptions();
            optionsBuilder(options);

            return Build(options);
        }

        public static WitServer Build(WitServerBuilderOptions options)
        {
            if (options.TransportFactory == null)
                throw new WitException("Transport cannot be empty");

            if (options.RequestProcessor == null)
                throw new WitException("Request processor cannot be empty");

            return new WitServer(options.TransportFactory, options.EncryptorFactory, options.TokenValidator, options.ParametersSerializer, options.MessageSerializer,
                options.RequestProcessor, options.DiscoveryServer, options.Logger, options.Timeout, options.Name, options.Description);
        }

        #region Processor

        public static WitServerBuilderOptions WithRequestProcessor(this WitServerBuilderOptions me, IRequestProcessor requestProcessor)
        {
            me.RequestProcessor = requestProcessor;
            return me;
        }

        public static WitServerBuilderOptions WithService<TService>(this WitServerBuilderOptions me, TService service, bool isStrongAssemblyMatch = true)
            where TService: class
        {
            me.RequestProcessor = new RequestProcessor<TService>(service, isStrongAssemblyMatch);
            return me;
        }

        public static WitServerBuilderOptions WithService<TService>(this WitServerBuilderOptions me, bool isStrongAssemblyMatch = true)
            where TService : class, new ()
        {
            me.RequestProcessor = new RequestProcessor<TService>(new TService(), isStrongAssemblyMatch);
            return me;
        }

        public static WitServerBuilderOptions WithService<TService>(this WitServerBuilderOptions me, Func<TService> serviceBuilder, bool isStrongAssemblyMatch = true)
            where TService : class, new()
        {
            me.RequestProcessor = new RequestProcessor<TService>(serviceBuilder(), isStrongAssemblyMatch);
            return me;
        }

        #endregion

        #region Authorization

        public static WitServerBuilderOptions WithAccessTokenValidator(this WitServerBuilderOptions me, IAccessTokenValidator validator)
        {
            me.TokenValidator = validator;
            return me;
        }

        public static WitServerBuilderOptions WithAccessToken(this WitServerBuilderOptions me, string accessToken)
        {
            me.TokenValidator = new AccessTokenValidatorStatic(accessToken);
            return me;
        }

        public static WitServerBuilderOptions WithoutAuthorization(this WitServerBuilderOptions me)
        {
            me.TokenValidator = new AccessTokenValidatorPlain();
            return me;
        }

        #endregion

        #region Encryption

        public static WitServerBuilderOptions WithEncryptor(this WitServerBuilderOptions me, IEncryptorServerFactory encryptorFactory)
        {
            me.EncryptorFactory = encryptorFactory;
            return me;
        }

        public static WitServerBuilderOptions WithEncryption(this WitServerBuilderOptions me)
        {
            me.EncryptorFactory = new EncryptorServerFactory<EncryptorServerGeneral>();
            return me;
        }

        public static WitServerBuilderOptions WithoutEncryption(this WitServerBuilderOptions me)
        {
            me.EncryptorFactory = new EncryptorServerFactory<EncryptorServerPlain>();
            return me;
        }

        #endregion

        #region Serialization

        public static WitServerBuilderOptions WithMessageSerializer(this WitServerBuilderOptions me, IMessageSerializer serializer)
        {
            me.MessageSerializer = serializer;
            return me;
        }

        public static WitServerBuilderOptions WithParametersSerializer(this WitServerBuilderOptions me, IMessageSerializer serializer)
        {
            me.ParametersSerializer = serializer;
            return me;
        }

        #endregion

        #region Json

        public static WitServerBuilderOptions WithJson(this WitServerBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerJson();
            return me;
        }

        public static WitServerBuilderOptions WithJson(this WitServerBuilderOptions me, Action<JsonOptions> options)
        {
            JsonUtils.Register(options);

            return me.WithJson();
        }

        #endregion

        #region MessagePack

        public static WitServerBuilderOptions WithMessagePack(this WitServerBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerMessagePack();
            return me;
        }

        public static WitServerBuilderOptions WithMessagePack(this WitServerBuilderOptions me, Action<MessagePackOptions> options)
        {
            MessagePackUtils.Register(options);
            
            return me.WithMessagePack();
        }
        
        #endregion

        #region MemoryPack

        public static WitServerBuilderOptions WithMemoryPack(this WitServerBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerMemoryPack();
            return me;
        }

        public static WitServerBuilderOptions WithMemoryPack(this WitServerBuilderOptions me, Action<MemoryPackOptions> options)
        {
            MemoryPackUtils.Register(options);
            return me.WithMemoryPack();
        }
        
        #endregion

        #region ProtoBuf

        public static WitServerBuilderOptions WithProtoBuf(this WitServerBuilderOptions me)
        {
            me.ParametersSerializer = new MessageSerializerProtoBuf();
            return me;
        }

        public static WitServerBuilderOptions WithProtoBuf(this WitServerBuilderOptions me, Action<ProtoBufOptions> options)
        {
            ProtoBufUtils.Register(options);
            
            return me.WithProtoBuf();
        }
        
        #endregion

        #region Logger

        public static WitServerBuilderOptions WithLogger(this WitServerBuilderOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        #endregion

        #region Discovery

        public static WitServerBuilderOptions WithName(this WitServerBuilderOptions me, string name)
        {
            me.Name = name;
            return me;
        }

        public static WitServerBuilderOptions WithDescription(this WitServerBuilderOptions me, string description)
        {
            me.Description = description;
            return me;
        }

        public static WitServerBuilderOptions WithDiscovery(this WitServerBuilderOptions me, DiscoveryServerOptions options)
        {
            me.DiscoveryServer = new DiscoveryServer(options);
            return me;
        }

        public static WitServerBuilderOptions WithDiscovery(this WitServerBuilderOptions me, IPAddress ipAddress, int port = DEFAULT_DISCOVERY_PORT, TimeSpan? period = null)
        {
            var mode = (period != null && period.Value != TimeSpan.Zero)
                ? DiscoveryServerMode.Continuous 
                : DiscoveryServerMode.StartStop;

            return me.WithDiscovery(new DiscoveryServerOptions
            {
                IpAddress = ipAddress,
                Port = port,
                Mode = mode,
                Period = period
            });
        }

        public static WitServerBuilderOptions WithDiscovery(this WitServerBuilderOptions me, string ipAddress, int port = DEFAULT_DISCOVERY_PORT, TimeSpan? period = null)
        {
            if(!IPAddress.TryParse(ipAddress, out var address))
                throw new WitException("Invalid IP address");

            return me.WithDiscovery(address, port, period);
        }

        public static WitServerBuilderOptions WithDiscovery(this WitServerBuilderOptions me, HostInfo hostInfo, TimeSpan? period = null)
        {
            if (hostInfo.Port == null || hostInfo.Port <= 0) 
                throw new WitException("Invalid port");

            return me.WithDiscovery(hostInfo.Host, hostInfo.Port.Value, period);
        }

        public static WitServerBuilderOptions WithDiscovery(this WitServerBuilderOptions me, TimeSpan? period = null)
        {
            return me.WithDiscovery(DEFAULT_DISCOVERY_IP, DEFAULT_DISCOVERY_PORT, period);
        }

        #endregion

        #region Timeout

        public static WitServerBuilderOptions WithTimeout(this WitServerBuilderOptions me, TimeSpan timeout)
        {
            me.Timeout = timeout;
            return me;
        }

        #endregion
    }
}

using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Converters;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Processors;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Server
{
    public static class WitComServerBuilder
    {
        public static WitComServer Build(Action<WitComServerBuilderOptions> optionsBuilder)
        {
            var options = new WitComServerBuilderOptions();
            optionsBuilder(options);

            if (options.TransportFactory == null)
                throw new WitComException("Transport cannot be empty");

            if (options.RequestProcessor == null)
                throw new WitComException("Request processor cannot be empty");

            return new WitComServer(options.TransportFactory, options.EncryptorFactory, options.TokenValidator, options.Serializer,
                options.Converter, options.RequestProcessor, options.Logger, options.Timeout);
        }

        #region Processor

        public static WitComServerBuilderOptions WithRequestProcessor(this WitComServerBuilderOptions me, IRequestProcessor requestProcessor)
        {
            me.RequestProcessor = requestProcessor;
            return me;
        }

        public static WitComServerBuilderOptions WithService<TService>(this WitComServerBuilderOptions me, TService service, bool isStrongAssemblyMatch = true)
            where TService: class
        {
            me.RequestProcessor = new RequestProcessor<TService>(service, isStrongAssemblyMatch);
            return me;
        }

        public static WitComServerBuilderOptions WithService<TService>(this WitComServerBuilderOptions me, bool isStrongAssemblyMatch = true)
            where TService : class, new ()
        {
            me.RequestProcessor = new RequestProcessor<TService>(new TService(), isStrongAssemblyMatch);
            return me;
        }

        public static WitComServerBuilderOptions WithService<TService>(this WitComServerBuilderOptions me, Func<TService> serviceBuilder, bool isStrongAssemblyMatch = true)
            where TService : class, new()
        {
            me.RequestProcessor = new RequestProcessor<TService>(serviceBuilder(), isStrongAssemblyMatch);
            return me;
        }

        #endregion

        #region Authorization

        public static WitComServerBuilderOptions WithAccessTokenValidator(this WitComServerBuilderOptions me, IAccessTokenValidator validator)
        {
            me.TokenValidator = validator;
            return me;
        }

        public static WitComServerBuilderOptions WithAccessToken(this WitComServerBuilderOptions me, string accessToken)
        {
            me.TokenValidator = new AccessTokenValidatorStatic(accessToken);
            return me;
        }

        public static WitComServerBuilderOptions WithoutAuthorization(this WitComServerBuilderOptions me)
        {
            me.TokenValidator = new AccessTokenValidatorPlain();
            return me;
        }

        #endregion

        #region Encryption

        public static WitComServerBuilderOptions WithEncryptor(this WitComServerBuilderOptions me, IEncryptorServerFactory encryptorFactory)
        {
            me.EncryptorFactory = encryptorFactory;
            return me;
        }

        public static WitComServerBuilderOptions WithEncryption(this WitComServerBuilderOptions me)
        {
            me.EncryptorFactory = new EncryptorServerFactory<EncryptorServerGeneral>();
            return me;
        }

        public static WitComServerBuilderOptions WithoutEncryption(this WitComServerBuilderOptions me)
        {
            me.EncryptorFactory = new EncryptorServerFactory<EncryptorServerPlain>();
            return me;
        }

        #endregion

        #region Serialization

        public static WitComServerBuilderOptions WithConverter(this WitComServerBuilderOptions me, IValueConverter converter)
        {
            me.Converter = converter;
            return me;
        }


        public static WitComServerBuilderOptions WithSerializer(this WitComServerBuilderOptions me, IMessageSerializer serializer)
        {
            me.Serializer = serializer;
            return me;
        }


        public static WitComServerBuilderOptions WithJson(this WitComServerBuilderOptions me)
        {
            me.Converter = new ValueConverterJson();
            me.Serializer = new MessageSerializerJson();
            return me;
        }

        public static WitComServerBuilderOptions WithMessagePack(this WitComServerBuilderOptions me)
        {
            me.Converter = new ValueConverterMessagePack();
            me.Serializer = new MessageSerializerMessagePack();
            return me;
        }

        #endregion

        #region Logger

        public static WitComServerBuilderOptions WithLogger(this WitComServerBuilderOptions me, ILogger logger)
        {
            me.Logger = logger;
            return me;
        }

        #endregion


        #region Timeout

        public static WitComServerBuilderOptions WithTimeout(this WitComServerBuilderOptions me, TimeSpan timeout)
        {
            me.Timeout = timeout;
            return me;
        }

        #endregion
    }
}

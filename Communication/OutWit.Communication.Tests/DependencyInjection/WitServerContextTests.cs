using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.DependencyInjection;
using OutWit.Communication.Server.DependencyInjection.Interfaces;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Server.Pipes;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.DependencyInjection
{
    [TestFixture]
    public class WitServerContextTests
    {
        #region Context Construction Tests

        [Test]
        public void ContextExposesServiceProviderTest()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var context = new WitServerBuilderContext(provider);

            Assert.That(context.ServiceProvider, Is.SameAs(provider));
        }

        #endregion

        #region WithLogger Tests

        [Test]
        public void WithLoggerGenericResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            var result = context.WithLogger<ILogger<WitServerContextTests>>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Logger, Is.Not.Null);
            Assert.That(context.Logger, Is.InstanceOf<ILogger<WitServerContextTests>>());
        }

        [Test]
        public void WithLoggerCategoryNameCreatesLoggerTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            var result = context.WithLogger("ServerCategory");

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Logger, Is.Not.Null);
        }

        [Test]
        public void WithLoggerCategoryNameThrowsOnNullCategoryTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            Assert.Throws<ArgumentNullException>(() => context.WithLogger(null!));
        }

        #endregion

        #region WithAccessTokenValidator Tests

        [Test]
        public void WithAccessTokenValidatorResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAccessTokenValidator>(new AccessTokenValidatorStatic("test-token"));
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            var result = context.WithAccessTokenValidator<IAccessTokenValidator>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.TokenValidator, Is.InstanceOf<AccessTokenValidatorStatic>());
        }

        #endregion

        #region WithEncryptor Tests

        [Test]
        public void WithEncryptorResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IEncryptorServerFactory>(new EncryptorServerFactory<EncryptorServerPlain>());
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            var result = context.WithEncryptor<IEncryptorServerFactory>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.EncryptorFactory, Is.Not.Null);
        }

        #endregion

        #region WithService Tests

        [Test]
        public void WithServiceResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IService>(new MockService());
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            var result = context.WithService<IService>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.RequestProcessor, Is.Not.Null);
        }

        #endregion

        #region Registration with Context Tests

        [Test]
        public void AddWitRpcServerWithContextConfiguresServerTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IService>(new MockService());
            var loggerResolved = false;

            services.AddWitRpcServer("ctx-server", ctx =>
            {
                ctx.WithNamedPipe("ctx-server-pipe", maxNumberOfClients: 1);
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
                ctx.WithService<IService>();
                ctx.WithLogger<ILogger<WitServerContextTests>>();
                loggerResolved = true;
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();
            var server = factory.GetServer("ctx-server");

            Assert.That(server, Is.Not.Null);
            Assert.That(loggerResolved, Is.True);
        }

        [Test]
        public void AddWitRpcServerWithContextAndAutoStartRegistersHostedServiceTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IService>(new MockService());

            services.AddWitRpcServer("ctx-auto-server", ctx =>
            {
                ctx.WithNamedPipe("ctx-auto-srv-pipe", maxNumberOfClients: 1);
                ctx.WithJson();
                ctx.WithService<IService>();
            }, autoStart: true);

            var hostedServiceDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
            Assert.That(hostedServiceDescriptor, Is.Not.Null);
        }

        #endregion

        #region Chaining Tests

        [Test]
        public void ContextExtensionMethodsChainingTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IAccessTokenValidator>(new AccessTokenValidatorStatic("chain-token"));
            services.AddSingleton<IEncryptorServerFactory>(new EncryptorServerFactory<EncryptorServerPlain>());
            var provider = services.BuildServiceProvider();
            var context = new WitServerBuilderContext(provider);

            var result = context
                .WithLogger<ILogger<WitServerContextTests>>()
                .WithAccessTokenValidator<IAccessTokenValidator>()
                .WithEncryptor<IEncryptorServerFactory>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Logger, Is.Not.Null);
            Assert.That(context.TokenValidator, Is.InstanceOf<AccessTokenValidatorStatic>());
            Assert.That(context.EncryptorFactory, Is.Not.Null);
        }

        #endregion
    }
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection;
using OutWit.Communication.Client.DependencyInjection.Interfaces;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Tests.DependencyInjection
{
    [TestFixture]
    public class WitClientContextTests
    {
        #region Context Construction Tests

        [Test]
        public void ContextExposesOptionsAndServiceProviderTest()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var options = new WitClientBuilderOptions();

            var context = new WitClientBuilderContext(options, provider);

            Assert.That(context.Options, Is.SameAs(options));
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
            var options = new WitClientBuilderOptions();
            var context = new WitClientBuilderContext(options, provider);

            var result = context.WithLogger<ILogger<WitClientContextTests>>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(options.Logger, Is.Not.Null);
            Assert.That(options.Logger, Is.InstanceOf<ILogger<WitClientContextTests>>());
        }

        [Test]
        public void WithLoggerCategoryNameCreatesLoggerTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var options = new WitClientBuilderOptions();
            var context = new WitClientBuilderContext(options, provider);

            var result = context.WithLogger("TestCategory");

            Assert.That(result, Is.SameAs(context));
            Assert.That(options.Logger, Is.Not.Null);
        }

        [Test]
        public void WithLoggerCategoryNameThrowsOnNullCategoryTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var options = new WitClientBuilderOptions();
            var context = new WitClientBuilderContext(options, provider);

            Assert.Throws<ArgumentNullException>(() => context.WithLogger(null!));
        }

        #endregion

        #region WithAccessTokenProvider Tests

        [Test]
        public void WithAccessTokenProviderResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAccessTokenProvider>(new MockAccessTokenProvider("test-token"));
            var provider = services.BuildServiceProvider();
            var options = new WitClientBuilderOptions();
            var context = new WitClientBuilderContext(options, provider);

            var result = context.WithAccessTokenProvider<IAccessTokenProvider>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(options.TokenProvider, Is.InstanceOf<MockAccessTokenProvider>());
        }

        #endregion

        #region WithEncryptor Tests

        [Test]
        public void WithEncryptorResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IEncryptorClient>(new EncryptorClientPlain());
            var provider = services.BuildServiceProvider();
            var options = new WitClientBuilderOptions();
            var context = new WitClientBuilderContext(options, provider);

            var result = context.WithEncryptor<IEncryptorClient>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(options.Encryptor, Is.InstanceOf<EncryptorClientPlain>());
        }

        #endregion

        #region Registration with Context Tests

        [Test]
        public void AddWitRpcClientWithContextConfiguresClientTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var loggerResolved = false;

            services.AddWitRpcClient("ctx-client", ctx =>
            {
                ctx.Options.WithNamedPipe("ctx-test-pipe");
                ctx.Options.WithJson();
                ctx.Options.WithoutEncryption();
                ctx.Options.WithoutAuthorization();
                ctx.WithLogger<ILogger<WitClientContextTests>>();
                loggerResolved = true;
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            var client = factory.GetClient("ctx-client");

            Assert.That(client, Is.Not.Null);
            Assert.That(loggerResolved, Is.True);
        }

        [Test]
        public void AddWitRpcClientTypedWithContextRegistersServiceTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcClient<ITestService>("ctx-typed", ctx =>
            {
                ctx.Options.WithNamedPipe("ctx-typed-pipe");
                ctx.Options.WithJson();
                ctx.Options.WithoutEncryption();
                ctx.Options.WithoutAuthorization();
            });

            var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestService));
            Assert.That(serviceDescriptor, Is.Not.Null);
        }

        [Test]
        public void AddWitRpcClientWithContextAndAutoConnectRegistersHostedServiceTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddWitRpcClient("ctx-auto", ctx =>
            {
                ctx.Options.WithNamedPipe("ctx-auto-pipe");
                ctx.Options.WithJson();
            }, autoConnect: true, connectionTimeout: TimeSpan.FromSeconds(5));

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
            services.AddSingleton<IAccessTokenProvider>(new MockAccessTokenProvider("chain-token"));
            services.AddSingleton<IEncryptorClient>(new EncryptorClientPlain());
            var provider = services.BuildServiceProvider();
            var options = new WitClientBuilderOptions();
            var context = new WitClientBuilderContext(options, provider);

            var result = context
                .WithLogger<ILogger<WitClientContextTests>>()
                .WithAccessTokenProvider<IAccessTokenProvider>()
                .WithEncryptor<IEncryptorClient>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(options.Logger, Is.Not.Null);
            Assert.That(options.TokenProvider, Is.InstanceOf<MockAccessTokenProvider>());
            Assert.That(options.Encryptor, Is.InstanceOf<EncryptorClientPlain>());
        }

        #endregion

        #region Mock Types

        private sealed class MockAccessTokenProvider : IAccessTokenProvider
        {
            private readonly string m_token;

            public MockAccessTokenProvider(string token)
            {
                m_token = token;
            }

            public Task<string> GetToken() => Task.FromResult(m_token);
        }

        public interface ITestService
        {
            string GetData();
        }

        #endregion
    }
}

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
        public void ContextExposesServiceProviderTest()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var context = new WitClientBuilderContext(provider);

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
            var context = new WitClientBuilderContext(provider);

            var result = context.WithLogger<ILogger<WitClientContextTests>>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Logger, Is.Not.Null);
            Assert.That(context.Logger, Is.InstanceOf<ILogger<WitClientContextTests>>());
        }

        [Test]
        public void WithLoggerCategoryNameCreatesLoggerTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var context = new WitClientBuilderContext(provider);

            var result = context.WithLogger("TestCategory");

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Logger, Is.Not.Null);
        }

        [Test]
        public void WithLoggerCategoryNameThrowsOnNullCategoryTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var context = new WitClientBuilderContext(provider);

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
            var context = new WitClientBuilderContext(provider);

            var result = context.WithAccessTokenProvider<IAccessTokenProvider>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.TokenProvider, Is.InstanceOf<MockAccessTokenProvider>());
        }

        #endregion

        #region WithEncryptor Tests

        [Test]
        public void WithEncryptorResolvesFromServiceProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IEncryptorClient>(new EncryptorClientPlain());
            var provider = services.BuildServiceProvider();
            var context = new WitClientBuilderContext(provider);

            var result = context.WithEncryptor<IEncryptorClient>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Encryptor, Is.InstanceOf<EncryptorClientPlain>());
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
                ctx.WithNamedPipe("ctx-test-pipe");
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
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
                ctx.WithNamedPipe("ctx-typed-pipe");
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
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
                ctx.WithNamedPipe("ctx-auto-pipe");
                ctx.WithJson();
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
            var context = new WitClientBuilderContext(provider);

            var result = context
                .WithLogger<ILogger<WitClientContextTests>>()
                .WithAccessTokenProvider<IAccessTokenProvider>()
                .WithEncryptor<IEncryptorClient>();

            Assert.That(result, Is.SameAs(context));
            Assert.That(context.Logger, Is.Not.Null);
            Assert.That(context.TokenProvider, Is.InstanceOf<MockAccessTokenProvider>());
            Assert.That(context.Encryptor, Is.InstanceOf<EncryptorClientPlain>());
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

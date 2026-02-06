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
    public class WitServerDependencyInjectionTests
    {
        #region Factory Tests

        [Test]
        public void AddWitRpcServerFactoryRegistersFactoryTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServerFactory();

            var provider = services.BuildServiceProvider();
            var factory = provider.GetService<IWitServerFactory>();

            Assert.That(factory, Is.Not.Null);
            Assert.That(factory, Is.InstanceOf<WitServerFactory>());
        }

        [Test]
        public void AddWitRpcServerRegistersConfigurationTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("test-server", options =>
            {
                options.WithNamedPipe("test-server-pipe", maxNumberOfClients: 1);
                options.WithJson();
                options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void GetServerReturnsConfiguredServerTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("test-server", options =>
            {
                options.WithNamedPipe("test-server-pipe-get", maxNumberOfClients: 1);
                options.WithJson();
                options.WithoutEncryption();
                options.WithoutAuthorization();
                options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            var server = factory.GetServer("test-server");

            Assert.That(server, Is.Not.Null);
        }

        [Test]
        public void GetServerReturnsSameInstanceForSameNameTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("test-server", options =>
            {
                options.WithNamedPipe("test-server-pipe-same", maxNumberOfClients: 1);
                options.WithJson();
                options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            var server1 = factory.GetServer("test-server");
            var server2 = factory.GetServer("test-server");

            Assert.That(server1, Is.SameAs(server2));
        }

        [Test]
        public void GetServerThrowsForUnknownNameTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServerFactory();

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            Assert.Throws<InvalidOperationException>(() => factory.GetServer("unknown"));
        }

        [Test]
        public void MultipleServersCanBeRegisteredTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("server-1", options =>
            {
                options.WithNamedPipe("pipe-srv-1", maxNumberOfClients: 1);
                options.WithJson();
                options.WithService(new MockService());
            });

            services.AddWitRpcServer("server-2", options =>
            {
                options.WithNamedPipe("pipe-srv-2", maxNumberOfClients: 1);
                options.WithJson();
                options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            var server1 = factory.GetServer("server-1");
            var server2 = factory.GetServer("server-2");

            Assert.That(server1, Is.Not.Null);
            Assert.That(server2, Is.Not.Null);
            Assert.That(server1, Is.Not.SameAs(server2));
        }

        #endregion

        #region Typed Service Tests

        [Test]
        public void AddWitRpcServerTypedRegistersServiceInDITest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer<IService, MockService>("typed-server", options =>
            {
                options.WithNamedPipe("typed-server-pipe", maxNumberOfClients: 1);
                options.WithJson();
                options.WithoutEncryption();
                options.WithoutAuthorization();
            });

            var provider = services.BuildServiceProvider();

            var resolvedService = provider.GetService<IService>();
            Assert.That(resolvedService, Is.Not.Null);
            Assert.That(resolvedService, Is.InstanceOf<MockService>());
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void FactoryDisposesAllServersTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("test-server", options =>
            {
                options.WithNamedPipe("dispose-srv-test", maxNumberOfClients: 1);
                options.WithJson();
                options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<WitServerFactory>();

            var server = factory.GetServer("test-server");

            Assert.DoesNotThrow(() => factory.Dispose());
        }

        #endregion
    }
}

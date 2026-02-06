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

            services.AddWitRpcServer("test-server", ctx =>
            {
                ctx.Options.WithNamedPipe("test-server-pipe", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void GetServerReturnsConfiguredServerTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("test-server", ctx =>
            {
                ctx.Options.WithNamedPipe("test-server-pipe-get", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithoutEncryption();
                ctx.Options.WithoutAuthorization();
                ctx.Options.WithService(new MockService());
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

            services.AddWitRpcServer("test-server", ctx =>
            {
                ctx.Options.WithNamedPipe("test-server-pipe-same", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithService(new MockService());
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

            services.AddWitRpcServer("server-1", ctx =>
            {
                ctx.Options.WithNamedPipe("pipe-srv-1", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithService(new MockService());
            });

            services.AddWitRpcServer("server-2", ctx =>
            {
                ctx.Options.WithNamedPipe("pipe-srv-2", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithService(new MockService());
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

            services.AddWitRpcServer<IService, MockService>("typed-server", ctx =>
            {
                ctx.Options.WithNamedPipe("typed-server-pipe", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithoutEncryption();
                ctx.Options.WithoutAuthorization();
            });

            var provider = services.BuildServiceProvider();

            var resolvedService = provider.GetService<IService>();
            Assert.That(resolvedService, Is.Not.Null);
            Assert.That(resolvedService, Is.InstanceOf<MockService>());
        }

        #endregion

        #region Composite Services Tests

        [Test]
        public void AddWitRpcServerWithServicesRegistersServerTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IService>(new MockService());

            services.AddWitRpcServerWithServices("composite-server",
                ctx =>
                {
                    ctx.Options.WithNamedPipe("composite-srv-pipe", maxNumberOfClients: 1);
                    ctx.Options.WithJson();
                    ctx.Options.WithoutEncryption();
                    ctx.Options.WithoutAuthorization();
                },
                composite =>
                {
                    composite.AddService<IService>();
                });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            var server = factory.GetServer("composite-server");
            Assert.That(server, Is.Not.Null);
        }

        [Test]
        public void AddWitRpcServerWithServicesUsesContextExtensionsTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IService>(new MockService());
            var loggerResolved = false;

            services.AddWitRpcServerWithServices("ctx-composite-server",
                ctx =>
                {
                    ctx.Options.WithNamedPipe("ctx-composite-pipe", maxNumberOfClients: 1);
                    ctx.Options.WithJson();
                    ctx.Options.WithoutEncryption();
                    ctx.Options.WithoutAuthorization();
                    ctx.WithLogger<ILogger<WitServerDependencyInjectionTests>>();
                    loggerResolved = true;
                },
                composite =>
                {
                    composite.AddService<IService>();
                });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerFactory>();

            var server = factory.GetServer("ctx-composite-server");
            Assert.That(server, Is.Not.Null);
            Assert.That(loggerResolved, Is.True);
        }

        [Test]
        public void AddWitRpcServerWithServicesAndAutoStartRegistersHostedServiceTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IService>(new MockService());

            services.AddWitRpcServerWithServices("auto-composite-server",
                ctx =>
                {
                    ctx.Options.WithNamedPipe("auto-composite-pipe", maxNumberOfClients: 1);
                    ctx.Options.WithJson();
                },
                composite =>
                {
                    composite.AddService<IService>();
                },
                autoStart: true);

            var hostedServiceDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
            Assert.That(hostedServiceDescriptor, Is.Not.Null);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void FactoryDisposesAllServersTest()
        {
            var services = new ServiceCollection();

            services.AddWitRpcServer("test-server", ctx =>
            {
                ctx.Options.WithNamedPipe("dispose-srv-test", maxNumberOfClients: 1);
                ctx.Options.WithJson();
                ctx.Options.WithService(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<WitServerFactory>();

            var server = factory.GetServer("test-server");

            Assert.DoesNotThrow(() => factory.Dispose());
        }

        #endregion
    }
}

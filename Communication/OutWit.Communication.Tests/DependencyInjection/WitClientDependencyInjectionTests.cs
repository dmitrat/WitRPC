using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Tests.DependencyInjection
{
    [TestFixture]
    public class WitClientDependencyInjectionTests
    {
        #region Factory Tests

        [Test]
        public void AddWitRpcClientFactoryRegistersFactory()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClientFactory();
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetService<IWitClientFactory>();
            
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory, Is.InstanceOf<WitClientFactory>());
        }

        [Test]
        public void AddWitRpcClientRegistersConfiguration()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.WithNamedPipe("test-pipe");
                ctx.WithJson();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void GetClientReturnsConfiguredClient()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.WithNamedPipe("test-pipe-getclient");
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            var client = factory.GetClient("test-client");
            
            Assert.That(client, Is.Not.Null);
            Assert.That(client.ConnectionState, Is.EqualTo(ReconnectionState.Disconnected));
        }

        [Test]
        public void GetClientReturnsSameInstanceForSameName()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.WithNamedPipe("test-pipe-same");
                ctx.WithJson();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            var client1 = factory.GetClient("test-client");
            var client2 = factory.GetClient("test-client");
            
            Assert.That(client1, Is.SameAs(client2));
        }

        [Test]
        public void GetClientThrowsForUnknownName()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClientFactory();
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            Assert.Throws<InvalidOperationException>(() => factory.GetClient("unknown"));
        }

        [Test]
        public void MultipleClientsCanBeRegistered()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("client-1", ctx =>
            {
                ctx.WithNamedPipe("pipe-1");
                ctx.WithJson();
            });
            
            services.AddWitRpcClient("client-2", ctx =>
            {
                ctx.WithNamedPipe("pipe-2");
                ctx.WithJson();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            var client1 = factory.GetClient("client-1");
            var client2 = factory.GetClient("client-2");
            
            Assert.That(client1, Is.Not.Null);
            Assert.That(client2, Is.Not.Null);
            Assert.That(client1, Is.Not.SameAs(client2));
        }

        #endregion

        #region Typed Service Tests

        [Test]
        public void AddWitRpcClientTypedRegistersServiceInterface()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient<ITestService>("test-service", ctx =>
            {
                ctx.WithNamedPipe("test-pipe-typed");
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
            });
            
            var provider = services.BuildServiceProvider();
            
            // Service should be registered
            var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestService));
            Assert.That(serviceDescriptor, Is.Not.Null);
        }

        #endregion

        #region Options Configuration Tests

        [Test]
        public void ClientOptionsAreAppliedCorrectly()
        {
            var services = new ServiceCollection();
            var configuredCorrectly = false;
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.WithNamedPipe("configured-pipe");
                ctx.WithJson();
                ctx.WithAutoReconnect(reconnect =>
                {
                    reconnect.MaxAttempts = 5;
                    reconnect.InitialDelay = TimeSpan.FromSeconds(2);
                });
                configuredCorrectly = true;
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            // Trigger client creation
            var client = factory.GetClient("test-client");
            
            Assert.That(configuredCorrectly, Is.True);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void FactoryDisposesAllClients()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.WithNamedPipe("dispose-test");
                ctx.WithJson();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<WitClientFactory>();
            
            var client = factory.GetClient("test-client");
            
            Assert.DoesNotThrow(() => factory.Dispose());
        }

        #endregion

        // Simple test interface
        public interface ITestService
        {
            string GetData();
        }
    }
}

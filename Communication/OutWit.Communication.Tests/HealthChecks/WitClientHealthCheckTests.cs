using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection;
using OutWit.Communication.Client.DependencyInjection.Interfaces;
using OutWit.Communication.Client.HealthChecks;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Reconnection;

namespace OutWit.Communication.Tests.HealthChecks
{
    [TestFixture]
    public class WitClientHealthCheckTests
    {
        #region Health Check Registration Tests

        [Test]
        public void AddWitRpcClientHealthCheckRegistersHealthCheck()
        {
            var services = new ServiceCollection();
            
            // Add logging (required by HealthCheckService)
            services.AddLogging();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.Options.WithNamedPipe("health-test-pipe");
                ctx.Options.WithJson();
            });
            
            services.AddHealthChecks()
                .AddWitRpcClient("test-client");
            
            var provider = services.BuildServiceProvider();
            var healthCheckService = provider.GetService<HealthCheckService>();
            
            Assert.That(healthCheckService, Is.Not.Null);
        }

        [Test]
        public void AddWitRpcClientHealthCheckWithCustomName()
        {
            var services = new ServiceCollection();
            
            // Add logging (required by HealthCheckService)
            services.AddLogging();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.Options.WithNamedPipe("health-test-pipe-name");
                ctx.Options.WithJson();
            });
            
            services.AddHealthChecks()
                .AddWitRpcClient("test-client", name: "custom-health-check");
            
            var provider = services.BuildServiceProvider();
            var healthCheckService = provider.GetService<HealthCheckService>();
            
            Assert.That(healthCheckService, Is.Not.Null);
        }

        #endregion

        #region Health Check Status Tests

        [Test]
        public async Task HealthCheckReturnsUnhealthyWhenDisconnected()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.Options.WithNamedPipe("health-disconnected");
                ctx.Options.WithJson();
                ctx.Options.WithoutEncryption();
                ctx.Options.WithoutAuthorization();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            // Create client but don't connect
            var client = factory.GetClient("test-client");
            
            var healthCheck = new WitClientHealthCheck(factory, "test-client");
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            };
            
            var result = await healthCheck.CheckHealthAsync(context);
            
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Does.Contain("not connected"));
        }

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        public async Task HealthCheckReturnsHealthyWhenConnected(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(HealthCheckReturnsHealthyWhenConnected)}{transportType}{serializerType}";
            
            // Start server
            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();
            
            try
            {
                var services = new ServiceCollection();
                
                services.AddWitRpcClient("test-client", ctx =>
                {
                    ctx.Options.WithNamedPipe(testName);
                    ctx.Options.WithJson();
                    ctx.Options.WithEncryption();
                    ctx.Options.WithAccessToken("token");
                });
                
                var provider = services.BuildServiceProvider();
                var factory = provider.GetRequiredService<IWitClientFactory>();
                
                var client = factory.GetClient("test-client");
                await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                
                var healthCheck = new WitClientHealthCheck(factory, "test-client");
                var context = new HealthCheckContext
                {
                    Registration = new HealthCheckRegistration("test", healthCheck, null, null)
                };
                
                var result = await healthCheck.CheckHealthAsync(context);
                
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
                Assert.That(result.Description, Does.Contain("connected"));
                
                await client.Disconnect();
            }
            finally
            {
                server.StopWaitingForConnection();
            }
        }

        [Test]
        public async Task HealthCheckReturnsDataWithConnectionState()
        {
            var services = new ServiceCollection();
            
            services.AddWitRpcClient("test-client", ctx =>
            {
                ctx.Options.WithNamedPipe("health-data-test");
                ctx.Options.WithJson();
            });
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            var client = factory.GetClient("test-client");
            
            var healthCheck = new WitClientHealthCheck(factory, "test-client");
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            };
            
            var result = await healthCheck.CheckHealthAsync(context);
            
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.ContainsKey("clientName"), Is.True);
            Assert.That(result.Data.ContainsKey("connectionState"), Is.True);
            Assert.That(result.Data.ContainsKey("isInitialized"), Is.True);
            Assert.That(result.Data.ContainsKey("isAuthorized"), Is.True);
            Assert.That(result.Data["clientName"], Is.EqualTo("test-client"));
        }

        [Test]
        public async Task HealthCheckHandlesExceptionGracefully()
        {
            var services = new ServiceCollection();
            services.AddWitRpcClientFactory();
            
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientFactory>();
            
            // Use non-existent client name
            var healthCheck = new WitClientHealthCheck(factory, "non-existent");
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null)
            };
            
            var result = await healthCheck.CheckHealthAsync(context);
            
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Exception, Is.Not.Null);
        }

        #endregion
    }
}

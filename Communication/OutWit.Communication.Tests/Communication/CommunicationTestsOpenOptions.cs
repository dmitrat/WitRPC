using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection;
using OutWit.Communication.Client.DependencyInjection.Interfaces;
using OutWit.Communication.Client.Pipes;
using OutWit.Communication.Processors;
using OutWit.Communication.Server;
using OutWit.Communication.Server.DependencyInjection;
using OutWit.Communication.Server.DependencyInjection.Interfaces;
using OutWit.Communication.Server.Discovery;
using OutWit.Communication.Server.Pipes;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Communication
{
    /// <summary>
    /// The open form of every builder option -- the one that takes an
    /// implementation of the interface rather than a convenience preset --
    /// both on the builders and, resolved from the container, on the DI contexts.
    /// </summary>
    [TestFixture]
    public class CommunicationTestsOpenOptions
    {
        #region Constants

        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(10);

        #endregion

        #region Setup

        private static NamedPipeServerTransportFactory ServerPipe(string name)
        {
            return new NamedPipeServerTransportFactory(new NamedPipeServerTransportOptions
            {
                PipeName = Shared.ChannelName(name),
                MaxNumberOfClients = 1
            });
        }

        private static NamedPipeClientTransport ClientPipe(string name)
        {
            return new NamedPipeClientTransport(new NamedPipeClientTransportOptions
            {
                ServerName = ".",
                PipeName = Shared.ChannelName(name)
            });
        }

        #endregion

        #region Builder Tests

        [Test]
        public async Task CustomTransportInstancesMakeARoundTripTest()
        {
            const string name = "open-options-builder";

            using var server = WitServerBuilder.Build(options =>
            {
                options.WithTransport(ServerPipe(name));
                options.WithService<IService>(new MockService());
                options.WithDiscovery(new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }));
                options.WithJson();
                options.WithoutEncryption();
                options.WithoutAuthorization();
            });
            server.StartWaitingForConnection();

            using var client = WitClientBuilder.Build(options =>
            {
                options.WithTransport(ClientPipe(name));
                options.WithJson();
                options.WithoutEncryption();
                options.WithoutAuthorization();
            });

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
            Assert.That(Shared.GetService(client).RequestData("open"), Is.EqualTo("open"));
        }

        #endregion

        #region Container Tests

        [Test]
        public async Task TransportAndProcessorResolvedFromTheContainerTest()
        {
            const string name = "open-options-container";

            var services = new ServiceCollection();
            services.AddSingleton(ServerPipe(name));
            services.AddSingleton(ClientPipe(name));
            services.AddSingleton(new RequestProcessor<IService>(new MockService()));

            services.AddWitRpcServer("server", ctx =>
            {
                ctx.WithTransport<NamedPipeServerTransportFactory>();
                ctx.WithRequestProcessor<RequestProcessor<IService>>();
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
            });
            services.AddWitRpcClient("client", ctx =>
            {
                ctx.WithTransport<NamedPipeClientTransport>();
                ctx.WithJson();
                ctx.WithoutEncryption();
                ctx.WithoutAuthorization();
            });

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerFactory>().GetServer("server");
            server.StartWaitingForConnection();

            using var client = provider.GetRequiredService<IWitClientFactory>().GetClient("client");

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);
            Assert.That(Shared.GetService(client).RequestData("container"), Is.EqualTo("container"));
        }

        #endregion
    }
}

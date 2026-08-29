using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Communication.Client.DependencyInjection;
using OutWit.Communication.Client.DependencyInjection.Interfaces;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Server.DependencyInjection;
using OutWit.Communication.Server.DependencyInjection.Interfaces;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.DependencyInjection
{
    /// <summary>
    /// REST clients registered and resolved through the container, calling a
    /// REST server that was registered through the container as well.
    /// </summary>
    [TestFixture]
    public class WitClientRestDependencyInjectionTests
    {
        #region Constants

        private const string TOKEN = "rest-di-token";

        #endregion

        #region Setup

        private static string NextUrl()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return $"http://localhost:{port}/di/";
        }

        #endregion

        #region Factory Tests

        [Test]
        public void AddWitRpcRestClientFactoryRegistersFactoryTest()
        {
            var services = new ServiceCollection();
            services.AddWitRpcRestClientFactory();

            var provider = services.BuildServiceProvider();

            Assert.That(provider.GetService<IWitClientRestFactory>(), Is.InstanceOf<WitClientRestFactory>());
        }

        [Test]
        public void GetClientReturnsSameInstanceForSameNameTest()
        {
            var services = new ServiceCollection();
            services.AddWitRpcRestClient("rest", ctx => ctx.WithUrl("http://localhost:1/"));

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitClientRestFactory>();

            var first = factory.GetClient("rest");
            var second = factory.GetClient("rest");

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.Options.Host?.Connection, Is.Not.Empty);
        }

        [Test]
        public void GetClientThrowsForUnknownNameTest()
        {
            var services = new ServiceCollection();
            services.AddWitRpcRestClientFactory();

            var provider = services.BuildServiceProvider();

            Assert.That(() => provider.GetRequiredService<IWitClientRestFactory>().GetClient("missing"), Throws.InvalidOperationException);
        }

        #endregion

        #region Round-Trip Tests

        [Test]
        public void InjectedServiceProxyCallsTheContainerHostedServerTest()
        {
            var url = NextUrl();

            // The server takes its implementation directly: registering IService for
            // the server and IService as the injected client proxy in one container
            // would hand the server the proxy, which would call itself.
            var services = new ServiceCollection();
            services.AddWitRpcRestServer("server", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithAccessToken(TOKEN);
                ctx.WithService<IService>(new MockService());
            });
            services.AddWitRpcRestClient<IService>("client", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithAccessToken(TOKEN);
                ctx.WithTimeout(TimeSpan.FromSeconds(10));
            }, strongAssemblyMatch: false);

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("server");
            server.StartWaitingForConnection();

            var service = provider.GetRequiredService<IService>();

            Assert.That(service.RequestData("through di"), Is.EqualTo("through di"));
            Assert.That(service.RequestWithMultipleNullableParams(null, 7, null), Is.EqualTo(new MockService().RequestWithMultipleNullableParams(null, 7, null)));
        }

        [Test]
        public void WrongTokenSurfacesAsAFaultTest()
        {
            var url = NextUrl();

            var services = new ServiceCollection();
            services.AddWitRpcRestServer("server", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithAccessToken(TOKEN);
                ctx.WithService<IService>(new MockService());
            });
            services.AddWitRpcRestClient<IService>("client", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithAccessToken("wrong");
                ctx.WithTimeout(TimeSpan.FromSeconds(10));
            }, strongAssemblyMatch: false);

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("server");
            server.StartWaitingForConnection();

            var service = provider.GetRequiredService<IService>();

            Assert.That(() => service.RequestData("nope"), Throws.Exception.With.Message.Contains("Token"));
        }


        [Test]
        public void NamedHttpClientAndLoggerComeFromTheContainerTest()
        {
            var url = NextUrl();
            var handler = new MockHttpHandlerCounting();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpClient("rest-http").AddHttpMessageHandler(() => handler);
            services.AddWitRpcRestServer("server", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithoutAuthorization();
                ctx.WithService<IService>(new MockService());
            });
            services.AddWitRpcRestClient<IService>("client", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithoutAuthorization();
                ctx.WithHttpClient("rest-http");
                ctx.WithLogger("WitRPC.Rest");
                ctx.WithTimeout(TimeSpan.FromSeconds(10));
            }, strongAssemblyMatch: false);

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("server");
            server.StartWaitingForConnection();

            var service = provider.GetRequiredService<IService>();
            var client = provider.GetRequiredService<IWitClientRestFactory>().GetClient("client");

            Assert.That(service.RequestData("through factory"), Is.EqualTo("through factory"));
            Assert.That(handler.Requests, Is.EqualTo(1));
            Assert.That(client.Logger, Is.Not.Null);
        }

        #endregion
    }
}

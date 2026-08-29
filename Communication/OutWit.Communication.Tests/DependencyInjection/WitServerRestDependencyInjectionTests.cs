using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using OutWit.Communication.Server.DependencyInjection;
using OutWit.Communication.Server.DependencyInjection.Interfaces;
using OutWit.Communication.Processors;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.DependencyInjection
{
    /// <summary>
    /// REST servers registered, built, started and called through the container.
    /// </summary>
    [TestFixture]
    public class WitServerRestDependencyInjectionTests
    {
        #region Fields

        private static readonly HttpClient s_http = new();

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

        private static async Task<(HttpStatusCode status, string body)> PostAsync(string url, string method, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await s_http.PostAsync(url + method, content);
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        #endregion

        #region Factory Tests

        [Test]
        public void AddWitRpcRestServerFactoryRegistersFactoryTest()
        {
            var services = new ServiceCollection();
            services.AddWitRpcRestServerFactory();

            var provider = services.BuildServiceProvider();

            Assert.That(provider.GetService<IWitServerRestFactory>(), Is.InstanceOf<WitServerRestFactory>());
        }

        [Test]
        public void GetServerReturnsSameInstanceForSameNameTest()
        {
            var services = new ServiceCollection();
            services.AddWitRpcRestServer("rest", ctx =>
            {
                ctx.WithUrl(NextUrl());
                ctx.WithService<IService>(new MockService());
            });

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerRestFactory>();

            var first = factory.GetServer("rest");
            var second = factory.GetServer("rest");

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GetServerThrowsForUnknownNameTest()
        {
            var services = new ServiceCollection();
            services.AddWitRpcRestServerFactory();

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IWitServerRestFactory>();

            Assert.That(() => factory.GetServer("missing"), Throws.InvalidOperationException);
        }

        #endregion

        #region Registration Tests

        [Test]
        public async Task ServiceResolvedFromContainerAnswersHttpTest()
        {
            var url = NextUrl();

            var services = new ServiceCollection();
            services.AddSingleton<IService, MockService>();
            services.AddWitRpcRestServer("rest", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithoutAuthorization();
                ctx.WithService<IService>();
            });

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("rest");
            server.StartWaitingForConnection();

            var (status, body) = await PostAsync(url, "RequestData", "{\"message\":\"from di\"}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("from di"));
        }

        [Test]
        public async Task TypedRegistrationExposesTheImplementationTest()
        {
            var url = NextUrl();

            var services = new ServiceCollection();
            services.AddWitRpcRestServer<IService, MockService>("rest", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithoutAuthorization();
            });

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("rest");
            server.StartWaitingForConnection();

            var (status, body) = await PostAsync(url, "RequestData", "[\"typed\"]");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("typed"));
        }

        [Test]
        public async Task AutoStartStartsWithTheHostTest()
        {
            var url = NextUrl();

            var services = new ServiceCollection();
            services.AddWitRpcRestServer<IService, MockService>("rest", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithoutAuthorization();
            }, autoStart: true);

            using var provider = services.BuildServiceProvider();
            var hosted = provider.GetRequiredService<IHostedService>();

            Assert.That(hosted, Is.InstanceOf<WitServerRestHostedService>());

            await hosted.StartAsync(CancellationToken.None);
            try
            {
                var (status, body) = await PostAsync(url, "RequestData", "{\"message\":\"hosted\"}");

                Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("hosted"));
            }
            finally
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }


        [Test]
        public async Task CompositeRegistrationAnswersEveryContractTest()
        {
            var url = NextUrl();

            var services = new ServiceCollection();
            services.AddWitRpcRestServerWithServices("rest",
                ctx =>
                {
                    ctx.WithUrl(url);
                    ctx.WithoutAuthorization();
                },
                composite =>
                {
                    composite.AddService<IService>(_ => new MockService());
                    composite.AddService<IEchoService, MockEchoService>();
                });

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("rest");
            server.StartWaitingForConnection();

            var (status1, body1) = await PostAsync(url, "RequestData", "[\"composite\"]");
            var (status2, body2) = await PostAsync(url, "SumNumbers", "{\"a\":2,\"b\":3}");

            Assert.That(status1, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body1), Is.EqualTo("composite"));
            Assert.That(status2, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<int>(body2), Is.EqualTo(5));
        }

        [Test]
        public async Task RequestProcessorResolvedFromContainerTest()
        {
            var url = NextUrl();

            var services = new ServiceCollection();
            services.AddSingleton(new RequestProcessor<IService>(new MockService()));
            services.AddWitRpcRestServer("rest", ctx =>
            {
                ctx.WithUrl(url);
                ctx.WithoutAuthorization();
                ctx.WithRequestProcessor<RequestProcessor<IService>>(typeof(IService));
            });

            using var provider = services.BuildServiceProvider();
            using var server = provider.GetRequiredService<IWitServerRestFactory>().GetServer("rest");
            server.StartWaitingForConnection();

            var (status, body) = await PostAsync(url, "RequestData", "{\"message\":\"from processor\"}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("from processor"));
        }

        #endregion
    }
}

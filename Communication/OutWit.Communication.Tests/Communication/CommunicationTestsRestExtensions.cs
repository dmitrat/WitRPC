using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests._Mock.Interfaces;

namespace OutWit.Communication.Tests.Communication
{
    /// <summary>
    /// The builder surface around REST: several contracts in one host, proxies
    /// straight off the client, token callbacks, a caller-supplied HttpClient
    /// and a logger.
    /// </summary>
    [TestFixture]
    public class CommunicationTestsRestExtensions
    {
        #region Constants

        private const string TOKEN = "rest-ext-token";

        #endregion

        #region Setup

        private static string NextUrl()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return $"http://localhost:{port}/ext/";
        }

        private static WitServerRest StartServer(string url)
        {
            var server = WitServerRestBuilder.Build(options =>
            {
                options.WithUrl(url);
                options.WithAccessToken(TOKEN);
                options.WithService<IService>(new MockService());
            });

            server.StartWaitingForConnection();
            return server;
        }

        private static WitClientRest BuildClient(string url, Action<WitClientRestBuilderOptions>? extra = null)
        {
            return WitClientRestBuilder.Build(options =>
            {
                options.WithUrl(url);
                options.WithAccessToken(TOKEN);
                options.WithTimeout(TimeSpan.FromSeconds(10));
                extra?.Invoke(options);
            });
        }

        #endregion

        #region Composite Tests

        [Test]
        public void CompositeHostAnswersEveryContractTest()
        {
            var url = NextUrl();

            using var server = WitServerRestBuilder.Build(options =>
            {
                options.WithUrl(url);
                options.WithAccessToken(TOKEN);
                options.WithServices()
                    .AddService<IService>(new MockService())
                    .AddService<IEchoService, MockEchoService>(new MockEchoService())
                    .Build();
            });
            server.StartWaitingForConnection();

            var client = BuildClient(url);
            var service = client.GetService<IService>();
            var echo = client.GetService<IEchoService>();

            Assert.That(service.RequestData("one"), Is.EqualTo("one"));
            Assert.That(echo.EchoText("two"), Is.EqualTo("echo: two"));
            Assert.That(echo.SumNumbers(2, 3), Is.EqualTo(5));
        }

        [Test]
        public void CompositeHostRefusesToBuildWithoutServicesTest()
        {
            Assert.That(() => WitServerRestBuilder.Build(options =>
            {
                options.WithUrl(NextUrl());
                options.WithServices().Build();
            }), Throws.Exception);
        }

        #endregion

        #region Proxy Tests

        [Test]
        public void StaticProxyComesStraightOffTheClientTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            var client = BuildClient(url);
            IServiceBase service = client.GetService<IServiceBase>(interceptor => new ServiceProxy(interceptor));

            Assert.That(service.RequestData("static"), Is.EqualTo("static"));
        }

        [Test]
        public void DynamicProxyComesStraightOffTheClientTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            var client = BuildClient(url);
            var service = client.GetService<IService>();

            Assert.That(service.RequestData("dynamic"), Is.EqualTo("dynamic"));
            Assert.That(service.RequestWithMultipleNullableParams(null, 7, null), Is.EqualTo(new MockService().RequestWithMultipleNullableParams(null, 7, null)));
        }

        #endregion

        #region Token Tests

        [Test]
        public void TokenCallbackIsAskedOnEveryCallTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            int asked = 0;
            var client = BuildClient(url, options => options.WithAccessToken(() =>
            {
                asked++;
                return TOKEN;
            }));
            var service = client.GetService<IService>();

            Assert.That(service.RequestData("a"), Is.EqualTo("a"));
            Assert.That(service.RequestData("b"), Is.EqualTo("b"));
            Assert.That(asked, Is.EqualTo(2));
        }

        [Test]
        public void AsyncTokenCallbackIsUsedTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            var client = BuildClient(url, options => options.WithAccessToken(async () =>
            {
                await Task.Yield();
                return TOKEN;
            }));
            var service = client.GetService<IService>();

            Assert.That(service.RequestData("async"), Is.EqualTo("async"));
        }

        [Test]
        public void WrongTokenFromCallbackIsAFaultTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            var client = BuildClient(url, options => options.WithAccessToken(() => "wrong"));
            var service = client.GetService<IService>();

            Assert.That(() => service.RequestData("nope"), Throws.Exception.With.Message.Contains("Token"));
        }

        #endregion

        #region Http Tests

        [Test]
        public void CallerSuppliedHandlerSeesEveryRequestTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            var handler = new MockHttpHandlerCounting(new HttpClientHandler());
            var client = BuildClient(url, options => options.WithHttpMessageHandler(handler));
            var service = client.GetService<IService>();

            Assert.That(service.RequestData("x"), Is.EqualTo("x"));
            Assert.That(service.RequestData("y"), Is.EqualTo("y"));
            Assert.That(handler.Requests, Is.EqualTo(2));
        }

        [Test]
        public void CallerSuppliedHttpClientIsUsedTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            var handler = new MockHttpHandlerCounting(new HttpClientHandler());
            using var http = new HttpClient(handler);
            var client = BuildClient(url, options => options.WithHttpClient(http));
            var service = client.GetService<IService>();

            Assert.That(service.RequestData("own client"), Is.EqualTo("own client"));
            Assert.That(handler.Requests, Is.EqualTo(1));
        }

        [Test]
        public void HttpClientOwnTimeoutIsReportedAsATimeoutTest()
        {
            var url = NextUrl();
            using var server = StartServer(url);

            using var http = new HttpClient(new MockHttpHandlerDelaying(TimeSpan.FromSeconds(5), new HttpClientHandler())) { Timeout = TimeSpan.FromMilliseconds(100) };
            var client = BuildClient(url, options => options.WithHttpClient(http));
            var service = client.GetService<IService>();

            Assert.That(() => service.RequestData("slow"), Throws.Exception.With.Message.Contains("timed out"));
        }

        #endregion

        #region Logger Tests

        [Test]
        public void TransportFailureIsLoggedTest()
        {
            var logger = new MockLoggerCapturing();

            // Nobody listens on this port.
            var client = BuildClient(NextUrl(), options => options.WithLogger(logger));
            var service = client.GetService<IService>();

            Assert.That(() => service.RequestData("nobody"), Throws.Exception);
            Assert.That(logger.Entries, Has.Some.Contains("RequestData"));
            Assert.That(client.Logger, Is.SameAs(logger));
        }

        #endregion
    }
}

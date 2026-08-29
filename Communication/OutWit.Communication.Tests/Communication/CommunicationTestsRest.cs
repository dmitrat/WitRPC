using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using NUnit.Framework;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Model;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock;

namespace OutWit.Communication.Tests.Communication
{
    /// <summary>
    /// The REST transport, rebuilt in 3.0: the whole request is a JSON body run
    /// through the same request processor as every other transport, so methods
    /// with parameters, async results, void, nullable parameters and faults all
    /// behave the same over HTTP.
    /// </summary>
    [TestFixture]
    public class CommunicationTestsRest
    {
        #region Constants

        private const string TOKEN = "rest-token";

        #endregion

        #region Setup

        // Each test binds its own port so the listeners never collide. The OS
        // assigns it, so it can never land in a Windows excluded-port range.
        private static string NextUrl()
        {
            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return $"http://localhost:{port}/rest/";
        }

        private static WitServerRest StartServer(string url, bool auth, RestClientRequestModes _ = RestClientRequestModes.PostOnly)
        {
            var server = WitServerRestBuilder.Build(options =>
            {
                options.WithUrl(url);
                options.WithService<IService>(new MockService());

                if (auth)
                    options.WithAccessToken(TOKEN);
                else
                    options.WithoutAuthorization();
            });

            server.StartWaitingForConnection();
            return server;
        }

        private static IService Proxy(string url, string? token, RestClientRequestModes mode = RestClientRequestModes.PostOnly, TimeSpan? timeout = null)
        {
            var client = new WitClientRest(
                new RestClientTransportOptions { Host = (HostInfo)url, Mode = mode, Timeout = timeout },
                token == null ? new AccessTokenProviderPlain() : new AccessTokenProviderStatic(token));

            return new ProxyGenerator().CreateInterfaceProxyWithoutTarget<IService>(new RequestInterceptorDynamic(client, false));
        }

        #endregion

        #region Tests

        [Test]
        public void SyncRequestReturnsResultTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            Assert.That(service.RequestData("hello"), Is.EqualTo("hello"));
        }

        [Test]
        public async Task AsyncRequestReturnsResultTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            Assert.That(await service.RequestDataAsync("world"), Is.EqualTo("world"));
        }

        [Test]
        public void VoidMethodReturnsWithoutErrorTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            Assert.That(() => service.ReportError("boom"), Throws.Nothing);
        }

        [Test]
        public async Task VoidAsyncMethodCompletesTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            await service.ReportErrorAsync("boom");
            Assert.Pass();
        }

        [Test]
        public void NullParameterIsHandledTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            Assert.That(service.RequestDataNullable(null), Is.EqualTo("nullable"));
        }

        [Test]
        public void NullResultIsHandledTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            Assert.That(service.RequestWithNullableResult(null), Is.Null);
        }

        [Test]
        public void MultipleNullableParametersTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, TOKEN);

            Assert.That(service.RequestWithMultipleNullableParams("a", null, null), Is.EqualTo("a|null|null"));
        }

        [Test]
        public void WithoutAuthorizationWorksTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var service = Proxy(url, token: null);

            Assert.That(service.RequestData("open"), Is.EqualTo("open"));
        }

        [Test]
        public void WrongTokenIsRejectedTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var service = Proxy(url, token: "wrong-token");

            // The server returns Unauthorized, which the proxy surfaces as a fault.
            Assert.That(() => service.RequestData("hello"), Throws.TypeOf<WitExceptionFault>());
        }

        [Test]
        public void GetForParameterlessMethodTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            // A parameterless call (a property getter) over GET when the mode allows it.
            var service = Proxy(url, TOKEN, RestClientRequestModes.AllowGetForMethodsWithoutParameters);

            Assert.That(service.StringProperty, Is.EqualTo("TestString"));
        }

        #endregion
    }
}

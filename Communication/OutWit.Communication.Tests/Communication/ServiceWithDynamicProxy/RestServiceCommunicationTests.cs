using OutWit.Communication.Converters;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Client.Authorization;
using System.Runtime.CompilerServices;
using OutWit.Communication.Tests.Mock.Interfaces;
using Castle.DynamicProxy;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Tests.Mock.Model;
using OutWit.Communication.Model;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Server.Rest.Processors;

namespace OutWit.Communication.Tests.Communication.ServiceWithDynamicProxy
{
    [TestFixture]
    public class RestServiceCommunicationTests
    {
        private const string AUTHORIZATION_TOKEN = "token";

        [Test]
        public async Task SimpleRequestsSingleClientTest()
        {
            var server = GetServer();
            server.StartWaitingForConnection();

            var client = GetClient();

            var service = GetService(client);

            Assert.That(service.RequestData("text"), Is.EqualTo("text"));
            Assert.That(service.StartProcessing(new ComplexNumber<int, int>(12, 34), 1).Is(new ComplexNumber<int, int>(24, 68)), Is.True);
            Assert.Throws<AggregateException>(()=>service.GenericComplex(12, "34", new ComplexNumber<int, double>(56, 6.7)));
        }

        private IService GetService(WitComClientRest client)
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(client, true, true);

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<IService>(interceptor);
        }


        private WitComServerRest GetServer([CallerMemberName] string callerMember = "")
        {

            var service = new MockService();
            var options = new RestServerTransportOptions
            {
                Url = $"http://localhost:5000/{callerMember}/",
            };
            return new WitComServerRest(options,
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                new RequestProcessorRest<IService>(service), null, null);
        }

        private WitComClientRest GetClient([CallerMemberName] string callerMember = "")
        {
            var options = new RestClientTransportOptions
            {
                Host = (HostInfo?) $"http://localhost:5000/{callerMember}/",
                Mode = RestClientRequestModes.AllowGet
            };

            return new WitComClientRest(options,
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN));
        }
    }
}

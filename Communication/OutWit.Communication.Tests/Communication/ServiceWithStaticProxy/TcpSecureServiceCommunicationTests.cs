using OutWit.Communication.Converters;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Server;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using System.Runtime.CompilerServices;
using OutWit.Communication.Processors;
using OutWit.Communication.Tests.Mock.Interfaces;
using Castle.DynamicProxy;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Tests.Mock.Model;
using OutWit.Communication.Client.Tcp;
using OutWit.Communication.Server.Tcp;
using OutWit.Common.Aspects.Utils;
using System.Security.Cryptography.X509Certificates;
using OutWit.Communication.Tests._Mock.Interfaces;

namespace OutWit.Communication.Tests.Communication.ServiceWithStaticProxy
{
    [TestFixture]
    public class TcpSecureServiceCommunicationTests
    {
        private const string PIPE_NAME = "TestPipe";
        private const string AUTHORIZATION_TOKEN = "token";

        [Test]
        public async Task SimpleRequestsSingleClientTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            Assert.That(service.StringProperty, Is.EqualTo("TestString"));
            Assert.That(service.DoubleProperty, Is.EqualTo(1.2));

            Assert.That(service.RequestData("text"), Is.EqualTo("text"));
        }

        [Test]
        public async Task SimpleRequestsSingleClientAsyncTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            Assert.That(await service.RequestDataAsync("text"), Is.EqualTo("text"));
        }

        [Test]
        public async Task PropertyChangedCallbackTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            int callbackCount = 0;
            service.PropertyChanged += (s, e) =>
            {
                if (e.IsProperty((IService ser) => ser.DoubleProperty))
                    callbackCount++;
            };

            Assert.That(service.DoubleProperty, Is.EqualTo(1.2));

            service.DoubleProperty = 3.4;

            Thread.Sleep(200);
            Assert.That(service.DoubleProperty, Is.EqualTo(3.4));
            Assert.That(callbackCount, Is.EqualTo(1));

        }

        [Test]
        public async Task SingleSubscribeSingleClientSimpleCallbackTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            int callbackCount = 0;
            string actual = "";
            service.Error += text =>
            {
                callbackCount++;
                actual = text;
                Console.WriteLine(text);
            };

            service.ReportError("text1");
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actual, Is.EqualTo("text1"));

            service.ReportError("text2");
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actual, Is.EqualTo("text2"));
        }

        [Test]
        public async Task SingleSubscribeSingleClientSimpleCallbackAsyncTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            int callbackCount = 0;
            string actual = "";
            service.Error += text =>
            {
                callbackCount++;
                actual = text;
                Console.WriteLine(text);
            };

            await service.ReportErrorAsync("text1");
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actual, Is.EqualTo("text1"));

            await service.ReportErrorAsync("text2");
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actual, Is.EqualTo("text2"));
        }

        [Test]
        public async Task SingleSubscribeComplexTypeSingleClientCallbackTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);
            int callbackCount = 0;
            ComplexNumber<int, int>? actualNum = null;
            int actualIter = 0;
            service.StartProcessingRequested += (num, iter) =>
            {
                callbackCount++;
                actualNum = num;
                actualIter = iter;
                Console.WriteLine(num);
            };

            service.StartProcessing(new ComplexNumber<int, int>(1, 2), 3);
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo(2));
            Assert.That(actualIter, Is.EqualTo(3));

            service.StartProcessing(new ComplexNumber<int, int>(4, 5), 6);
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actualNum!.A, Is.EqualTo(4));
            Assert.That(actualNum!.B, Is.EqualTo(5));
            Assert.That(actualIter, Is.EqualTo(6));
        }

        [Test]
        public async Task SingleSubscribeComplexTypeSingleClientCallbackAsyncTest()
        {
            var server = GetServer(1);
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);
            int callbackCount = 0;
            ComplexNumber<int, int>? actualNum = null;
            int actualIter = 0;
            service.StartProcessingRequested += (num, iter) =>
            {
                callbackCount++;
                actualNum = num;
                actualIter = iter;
                Console.WriteLine(num);
            };

            await service.StartProcessingAsync(new ComplexNumber<int, int>(1, 2), 3);
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo(2));
            Assert.That(actualIter, Is.EqualTo(3));

            await service.StartProcessingAsync(new ComplexNumber<int, int>(4, 5), 6);
            Thread.Sleep(200);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actualNum!.A, Is.EqualTo(4));
            Assert.That(actualNum!.B, Is.EqualTo(5));
            Assert.That(actualIter, Is.EqualTo(6));
        }

        [Test]
        public async Task MultiSubscribeMultiClientsCallbackTest()
        {
            var server = GetServer(5);
            server.StartWaitingForConnection();

            var client1 = GetClient();

            Assert.That(await client1.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var service1 = GetService(client1);


            var client2 = GetClient();

            Assert.That(await client2.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);

            var service2 = GetService(client1);

            int callbackFirstCount = 0;
            int callbackSecondCount = 0;
            string actualFirst = "";
            string actualSecond = "";
            service1.Error += text =>
            {
                callbackFirstCount++;
                actualFirst = text;
                Console.WriteLine(text);
            };

            service1.ReportError("text1");
            Assert.That(callbackFirstCount, Is.EqualTo(1));
            Assert.That(actualFirst, Is.EqualTo("text1"));
            Assert.That(callbackSecondCount, Is.EqualTo(0));
            Assert.That(actualSecond, Is.EqualTo(""));

            service2.Error += text =>
            {
                callbackSecondCount++;
                actualSecond = text;
                Console.WriteLine(text);
            };

            service2.ReportError("text2");
            Thread.Sleep(200);
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(1));
            Assert.That(actualSecond, Is.EqualTo("text2"));

            service1.ReportError("text3");
            Thread.Sleep(200);
            Assert.That(callbackFirstCount, Is.EqualTo(3));
            Assert.That(actualFirst, Is.EqualTo("text3"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));
        }

        [Test]
        public async Task MultiSubscribeMultiClientsCallbackAsyncTest()
        {
            var server = GetServer(5);
            server.StartWaitingForConnection();

            var client1 = GetClient();

            Assert.That(await client1.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client1.IsInitialized, Is.True);
            Assert.That(client1.IsAuthorized, Is.True);

            var service1 = GetService(client1);


            var client2 = GetClient();

            Assert.That(await client2.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client2.IsInitialized, Is.True);
            Assert.That(client2.IsAuthorized, Is.True);

            var service2 = GetService(client1);

            int callbackFirstCount = 0;
            int callbackSecondCount = 0;
            string actualFirst = "";
            string actualSecond = "";
            service1.Error += text =>
            {
                callbackFirstCount++;
                actualFirst = text;
                Console.WriteLine(text);
            };

            await service1.ReportErrorAsync("text1");
            Assert.That(callbackFirstCount, Is.EqualTo(1));
            Assert.That(actualFirst, Is.EqualTo("text1"));
            Assert.That(callbackSecondCount, Is.EqualTo(0));
            Assert.That(actualSecond, Is.EqualTo(""));

            service2.Error += text =>
            {
                callbackSecondCount++;
                actualSecond = text;
                Console.WriteLine(text);
            };

            await service2.ReportErrorAsync("text2");
            Thread.Sleep(200);
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(1));
            Assert.That(actualSecond, Is.EqualTo("text2"));

            await service1.ReportErrorAsync("text3");
            Thread.Sleep(200);
            Assert.That(callbackFirstCount, Is.EqualTo(3));
            Assert.That(actualFirst, Is.EqualTo("text3"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));
        }

        private IServiceBase GetService(WitComClient client)
        {
            var interceptor = new RequestInterceptorDynamic(client, true, true);

            return new ServiceProxy(interceptor);
        }


        private WitComServer GetServer(int maxNumberOfClients, [CallerMemberName] string callerMember = "")
        {

            var service = new MockService();
            var random = new Random(callerMember.GetHashCode());
            var serverTransport = new TcpSecureServerTransportFactory(new TcpSecureServerTransportOptions
            {
                Port = random.Next(100, 300),
                MaxNumberOfClients = maxNumberOfClients,
                Certificate = new X509Certificate(Properties.Resources.certificate1, "Pa$$w0rd")
            });
            return new WitComServer(serverTransport,
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(),
                new ValueConverterJson(),
                new RequestProcessor<IService>(service), null, null);
        }

        private WitComClient GetClient([CallerMemberName] string callerMember = "")
        {
            var random = new Random(callerMember.GetHashCode());
            var clientTransport = new TcpSecureClientTransport(new TcpSecureClientTransportOptions
            {
                Port = random.Next(100, 300),
                Host = "127.0.0.1",
                TargetHost = "localhost",
            });

            return new WitComClient(clientTransport,
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(),
                new ValueConverterJson(), null, null);
        }
    }
}

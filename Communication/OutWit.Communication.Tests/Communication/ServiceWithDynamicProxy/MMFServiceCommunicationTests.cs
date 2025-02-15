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
using OutWit.Communication.Client.MMF;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Server.MMF;
using OutWit.Communication.Tests.Mock.Model;
using OutWit.Common.Aspects.Utils;
using OutWit.Communication.Server.Discovery;
using System.Net;

namespace OutWit.Communication.Tests.Communication.ServiceWithDynamicProxy
{
    [TestFixture]
    public class MMFServiceCommunicationTests
    {
        private const string MEMORY_MAPPED_FILE_NAME = "TestMMF";
        private const string AUTHORIZATION_TOKEN = "token";

        [Test]
        public async Task SimpleRequestsSingleClientTest()
        {
            var server = GetServer();
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            Assert.That(service.StringProperty, Is.EqualTo("TestString"));
            Assert.That(service.DoubleProperty, Is.EqualTo(1.2));

            Assert.That(service.RequestData("text"), Is.EqualTo("text"));
            Assert.That(service.GenericSimple(12, "34", 5.6), Is.EqualTo(5.6));
            Assert.That(service.GenericComplex(12, "34", new ComplexNumber<int, double>(56, 6.7)).Is(new ComplexNumber<int, double>(56, 6.7)), Is.EqualTo(true));
            Assert.That(service.GenericComplexArray(12, "34", new List<ComplexNumber<int, double>>
            {
                new ComplexNumber<int, double>(56, 6.7),
                new ComplexNumber<int, double>(89, 10.11),
                new ComplexNumber<int, double>(123, 14.15),
            }).Is(new ComplexNumber<int, double>(56, 6.7)), Is.EqualTo(true));

            Assert.That(service.GenericComplexMulti(new ComplexNumber<string, string>("aa", "bb"), "34", new List<ComplexNumber<int, double>>
            {
                new ComplexNumber<int, double>(56, 6.7),
                new ComplexNumber<int, double>(89, 10.11),
                new ComplexNumber<int, double>(123, 14.15),
            }).Is(new ComplexNumber<string, int>("bb", 56)), Is.EqualTo(true));
        }


        [Test]
        public async Task SimpleRequestsSingleClientAsyncTest()
        {
            var server = GetServer();
            server.StartWaitingForConnection();

            var client = GetClient();

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);
            Assert.That(client.IsInitialized, Is.True);
            Assert.That(client.IsAuthorized, Is.True);

            var service = GetService(client);

            Assert.That(await service.RequestDataAsync("text"), Is.EqualTo("text"));
            Assert.That(await service.GenericSimpleAsync(12, "34", 5.6), Is.EqualTo(5.6));
            Assert.That((await service.GenericComplexAsync(12, "34", new ComplexNumber<int, double>(56, 6.7))).Is(new ComplexNumber<int, double>(56, 6.7)), Is.EqualTo(true));
            Assert.That((await service.GenericComplexArrayAsync(12, "34", new List<ComplexNumber<int, double>>
            {
                new ComplexNumber<int, double>(56, 6.7),
                new ComplexNumber<int, double>(89, 10.11),
                new ComplexNumber<int, double>(123, 14.15),
            })).Is(new ComplexNumber<int, double>(56, 6.7)), Is.EqualTo(true));

            Assert.That((await service.GenericComplexMultiAsync(new ComplexNumber<string, string>("aa", "bb"), "34", new List<ComplexNumber<int, double>>
            {
                new ComplexNumber<int, double>(56, 6.7),
                new ComplexNumber<int, double>(89, 10.11),
                new ComplexNumber<int, double>(123, 14.15),
            })).Is(new ComplexNumber<string, int>("bb", 56)), Is.EqualTo(true));
        }


        [Test]
        public async Task PropertyChangedCallbackTest()
        {
            var server = GetServer();
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
            var server = GetServer();
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
            var server = GetServer();
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
            var server = GetServer();
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
            var server = GetServer();
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


        private IService GetService(WitComClient client)
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(client, true);

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<IService>(interceptor);
        }

        private WitComServer GetServer([CallerMemberName] string pipeName = MEMORY_MAPPED_FILE_NAME)
        {
            var service = new MockService();

            var serverTransport = new MemoryMappedFileServerTransportFactory(new MemoryMappedFileServerTransportOptions()
            {
                Name = pipeName,
                Size = 1024 * 1024
            });
            return new WitComServer(serverTransport,
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(),
                new ValueConverterJson(),
                new RequestProcessor<IService>(service),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null);
        }

        private WitComClient GetClient([CallerMemberName] string pipeName = MEMORY_MAPPED_FILE_NAME)
        {
            var clientTransport = new MemoryMappedFileClientTransport(new MemoryMappedFileClientTransportOptions
            {
                Name = pipeName
            });

            return new WitComClient(clientTransport,
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                new MessageSerializerJson(),
                new ValueConverterJson(), null, null);
        }

    }
}

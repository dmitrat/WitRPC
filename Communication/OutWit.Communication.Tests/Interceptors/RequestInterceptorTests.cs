using System;
using Castle.DynamicProxy;
using OutWit.Common.Aspects.Utils;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Tests._Mock.Interfaces;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Interceptors
{
    [TestFixture]
    public class RequestInterceptorTests
    {
        [Test]
        public void SimpleRequestsTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);

            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            Assert.That(serviceProxy.StringProperty, Is.EqualTo("TestString"));
            Assert.That(serviceProxy.DoubleProperty, Is.EqualTo(1.2));

            Assert.That(serviceProxy.RequestData("text"), Is.EqualTo("text"));
        }

        [Test]
        public async Task SimpleRequestsAsyncTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);

            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            var result = await serviceProxy.RequestDataAsync("text");

            Assert.That(result, Is.EqualTo("text"));
        }

        [Test]
        public void PropertyChangedCallbackTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            serviceProxy.PropertyChanged += (s,e) =>
            {
                if(e.IsProperty((IService ser)=>ser.DoubleProperty))
                    callbackCount++;
            };

            Assert.That(serviceProxy.DoubleProperty, Is.EqualTo(1.2));

            serviceProxy.DoubleProperty = 3.4;
            Assert.That(serviceProxy.DoubleProperty, Is.EqualTo(3.4));
            Assert.That(callbackCount, Is.EqualTo(1));

        }

        [Test]
        public void SingleSubscribeSimpleCallbackTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            string actual = "";
            serviceProxy.Error += text =>
            {
                callbackCount++;
                actual = text;
                Console.WriteLine(text);
            };

            serviceProxy.ReportError("text1");
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actual, Is.EqualTo("text1"));

            serviceProxy.ReportError("text2");
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actual, Is.EqualTo("text2"));
        }

        [Test]
        public async Task SingleSubscribeSimpleCallbackAsyncTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            string actual = "";
            serviceProxy.Error += text =>
            {
                callbackCount++;
                actual = text;
                Console.WriteLine(text);
            };

            await serviceProxy.ReportErrorAsync("text1");
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actual, Is.EqualTo("text1"));

            await serviceProxy.ReportErrorAsync("text2");
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actual, Is.EqualTo("text2"));
        }

        [Test]
        public void SingleSubscribeComplexTypeCallbackTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            ComplexNumber<int, int>? actualNum = null;
            int actualIter = 0;
            serviceProxy.StartProcessingRequested += (num, iter) =>
            {
                callbackCount++;
                actualNum = num;
                actualIter = iter;
                Console.WriteLine(num);
            };

            serviceProxy.StartProcessing(new ComplexNumber<int, int>(1, 2), 3);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo(2));
            Assert.That(actualIter, Is.EqualTo(3));

            serviceProxy.StartProcessing(new ComplexNumber<int, int>(4, 5), 6);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actualNum!.A, Is.EqualTo(4));
            Assert.That(actualNum!.B, Is.EqualTo(5));
            Assert.That(actualIter, Is.EqualTo(6));
        }


        [Test]
        public async Task SingleSubscribeComplexTypeCallbackAsyncTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            ComplexNumber<int, int>? actualNum = null;
            int actualIter = 0;
            serviceProxy.StartProcessingRequested += (num, iter) =>
            {
                callbackCount++;
                actualNum = num;
                actualIter = iter;
                Console.WriteLine(num);
            };

            await serviceProxy.StartProcessingAsync(new ComplexNumber<int, int>(1, 2), 3);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo(2));
            Assert.That(actualIter, Is.EqualTo(3));

            await serviceProxy.StartProcessingAsync(new ComplexNumber<int, int>(4, 5), 6);
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actualNum!.A, Is.EqualTo(4));
            Assert.That(actualNum!.B, Is.EqualTo(5));
            Assert.That(actualIter, Is.EqualTo(6));
        }

        [Test]
        public void SingleSubscribeEventWithSenderCallbackTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            object? actualSender = null;
            ComplexNumber<int, string>? actualNum = null;
            serviceProxy.GeneralEvent += (sender, num) =>
            {
                callbackCount++;
                actualSender = sender;
                actualNum = num;
                Console.WriteLine(num);
            };

            serviceProxy.RequestData("2");
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo("2"));
            Assert.That(actualSender, Is.Null);

            serviceProxy.RequestData("3");
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo("3"));
            Assert.That(actualSender, Is.Null);
        }


        [Test]
        public async Task SingleSubscribeEventWithSenderCallbackAsyncTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackCount = 0;
            object? actualSender = null;
            ComplexNumber<int, string>? actualNum = null;
            serviceProxy.GeneralEvent += (sender, num) =>
            {
                callbackCount++;
                actualSender = sender;
                actualNum = num;
                Console.WriteLine(num);
            };

            await serviceProxy.RequestDataAsync("2");
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo("2"));
            Assert.That(actualSender, Is.Null);

            await serviceProxy.RequestDataAsync("3");
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(actualNum!.A, Is.EqualTo(1));
            Assert.That(actualNum!.B, Is.EqualTo("3"));
            Assert.That(actualSender, Is.Null);
        }

        [Test]
        public void MultiSubscribeCallbackTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackFirstCount = 0;
            int callbackSecondCount = 0;
            string actualFirst = "";
            string actualSecond = "";
            serviceProxy.Error += text =>
            {
                callbackFirstCount++;
                actualFirst = text;
                Console.WriteLine(text);
            };

            serviceProxy.ReportError("text1");
            Assert.That(callbackFirstCount, Is.EqualTo(1));
            Assert.That(actualFirst, Is.EqualTo("text1"));
            Assert.That(callbackSecondCount, Is.EqualTo(0));
            Assert.That(actualSecond, Is.EqualTo(""));

            serviceProxy.Error += text =>
            {
                callbackSecondCount++;
                actualSecond = text;
                Console.WriteLine(text);
            };

            serviceProxy.ReportError("text2");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(1));
            Assert.That(actualSecond, Is.EqualTo("text2"));

            serviceProxy.ReportError("text3");
            Assert.That(callbackFirstCount, Is.EqualTo(3));
            Assert.That(actualFirst, Is.EqualTo("text3"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));
        }

        [Test]
        public async Task MultiSubscribeCallbackAsyncTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackFirstCount = 0;
            int callbackSecondCount = 0;
            string actualFirst = "";
            string actualSecond = "";
            serviceProxy.Error += text =>
            {
                callbackFirstCount++;
                actualFirst = text;
                Console.WriteLine(text);
            };

            await serviceProxy.ReportErrorAsync("text1");
            Assert.That(callbackFirstCount, Is.EqualTo(1));
            Assert.That(actualFirst, Is.EqualTo("text1"));
            Assert.That(callbackSecondCount, Is.EqualTo(0));
            Assert.That(actualSecond, Is.EqualTo(""));

            serviceProxy.Error += text =>
            {
                callbackSecondCount++;
                actualSecond = text;
                Console.WriteLine(text);
            };

            await serviceProxy.ReportErrorAsync("text2");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(1));
            Assert.That(actualSecond, Is.EqualTo("text2"));

            await serviceProxy.ReportErrorAsync("text3");
            Assert.That(callbackFirstCount, Is.EqualTo(3));
            Assert.That(actualFirst, Is.EqualTo("text3"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));
        }

        [Test]
        public void UnsubscribeCallbackTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackFirstCount = 0;
            int callbackSecondCount = 0;
            string actualFirst = "";
            string actualSecond = "";

            void Handler1(string text)
            {
                callbackFirstCount++;
                actualFirst = text;
                Console.WriteLine(text);
            }

            serviceProxy.Error += Handler1;

            serviceProxy.ReportError("text1");
            Assert.That(callbackFirstCount, Is.EqualTo(1));
            Assert.That(actualFirst, Is.EqualTo("text1"));
            Assert.That(callbackSecondCount, Is.EqualTo(0));
            Assert.That(actualSecond, Is.EqualTo(""));


            void Handler2(string text)
            {
                callbackSecondCount++;
                actualSecond = text;
                Console.WriteLine(text);
            }

            serviceProxy.Error += Handler2;

            serviceProxy.ReportError("text2");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(1));
            Assert.That(actualSecond, Is.EqualTo("text2"));

            serviceProxy.Error -= Handler1;
            serviceProxy.ReportError("text3");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));

            serviceProxy.Error -= Handler2;
            serviceProxy.ReportError("text4");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));
        }

        [Test]
        public async Task UnsubscribeCallbackAsyncTest()
        {
            var service = new MockService();
            var client = new MockClient<IService>(service);

            var interceptor = new RequestInterceptor(client, true, true);
            IServiceBase serviceProxy = new ServiceProxy(interceptor);

            int callbackFirstCount = 0;
            int callbackSecondCount = 0;
            string actualFirst = "";
            string actualSecond = "";

            void Handler1(string text)
            {
                callbackFirstCount++;
                actualFirst = text;
                Console.WriteLine(text);
            }

            serviceProxy.Error += Handler1;

            await serviceProxy.ReportErrorAsync("text1");
            Assert.That(callbackFirstCount, Is.EqualTo(1));
            Assert.That(actualFirst, Is.EqualTo("text1"));
            Assert.That(callbackSecondCount, Is.EqualTo(0));
            Assert.That(actualSecond, Is.EqualTo(""));


            void Handler2(string text)
            {
                callbackSecondCount++;
                actualSecond = text;
                Console.WriteLine(text);
            }

            serviceProxy.Error += Handler2;

            await serviceProxy.ReportErrorAsync("text2");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(1));
            Assert.That(actualSecond, Is.EqualTo("text2"));

            serviceProxy.Error -= Handler1;
            await serviceProxy.ReportErrorAsync("text3");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));

            serviceProxy.Error -= Handler2;
            await serviceProxy.ReportErrorAsync("text4");
            Assert.That(callbackFirstCount, Is.EqualTo(2));
            Assert.That(actualFirst, Is.EqualTo("text2"));
            Assert.That(callbackSecondCount, Is.EqualTo(2));
            Assert.That(actualSecond, Is.EqualTo("text3"));
        }
    }
}

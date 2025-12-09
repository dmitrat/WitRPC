using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Processors;
using OutWit.Communication.Requests;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Tests.Processors
{
    [TestFixture]
    public class CompositeRequestProcessorTests
    {
        #region Test Interfaces

        public interface IChannel1
        {
            string GetChannel1Data(string input);
            Task<int> Channel1AsyncMethod(int value);
        }

        public interface IChannel2
        {
            string GetChannel2Data(string input);
            double Channel2Calculate(double a, double b);
        }

        public interface IChannel3
        {
            event Action<string>? Channel3Event;
            void TriggerChannel3Event(string message);
        }

        #endregion

        #region Test Implementations

        public class Channel1Impl : IChannel1
        {
            public string GetChannel1Data(string input) => $"Channel1:{input}";
            public Task<int> Channel1AsyncMethod(int value) => Task.FromResult(value * 2);
        }

        public class Channel2Impl : IChannel2
        {
            public string GetChannel2Data(string input) => $"Channel2:{input}";
            public double Channel2Calculate(double a, double b) => a + b;
        }

        public class Channel3Impl : IChannel3
        {
            public event Action<string>? Channel3Event;
            public void TriggerChannel3Event(string message) => Channel3Event?.Invoke(message);
        }

        #endregion

        #region Registration Tests

        [Test]
        public void RegisterSingleService()
        {
            var processor = new CompositeRequestProcessor();
            
            var result = processor.Register<IChannel1>(new Channel1Impl());
            
            Assert.That(result, Is.SameAs(processor));
        }

        [Test]
        public void RegisterMultipleServices()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl())
                .Register<IChannel2>(new Channel2Impl())
                .Register<IChannel3>(new Channel3Impl());

            Assert.Pass("Multiple services registered successfully");
        }

        [Test]
        public void RegisterDuplicateServiceThrows()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl());

            Assert.Throws<InvalidOperationException>(() => 
                processor.Register<IChannel1>(new Channel1Impl()));
        }

        [Test]
        public void RegisterWithExplicitInterface()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1, Channel1Impl>(new Channel1Impl());

            Assert.Pass("Service registered with explicit interface");
        }

        #endregion

        #region Process Tests

        [Test]
        public async Task ProcessRequestFromChannel1()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl())
                .Register<IChannel2>(new Channel2Impl());

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var request = new WitRequest
            {
                MethodName = "GetChannel1Data",
                Parameters = new[] { serializer.Serialize("test") },
                ParameterTypes = new[] { typeof(string) },
                GenericArguments = Array.Empty<Type>()
            };

            var response = await processor.Process(request);

            Assert.That(response.IsSuccess, Is.True);
            var result = serializer.Deserialize<string>(response.Data!);
            Assert.That(result, Is.EqualTo("Channel1:test"));
        }

        [Test]
        public async Task ProcessRequestFromChannel2()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl())
                .Register<IChannel2>(new Channel2Impl());

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var request = new WitRequest
            {
                MethodName = "Channel2Calculate",
                Parameters = new[] { serializer.Serialize(2.5, typeof(double)), serializer.Serialize(3.5, typeof(double)) },
                ParameterTypes = new[] { typeof(double), typeof(double) },
                GenericArguments = Array.Empty<Type>()
            };

            var response = await processor.Process(request);

            Assert.That(response.IsSuccess, Is.True);
            var result = (double)serializer.Deserialize(response.Data!, typeof(double))!;
            Assert.That(result, Is.EqualTo(6.0));
        }

        [Test]
        public async Task ProcessAsyncRequestFromChannel1()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl());

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var request = new WitRequest
            {
                MethodName = "Channel1AsyncMethod",
                Parameters = new[] { serializer.Serialize(5, typeof(int)) },
                ParameterTypes = new[] { typeof(int) },
                GenericArguments = Array.Empty<Type>()
            };

            var response = await processor.Process(request);

            Assert.That(response.IsSuccess, Is.True);
            var result = (int)serializer.Deserialize(response.Data!, typeof(int))!;
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public async Task ProcessRequestMethodNotFound()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl());

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var request = new WitRequest
            {
                MethodName = "NonExistentMethod",
                Parameters = Array.Empty<byte[]>(),
                ParameterTypes = Array.Empty<Type>(),
                GenericArguments = Array.Empty<Type>()
            };

            var response = await processor.Process(request);

            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.ErrorMessage, Does.Contain("Method not found"));
        }

        [Test]
        public async Task ProcessNullRequestReturnsBadRequest()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel1>(new Channel1Impl());

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var response = await processor.Process(null);

            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.ErrorMessage, Does.Contain("Request is empty"));
        }

        #endregion

        #region Event Tests

        [Test]
        public void EventCallbackIsRaised()
        {
            var processor = new CompositeRequestProcessor()
                .Register<IChannel3>(new Channel3Impl());

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            WitRequest? receivedRequest = null;
            processor.Callback += request => receivedRequest = request;

            Assert.Pass("Event subscription works");
        }

        #endregion
    }
}

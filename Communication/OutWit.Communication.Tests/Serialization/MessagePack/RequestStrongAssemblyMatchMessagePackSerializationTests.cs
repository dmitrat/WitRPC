using OutWit.Communication.Converters;
using OutWit.Communication.Serializers;
using OutWit.Common.Collections;
using OutWit.Communication.Requests;
using OutWit.Communication.Tests.Mock.Model;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Serialization.MessagePack
{
    [TestFixture]
    public class RequestStrongAssemblyMatchMessagePackSerializationTests
    {
        [Test]
        public void SimpleRequestSerializationTest()
        {
            var serializer = new MessageSerializerMessagePack();
            var converter = new ValueConverterMessagePack();

            var request1 = new WitComRequest
            {
                MethodName = "TestMethod",
                Parameters = new object[]
                {
                    (double)1.0,
                    (int)2,
                    "3"
                },
                ParameterTypes = new[]
                {
                    typeof(double),
                    typeof(int),
                    typeof(string)
                },
                GenericArguments = Array.Empty<Type>()
            };

            var bytes = serializer.Serialize(request1);
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes, Is.Not.Empty);

            var request2 = bytes.GetRequest(serializer, converter);
            Assert.That(request2, Is.Not.Null);
            Assert.That(request2, Is.Not.SameAs(request1));
            Assert.That(request2.Is(request1));
        }

        [Test]
        public void BytesRequestSerializationTest()
        {
            var serializer = new MessageSerializerMessagePack();
            var converter = new ValueConverterMessagePack();

            var request1 = new WitComRequest
            {
                MethodName = "TestMethod",
                Parameters = new object[]
                {
                    new byte[] { 1, 2, 3, 4, 5 },
                },
                ParameterTypes = new[]
                {
                    typeof(byte[])
                },
                GenericArguments = Array.Empty<Type>()
            };

            var bytes = serializer.Serialize(request1);
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes, Is.Not.Empty);

            var request2 = bytes.GetRequest(serializer, converter);
            Assert.That(request2, Is.Not.Null);
            Assert.That(request2, Is.Not.SameAs(request1));
            Assert.That(((byte[])request2.Parameters[0]).Is((byte[])request1.Parameters[0]));
        }

        [Test]
        public void SimpleRequestComplexTypeSerializationTest()
        {
            var serializer = new MessageSerializerMessagePack();
            var converter = new ValueConverterMessagePack();

            var request1 = new WitComRequest
            {
                MethodName = "TestMethod",
                Parameters = new object[]
                {
                    (double)1.0,
                    (int)2,
                    new ComplexType
                    {
                        Int32Value = 3,
                        StringValue = "4"
                    }
                },
                ParameterTypes = new[]
                {
                    typeof(double),
                    typeof(int),
                    typeof(ComplexType)
                },
                GenericArguments = Array.Empty<Type>()
            };

            var bytes = serializer.Serialize(request1);
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes, Is.Not.Empty);

            var request2 = bytes.GetRequest(serializer, converter);
            Assert.That(request2, Is.Not.Null);
            Assert.That(request2, Is.Not.SameAs(request1));
            Assert.That(request2.Is(request1));
        }

        [Test]
        public void ComplexRequestSerializationTest()
        {
            var serializer = new MessageSerializerMessagePack();
            var converter = new ValueConverterMessagePack();

            var request1 = new WitComRequest
            {
                MethodName = "TestMethod",
                Parameters = new object[]
                {
                    (double)1.0,
                    (int)2,
                    new ComplexNumber<int, double>(3, 4.0)
                },
                ParameterTypes = new[]
                {
                    typeof(double),
                    typeof(int),
                    typeof(ComplexNumber<int, double>)
                },
                GenericArguments = new[]
                {
                    typeof(int),
                    typeof(double)
                }
            };

            var bytes = serializer.Serialize(request1);
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes, Is.Not.Empty);

            var request2 = bytes.GetRequest(serializer, converter);
            Assert.That(request2, Is.Not.Null);
            Assert.That(request2, Is.Not.SameAs(request1));
            Assert.That(request2.Is(request1));
        }

        [Test]
        public void ComplexRequestArraySerializationTest()
        {
            var serializer = new MessageSerializerMessagePack();
            var converter = new ValueConverterMessagePack();

            var request1 = new WitComRequest
            {
                MethodName = "TestMethod",
                Parameters = new object[]
                {
                    (double)1.0,
                    (int)2,
                    new List<ComplexNumber<int, double>>
                    {
                        new ComplexNumber<int, double>(3, 4.0),
                        new ComplexNumber<int, double>(5, 6.0),
                        new ComplexNumber<int, double>(7, 8.0),
                    }
                },
                ParameterTypes = new[]
                {
                    typeof(double),
                    typeof(int),
                    typeof(List<ComplexNumber<int, double>>)
                },
                GenericArguments = new[]
                {
                    typeof(int),
                    typeof(double)
                }
            };

            var bytes = serializer.Serialize(request1);
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes, Is.Not.Empty);

            var request2 = bytes.GetRequest(serializer, converter);
            Assert.That(request2, Is.Not.Null);
            Assert.That(request2, Is.Not.SameAs(request1));
            Assert.That(request2.MethodName, Is.EqualTo(request1.MethodName));
            Assert.That(request2.Parameters[0], Is.EqualTo(request1.Parameters[0]));
            Assert.That(request2.Parameters[1], Is.EqualTo(request1.Parameters[1]));
            Assert.That(((List<ComplexNumber<int, double>>)request2.Parameters[2]).Is((List<ComplexNumber<int, double>>)request1.Parameters[2]), Is.EqualTo(true));
            Assert.That(request2.ParameterTypes, Is.EqualTo(request1.ParameterTypes));
            Assert.That(request2.GenericArguments, Is.EqualTo(request1.GenericArguments));
        }
    }
}

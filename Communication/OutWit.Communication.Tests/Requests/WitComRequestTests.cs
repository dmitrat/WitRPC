using System;
using OutWit.Common.Collections;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Communication.Model;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Common.Utils;
using OutWit.Communication.Utils;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Tests.Requests
{
    [TestFixture]
    public class WitRequestTests
    {
        [Test]
        public void ConstructorTest()
        {
            var request = new WitRequest();
            Assert.That(request.Token, Is.Empty);
            Assert.That(request.MethodName, Is.Empty);
            Assert.That(request.Parameters, Is.Empty);
            Assert.That(request.ParameterTypes, Is.Empty);
            Assert.That(request.ParameterTypesByName, Is.Empty);
            Assert.That(request.GenericArguments, Is.Empty);
            Assert.That(request.GenericArgumentsByName, Is.Empty);

            request = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[]{2, 2}, new byte[] {3, 3, 3} },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };

            Assert.That(request.Token, Is.EqualTo("0"));
            Assert.That(request.MethodName, Is.EqualTo("1"));
            Assert.That(request.Parameters.SelectMany(x=>x).Is( new byte[]{2, 2, 3, 3, 3} ), Is.EqualTo(true));
            Assert.That(request.ParameterTypes.Is(typeof(int), typeof(string)), Is.EqualTo(true));
            Assert.That(request.ParameterTypesByName.Is((ParameterType)typeof(int), (ParameterType)typeof(string)), Is.EqualTo(true));
            Assert.That(request.GenericArguments.Is(typeof(double), typeof(string)), Is.EqualTo(true));
            Assert.That(request.GenericArgumentsByName.Is((ParameterType)typeof(double), (ParameterType)typeof(string)), Is.EqualTo(true));
        }

        [Test]
        public void IsTest()
        {
            var request = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[]{2, 2}, new byte[] {3, 3, 3} },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };

            Assert.That(request.Is(request.Clone()), Is.True);

            Assert.That(request.Is(request.With(x => x.Token = "1")), Is.False);
            Assert.That(request.Is(request.With(x => x.MethodName = "2")), Is.False);
            Assert.That(request.Is(request.With(x => x.Parameters = new byte[][] { new byte[] { 2, 2 } })), Is.False);
            Assert.That(request.Is(request.With(x => x.ParameterTypes = new[] { typeof(double), typeof(string) })), Is.False);
            Assert.That(request.Is(request.With(x => x.ParameterTypesByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) })), Is.False);
            Assert.That(request.Is(request.With(x => x.GenericArguments = new[] { typeof(int), typeof(string) })), Is.False);
            Assert.That(request.Is(request.With(x => x.GenericArgumentsByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) })), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var request1 = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[]{2, 2}, new byte[] {3, 3, 3} },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };
            var request2 = request1.Clone() as WitRequest;

            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));

            Assert.That(request2.Token, Is.EqualTo("0"));
            Assert.That(request2.MethodName, Is.EqualTo("1"));
            Assert.That(request2.Parameters.SelectMany(x => x).Is(new byte[] { 2, 2, 3, 3, 3 }), Is.EqualTo(true));
            Assert.That(request2.ParameterTypes.Is(typeof(int), typeof(string)), Is.EqualTo(true));
            Assert.That(request2.ParameterTypesByName.Is((ParameterType)typeof(int), (ParameterType)typeof(string)), Is.EqualTo(true));
            Assert.That(request2.GenericArguments.Is(typeof(double), typeof(string)), Is.EqualTo(true));
            Assert.That(request2.GenericArgumentsByName.Is((ParameterType)typeof(double), (ParameterType)typeof(string)), Is.EqualTo(true));
        }

        [Test]
        public void JsonCloneTest()
        {
            var request1 = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[] { 2, 2 }, new byte[] { 3, 3, 3 } },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };
            var request2 = request1.JsonClone() as WitRequest;

            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));

            Assert.That(request2.MethodName, Is.EqualTo("1"));
            Assert.That(request2.Parameters.SelectMany(x => x).Is(new byte[] { 2, 2, 3, 3, 3 }), Is.EqualTo(true));
            Assert.That(request2.ParameterTypes.Is(typeof(int), typeof(string)), Is.EqualTo(true));
            Assert.That(request2.ParameterTypesByName.Is((ParameterType)typeof(int), (ParameterType)typeof(string)), Is.EqualTo(true));
            Assert.That(request2.GenericArguments.Is(typeof(double), typeof(string)), Is.EqualTo(true));
            Assert.That(request2.GenericArgumentsByName.Is((ParameterType)typeof(double), (ParameterType)typeof(string)), Is.EqualTo(true));
            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var request1 = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[] { 2, 2 }, new byte[] { 3, 3, 3 } },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };

            var bytes = request1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var request2 = bytes.FromMessagePackBytes<WitRequest>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var request1 = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[] { 2, 2 }, new byte[] { 3, 3, 3 } },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };

            var json = request1.ToJsonString();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromJsonString<WitRequest>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var request1 = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[] { 2, 2 }, new byte[] { 3, 3, 3 } },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };

            var json = request1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromMemoryPackBytes<WitRequest>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var request1 = new WitRequest
            {
                Token = "0",
                MethodName = "1",
                Parameters = new byte[][] { new byte[] { 2, 2 }, new byte[] { 3, 3, 3 } },
                ParameterTypes = new[] { typeof(int), typeof(string) },
                ParameterTypesByName = new[] { (ParameterType)typeof(int), (ParameterType)typeof(string) },
                GenericArguments = new[] { typeof(double), typeof(string) },
                GenericArgumentsByName = new[] { (ParameterType)typeof(double), (ParameterType)typeof(string) }
            };

            var json = request1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromProtoBytes<WitRequest>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }
    }
}

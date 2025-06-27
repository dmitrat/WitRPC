using System;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Common.Utils;
using OutWit.Communication.Utils;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Tests.Requests
{
    [TestFixture]
    public class WitRequestAuthorizationTests
    {
        [Test]
        public void ConstructorTest()
        {
            var request = new WitRequestAuthorization();
            Assert.That(request.Token, Is.Null);

            request = new WitRequestAuthorization
            {
                Token = "token"
            };

            Assert.That(request.Token, Is.EqualTo("token"));
        }

        [Test]
        public void IsTest()
        {
            var request = new WitRequestAuthorization
            {
                Token = "token"
            };

            Assert.That(request.Is(request.Clone()), Is.True);
            Assert.That(request.Is(request.With(x => x.Token = "token1")), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var request1 = new WitRequestAuthorization
            {
                Token = "token"
            };
            var request2 = request1.Clone() as WitRequestAuthorization;

            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));

            Assert.That(request2.Token, Is.EqualTo("token"));
        }

        [Test]
        public void JsonCloneTest()
        {
            var request1 = new WitRequestAuthorization
            {
                Token = "token"
            };
            var request2 = request1.JsonClone() as WitRequestAuthorization;

            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));

            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var request1 = new WitRequestAuthorization
            {
                Token = "token"
            };

            var bytes = request1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var request2 = bytes.FromMessagePackBytes<WitRequestAuthorization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var request1 = new WitRequestAuthorization
            {
                Token = "token"
            };

            var json = request1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromJsonBytes<WitRequestAuthorization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }


        [Test]
        public void MemoryPackSerializationTest()
        {
            var request1 = new WitRequestAuthorization
            {
                Token = "token"
            };

            var json = request1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromMemoryPackBytes<WitRequestAuthorization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }


        [Test]
        public void ProtoBufSerializationTest()
        {
            var request1 = new WitRequestAuthorization
            {
                Token = "token"
            };

            var json = request1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromProtoBytes<WitRequestAuthorization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }
    }
}

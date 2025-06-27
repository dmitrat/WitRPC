using System;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Common.Utils;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Responses
{
    [TestFixture]
    public class WitResponseAuthorizationTests
    {
        [Test]
        public void ConstructorTest()
        {
            var response = new WitResponseAuthorization();
            Assert.That(response.IsAuthorized, Is.EqualTo(false));
            Assert.That(response.Message, Is.Null);

            response = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            Assert.That(response.IsAuthorized, Is.EqualTo(true));
            Assert.That(response.Message, Is.EqualTo("2"));
        }

        [Test]
        public void IsTest()
        {
            var response = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            Assert.That(response.Is(response.Clone()), Is.True);
            Assert.That(response.Is(response.With(x => x.IsAuthorized = false)), Is.False);
            Assert.That(response.Is(response.With(x => x.Message = "3")), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var response1 = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };
            var response2 = response1.Clone() as WitResponseAuthorization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response2.IsAuthorized, Is.EqualTo(true));
            Assert.That(response2.Message, Is.EqualTo("2"));
        }

        [Test]
        public void JsonCloneTest()
        {
            var response1 = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };
            var response2 = response1.JsonClone() as WitResponseAuthorization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var response1 = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            var bytes = response1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var response2 = bytes.FromMessagePackBytes<WitResponseAuthorization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var response1 = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            var json = response1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromJsonBytes<WitResponseAuthorization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var response1 = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            var json = response1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromMemoryPackBytes<WitResponseAuthorization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var response1 = new WitResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            var json = response1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromProtoBytes<WitResponseAuthorization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }
    }
}

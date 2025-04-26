using System;
using OutWit.Common.Json;
using OutWit.Common.MessagePack;
using OutWit.Common.Utils;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Responses
{
    [TestFixture]
    public class WitComResponseAuthorizationTests
    {
        [Test]
        public void ConstructorTest()
        {
            var response = new WitComResponseAuthorization();
            Assert.That(response.IsAuthorized, Is.EqualTo(false));
            Assert.That(response.Message, Is.Null);

            response = new WitComResponseAuthorization
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
            var response = new WitComResponseAuthorization
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
            var response1 = new WitComResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };
            var response2 = response1.Clone() as WitComResponseAuthorization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response2.IsAuthorized, Is.EqualTo(true));
            Assert.That(response2.Message, Is.EqualTo("2"));
        }

        [Test]
        public void JsonCloneTest()
        {
            var response1 = new WitComResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };
            var response2 = response1.JsonClone() as WitComResponseAuthorization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var response1 = new WitComResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            var bytes = response1.ToPackBytes();
            Assert.That(bytes, Is.Not.Null);

            var response2 = bytes.FromPackBytes<WitComResponseAuthorization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var response1 = new WitComResponseAuthorization
            {
                IsAuthorized = true,
                Message = "2"
            };

            var json = response1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromJsonBytes<WitComResponseAuthorization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }
    }
}

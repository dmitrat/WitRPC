using OutWit.Communication.Requests;
using System;
using OutWit.Common.Json;
using OutWit.Common.MessagePack;
using OutWit.Common.Utils;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Responses
{
    [TestFixture]
    public class WitComResponseInitializationTests
    {
        [Test]
        public void ConstructorTest()
        {
            var response = new WitComResponseInitialization();
            Assert.That(response.SymmetricKey, Is.Null);
            Assert.That(response.Vector, Is.Null);

            response = new WitComResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            Assert.That(response.SymmetricKey, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(response.Vector, Is.EqualTo(new byte[] { 4, 5 }));
        }

        [Test]
        public void IsTest()
        {
            var response = new WitComResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            Assert.That(response.Is(response.Clone()), Is.True);
            Assert.That(response.Is(response.With(x => x.SymmetricKey = new byte[] { 3, 4 })), Is.False);
            Assert.That(response.Is(response.With(x => x.Vector = new byte[] { 5, 6 })), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var response1 = new WitComResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };
            var response2 = response1.Clone() as WitComResponseInitialization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response2.SymmetricKey, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(response2.Vector, Is.EqualTo(new byte[] { 4, 5 }));
        }

        [Test]
        public void JsonCloneTest()
        {
            var response1 = new WitComResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };
            var response2 = response1.JsonClone() as WitComResponseInitialization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var response1 = new WitComResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            var bytes = response1.ToPackBytes();
            Assert.That(bytes, Is.Not.Null);

            var response2 = bytes.FromPackBytes<WitComResponseInitialization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var response1 = new WitComResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            var json = response1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromJsonBytes<WitComResponseInitialization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }
    }
}

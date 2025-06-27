using OutWit.Communication.Requests;
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
    public class WitResponseInitializationTests
    {
        [Test]
        public void ConstructorTest()
        {
            var response = new WitResponseInitialization();
            Assert.That(response.SymmetricKey, Is.Null);
            Assert.That(response.Vector, Is.Null);

            response = new WitResponseInitialization
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
            var response = new WitResponseInitialization
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
            var response1 = new WitResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };
            var response2 = response1.Clone() as WitResponseInitialization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response2.SymmetricKey, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(response2.Vector, Is.EqualTo(new byte[] { 4, 5 }));
        }

        [Test]
        public void JsonCloneTest()
        {
            var response1 = new WitResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };
            var response2 = response1.JsonClone() as WitResponseInitialization;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));

            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var response1 = new WitResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            var bytes = response1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var response2 = bytes.FromMessagePackBytes<WitResponseInitialization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var response1 = new WitResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            var json = response1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromJsonBytes<WitResponseInitialization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var response1 = new WitResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            var json = response1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromMemoryPackBytes<WitResponseInitialization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var response1 = new WitResponseInitialization
            {
                SymmetricKey = new byte[] { 1, 2, 3 },
                Vector = new byte[] { 4, 5 }
            };

            var json = response1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromProtoBytes<WitResponseInitialization>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }
    }
}

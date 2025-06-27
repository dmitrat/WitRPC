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
    public class WitRequestInitializationTests
    {
        [Test]
        public void ConstructorTest()
        {
            var request = new WitRequestInitialization();
            Assert.That(request.PublicKey, Is.Null);

            request = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };

            Assert.That(request.PublicKey, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void IsTest()
        {
            var request = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };

            Assert.That(request.Is(request.Clone()), Is.True);
            Assert.That(request.Is(request.With(x => x.PublicKey = new byte[] { 3, 4 })), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var request1 = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };
            var request2 = request1.Clone() as WitRequestInitialization;

            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));

            Assert.That(request2.PublicKey, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void JsonCloneTest()
        {
            var request1 = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };
            var request2 = request1.JsonClone() as WitRequestInitialization;

            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));

            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var request1 = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };

            var bytes = request1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var request2 = bytes.FromMessagePackBytes<WitRequestInitialization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var request1 = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };

            var json = request1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromJsonBytes<WitRequestInitialization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var request1 = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };

            var json = request1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromMemoryPackBytes<WitRequestInitialization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var request1 = new WitRequestInitialization
            {
                PublicKey = new byte[] { 1, 2, 3 }
            };

            var json = request1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var request2 = json.FromProtoBytes<WitRequestInitialization>();
            Assert.That(request2, Is.Not.Null);
            Assert.That(request1, Is.Not.SameAs(request2));
            Assert.That(request1.Is(request2), Is.True);
        }
    }
}

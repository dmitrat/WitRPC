using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Communication.Model;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Responses
{
    [TestFixture]
    public class WitResponseTests
    {
        [Test]
        public void ConstructorTest()
        {
            var response = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo(new byte[] {0 ,1}));
            Assert.That(response.ErrorMessage, Is.EqualTo("2"));
            Assert.That(response.ErrorDetails, Is.EqualTo("3"));
        }

        [Test]
        public void IsTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");
            var response2 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");

            Assert.That(response1.Is(response2), Is.EqualTo(true));

            response2 = new WitResponse(CommunicationStatus.BadRequest, new byte[] {0 ,1}, "2", "3");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitResponse(CommunicationStatus.Ok, new byte[] { 0 }, "2", "3");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "3", "3");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "4");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitResponse(CommunicationStatus.Ok, null, "2", "4");
            Assert.That(response1.Is(response2), Is.EqualTo(false));
        }

        [Test]
        public void CloneTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");
            var response2 = response1.Clone() as WitResponse;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void JsonCloneTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");
            var response2 = response1.JsonClone() as WitResponse;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");

            var bytes = response1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var response2 = bytes.FromMessagePackBytes<WitResponse>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] {0 ,1}, "2", "3");

            var json = response1.ToJsonString();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromJsonString<WitResponse>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] { 0, 1 }, "2", "3");

            var json = response1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromMemoryPackBytes<WitResponse>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var response1 = new WitResponse(CommunicationStatus.Ok, new byte[] { 0, 1 }, "2", "3");

            var json = response1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromProtoBytes<WitResponse>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }
    }
}

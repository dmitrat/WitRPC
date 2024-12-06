using OutWit.Common.MessagePack;
using OutWit.Communication.Model;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Responses
{
    [TestFixture]
    public class WitComResponseTests
    {
        [Test]
        public void ConstructorTest()
        {
            var response = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(response.Data, Is.EqualTo("1"));
            Assert.That(response.ErrorMessage, Is.EqualTo("2"));
            Assert.That(response.ErrorDetails, Is.EqualTo("3"));
        }

        [Test]
        public void IsTest()
        {
            var response1 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");
            var response2 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");

            Assert.That(response1.Is(response2), Is.EqualTo(true));

            response2 = new WitComResponse(CommunicationStatus.BadRequest, "1", "2", "3");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitComResponse(CommunicationStatus.Ok, "2", "2", "3");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitComResponse(CommunicationStatus.Ok, "1", "3", "3");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "4");
            Assert.That(response1.Is(response2), Is.EqualTo(false));

            response2 = new WitComResponse(CommunicationStatus.Ok, null, "2", "4");
            Assert.That(response1.Is(response2), Is.EqualTo(false));
        }

        [Test]
        public void CloneTest()
        {
            var response1 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");
            var response2 = response1.Clone() as WitComResponse;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void JsonCloneTest()
        {
            var response1 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");
            var response2 = response1.JsonClone() as WitComResponse;

            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var response1 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");

            var bytes = response1.ToPackBytes();
            Assert.That(bytes, Is.Not.Null);

            var response2 = bytes.FromPackBytes<WitComResponse>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var response1 = new WitComResponse(CommunicationStatus.Ok, "1", "2", "3");

            var json = response1.ToJsonString();
            Assert.That(json, Is.Not.Null);

            var response2 = json.FromJsonString<WitComResponse>();
            Assert.That(response2, Is.Not.Null);
            Assert.That(response1, Is.Not.SameAs(response2));
            Assert.That(response1.Is(response2), Is.True);
        }
    }
}

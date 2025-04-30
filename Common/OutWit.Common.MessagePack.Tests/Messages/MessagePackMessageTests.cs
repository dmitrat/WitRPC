using OutWit.Common.MessagePack.Messages;
using NUnit.Framework;

namespace OutWit.Common.MessagePack.Tests.Messages
{
    [TestFixture]
    public class MessagePackMessageTests
    {
        [Test]
        public void ConstructorTest()
        {
            var message = new MessagePackMessage("testMessage", true);

            Assert.That(message.Message, Is.EqualTo("testMessage"));
            Assert.That(message.IsError, Is.EqualTo(true));
        }

        [Test]
        public void IsTest()
        {
            var message1 = new MessagePackMessage("testMessage", true);
            var message2 = new MessagePackMessage("testMessage", true);

            Assert.That(message1.Is(message2), Is.True);

            message2 = new MessagePackMessage("testMessage1", true);
            Assert.That(message1.Is(message2), Is.False);

            message2 = new MessagePackMessage("testMessage", false);
            Assert.That(message1.Is(message2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new MessagePackMessage("testMessage", true);

            var message2 = message1.Clone() as MessagePackMessage;
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var message1 = new MessagePackMessage("testMessage", true);

            var bytes = message1.ToMessagePackBytes(false);

            var message2 = bytes.FromMessagePackBytes<MessagePackMessage>(false);
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}

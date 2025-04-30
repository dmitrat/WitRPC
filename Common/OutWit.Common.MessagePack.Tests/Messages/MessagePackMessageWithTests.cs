using OutWit.Common.MessagePack.Messages;
using OutWit.Common.MessagePack.Tests.Utils;
using NUnit.Framework;

namespace OutWit.Common.MessagePack.Tests.Messages
{
    [TestFixture]
    public class MessagePackMessageWithTests
    {
        [Test]
        public void ConstructorTest()
        {
            var data = new MockData {Text = "testText", Value = 0.123};
            var message = new MessagePackMessageWith<MockData>("testMessage", true, data);

            Assert.That(message.Message, Is.EqualTo("testMessage"));
            Assert.That(message.IsError, Is.EqualTo(true));
            Assert.That(message.Data, Is.EqualTo(data));
        }

        [Test]
        public void IsTest()
        {
            var message1 = new MessagePackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123 });
            var message2 = new MessagePackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123 });

            Assert.That(message1.Is(message2), Is.True);

            message2 = new MessagePackMessageWith<MockData>("testMessage1", true, new MockData { Text = "testText", Value = 0.123 });
            Assert.That(message1.Is(message2), Is.False);

            message2 = new MessagePackMessageWith<MockData>("testMessage", false, new MockData { Text = "testText", Value = 0.123 });
            Assert.That(message1.Is(message2), Is.False);

            message2 = new MessagePackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText1", Value = 0.123 });
            Assert.That(message1.Is(message2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new MessagePackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123 });

            var message2 = message1.Clone() as MessagePackMessageWith<MockData>;
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var message1 = new MessagePackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123 });

            var bytes = message1.ToMessagePackBytes(false);

            var message2 = bytes.FromMessagePackBytes<MessagePackMessageWith<MockData>>(false);
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}

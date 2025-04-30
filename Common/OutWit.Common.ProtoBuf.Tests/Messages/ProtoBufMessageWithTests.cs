using NUnit.Framework;
using OutWit.Common.ProtoBuf.Messages;
using OutWit.Common.ProtoBuf.Tests.Utils;

namespace OutWit.Common.ProtoBuf.Tests.Messages
{
    [TestFixture]
    public class ProtoBufMessageWithTests
    {
        [Test]
        public void ConstructorTest()
        {
            var data = new MockData {Text = "testText", Value = 0.123, Type = typeof(MockData)};
            var message = new ProtoBufMessageWith<MockData>("testMessage", true, data);

            Assert.That(message.Message, Is.EqualTo("testMessage"));
            Assert.That(message.IsError, Is.EqualTo(true));
            Assert.That(message.Data, Is.EqualTo(data));
        }

        [Test]
        public void IsTest()
        {
            var message1 = new ProtoBufMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });
            var message2 = new ProtoBufMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });

            Assert.That(message1.Is(message2), Is.True);

            message2 = new ProtoBufMessageWith<MockData>("testMessage1", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });
            Assert.That(message1.Is(message2), Is.False);

            message2 = new ProtoBufMessageWith<MockData>("testMessage", false, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });
            Assert.That(message1.Is(message2), Is.False);

            message2 = new ProtoBufMessageWith<MockData>("testMessage", true, new MockData { Text = "testText1", Value = 0.123, Type = typeof(MockData) });
            Assert.That(message1.Is(message2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new ProtoBufMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });

            var message2 = message1.Clone() as ProtoBufMessageWith<MockData>;
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var message1 = new ProtoBufMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });

            var bytes = message1.ToProtoBytes();
            Assert.That(bytes, Is.Not.Null);
            
            var message2 = bytes.FromProtoBytes<ProtoBufMessageWith<MockData>>();
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}

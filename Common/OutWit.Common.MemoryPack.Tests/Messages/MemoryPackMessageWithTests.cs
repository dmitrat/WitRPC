using NUnit.Framework;
using OutWit.Common.MemoryPack.Messages;
using OutWit.Common.MemoryPack.Tests.Utils;

namespace OutWit.Common.MemoryPack.Tests.Messages
{
    [TestFixture]
    public class MemoryPackMessageWithTests
    {
        [Test]
        public void ConstructorTest()
        {
            var data = new MockData {Text = "testText", Value = 0.123, Type = typeof(MockData)};
            var message = new MemoryPackMessageWith<MockData>("testMessage", true, data);

            Assert.That(message.Message, Is.EqualTo("testMessage"));
            Assert.That(message.IsError, Is.EqualTo(true));
            Assert.That(message.Data, Is.EqualTo(data));
        }

        [Test]
        public void IsTest()
        {
            var message1 = new MemoryPackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });
            var message2 = new MemoryPackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });

            Assert.That(message1.Is(message2), Is.True);

            message2 = new MemoryPackMessageWith<MockData>("testMessage1", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });
            Assert.That(message1.Is(message2), Is.False);

            message2 = new MemoryPackMessageWith<MockData>("testMessage", false, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });
            Assert.That(message1.Is(message2), Is.False);

            message2 = new MemoryPackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText1", Value = 0.123, Type = typeof(MockData) });
            Assert.That(message1.Is(message2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new MemoryPackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });

            var message2 = message1.Clone() as MemoryPackMessageWith<MockData>;
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var message1 = new MemoryPackMessageWith<MockData>("testMessage", true, new MockData { Text = "testText", Value = 0.123, Type = typeof(MockData) });

            var bytes = message1.ToMemoryPackBytes();
            Assert.That(bytes, Is.Not.Null);
            
            var message2 = bytes.FromMemoryPackBytes<MemoryPackMessageWith<MockData>>();
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}

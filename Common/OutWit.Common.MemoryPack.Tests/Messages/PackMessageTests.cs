
using NUnit.Framework;
using OutWit.Common.MemoryPack.Messages;

namespace OutWit.Common.MemoryPack.Tests.Messages
{
    [TestFixture]
    public class PackMessageTests
    {
        [Test]
        public void ConstructorTest()
        {
            var message = new PackMessage("testMessage", true);

            Assert.That(message.Message, Is.EqualTo("testMessage"));
            Assert.That(message.IsError, Is.EqualTo(true));
        }

        [Test]
        public void IsTest()
        {
            var message1 = new PackMessage("testMessage", true);
            var message2 = new PackMessage("testMessage", true);

            Assert.That(message1.Is(message2), Is.True);

            message2 = new PackMessage("testMessage1", true);
            Assert.That(message1.Is(message2), Is.False);

            message2 = new PackMessage("testMessage", false);
            Assert.That(message1.Is(message2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new PackMessage("testMessage", true);

            var message2 = message1.Clone() as PackMessage;
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var message1 = new PackMessage("testMessage", true);

            var bytes = message1.ToMemoryPackBytes();
            Assert.That(bytes, Is.Not.Null);
            
            var message2 = bytes.FromMemoryPackBytes<PackMessage>();
            Assert.That(message2, Is.Not.Null);

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}

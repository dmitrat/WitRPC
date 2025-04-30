using OutWit.Common.MessagePack.Messages;
using OutWit.Common.MessagePack.Ranges;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace OutWit.Common.MessagePack.Tests.Ranges
{
    [TestFixture]
    public class MessagePackRangeTests
    {
        [Test]
        public void ConstructorTest()
        {
            var range = new MessagePackRange<int>(1, 2);

            Assert.That(range.From, Is.EqualTo(1));
            Assert.That(range.To, Is.EqualTo(2));
        }

        [Test]
        public void IsTest()
        {
            var range1 = new MessagePackRange<int>(1, 2);
            var range2 = new MessagePackRange<int>(1, 2);

            Assert.That(range1.Is(range2), Is.True);

            range2 = new MessagePackRange<int>(1, 3);
            Assert.That(range1.Is(range2), Is.False);

            range2 = new MessagePackRange<int>(3, 2);
            Assert.That(range1.Is(range2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var range1 = new MessagePackRange<int>(1, 2);

            var range2 = range1.Clone() as MessagePackRange<int>;
            Assert.That(range2, Is.Not.Null);

            Assert.That(range1, Is.Not.SameAs(range2));
            Assert.That(range1.Is(range2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var range1 = new MessagePackRange<int>(1, 2);

            var bytes = range1.ToMessagePackBytes(false);

            var range2 = bytes.FromMessagePackBytes<MessagePackRange<int>>(false);
            Assert.That(range2, Is.Not.Null);

            Assert.That(range1, Is.Not.SameAs(range2));
            Assert.That(range1.Is(range2), Is.True);
        }
    }
}

using OutWit.Common.MemoryPack.Messages;
using OutWit.Common.MemoryPack.Ranges;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace OutWit.Common.MemoryPack.Tests.Ranges
{
    [TestFixture]
    public class MemoryPackRangeTests
    {
        [Test]
        public void ConstructorTest()
        {
            var range = new MemoryPackRange<int>(1, 2);

            Assert.That(range.From, Is.EqualTo(1));
            Assert.That(range.To, Is.EqualTo(2));
        }

        [Test]
        public void IsTest()
        {
            var range1 = new MemoryPackRange<int>(1, 2);
            var range2 = new MemoryPackRange<int>(1, 2);

            Assert.That(range1.Is(range2), Is.True);

            range2 = new MemoryPackRange<int>(1, 3);
            Assert.That(range1.Is(range2), Is.False);

            range2 = new MemoryPackRange<int>(3, 2);
            Assert.That(range1.Is(range2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var range1 = new MemoryPackRange<int>(1, 2);

            var range2 = range1.Clone() as MemoryPackRange<int>;
            Assert.That(range2, Is.Not.Null);

            Assert.That(range1, Is.Not.SameAs(range2));
            Assert.That(range1.Is(range2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var range1 = new MemoryPackRange<int>(1, 2);

            var bytes = range1.ToMemoryPackBytes();
            Assert.That(bytes, Is.Not.Null);
            
            var range2 = bytes.FromMemoryPackBytes<MemoryPackRange<int>>();
            Assert.That(range2, Is.Not.Null);

            Assert.That(range1, Is.Not.SameAs(range2));
            Assert.That(range1.Is(range2), Is.True);
        }
    }
}

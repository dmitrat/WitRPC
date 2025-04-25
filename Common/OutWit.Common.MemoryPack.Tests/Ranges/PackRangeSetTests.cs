using OutWit.Common.MemoryPack.Messages;
using OutWit.Common.MemoryPack.Ranges;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace OutWit.Common.MemoryPack.Tests.Ranges
{
    [TestFixture]
    public class PackRangeSetTests
    {
        [Test]
        public void ConstructorTest()
        {
            var set = new PackRangeSet<int>();
            Assert.That(set.Count, Is.EqualTo(0));

            set = new PackRangeSet<int>(new PackRange<int>(1, 2), new PackRange<int>(3, 4));
            Assert.That(set.Count, Is.EqualTo(2));

            Assert.That(set[0].From, Is.EqualTo(1));
            Assert.That(set[0].To, Is.EqualTo(2));

            Assert.That(set[1].From, Is.EqualTo(3));
            Assert.That(set[1].To, Is.EqualTo(4));
        }

        [Test]
        public void IsTest()
        {
            var set1 = new PackRangeSet<int>(new PackRange<int>(1, 2), new PackRange<int>(3, 4));
            var set2 = new PackRangeSet<int>(new PackRange<int>(1, 2), new PackRange<int>(3, 4));

            Assert.That(set1.Is(set2), Is.True);

            set2 = new PackRangeSet<int>(new PackRange<int>(1, 2), new PackRange<int>(2, 4));
            Assert.That(set1.Is(set2), Is.False);

            set2 = new PackRangeSet<int>(new PackRange<int>(1, 2));
            Assert.That(set1.Is(set2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var set1 = new PackRangeSet<int>(new PackRange<int>(1, 2), new PackRange<int>(3, 4));

            var set2 = set1.Clone() as PackRangeSet<int>;
            Assert.That(set2, Is.Not.Null);

            Assert.That(set1, Is.Not.SameAs(set2));
            Assert.That(set1.Is(set2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var set1 = new PackRangeSet<int>(new PackRange<int>(1, 2), new PackRange<int>(3, 4));

            var bytes = set1.ToMemoryPackBytes();
            Assert.That(bytes, Is.Not.Null);
            
            var set2 = bytes.FromMemoryPackBytes<PackRangeSet<int>>();
            Assert.That(set2, Is.Not.Null);

            Assert.That(set1, Is.Not.SameAs(set2));
            Assert.That(set1.Is(set2), Is.True);
        }
    }
}

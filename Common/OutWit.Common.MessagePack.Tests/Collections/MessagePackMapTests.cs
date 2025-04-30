using System;
using System.Collections.Generic;
using OutWit.Common.MessagePack.Collections;
using OutWit.Common.Serialization;
using NUnit.Framework;

namespace OutWit.Common.MessagePack.Tests.Collections
{
    [TestFixture]
    public class MessagePackMapTests
    {
        [Test]
        public void ConstructorTest()
        {
            var collection = new MessagePackMap<string, int>(new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } });

            Assert.That(collection.Count, Is.EqualTo(3));

            Assert.That(collection["a"], Is.EqualTo(1));
            Assert.That(collection["b"], Is.EqualTo(2));
            Assert.That(collection["c"], Is.EqualTo(3));
        }

        [Test]
        public void IsTest()
        {
            var collection1 = new MessagePackMap<string, int>(new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } });
            var collection2 = new MessagePackMap<string, int>(new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } });

            Assert.That(collection1.Is(collection2), Is.True);

            collection2 = new MessagePackMap<string, int>(new Dictionary<string, int> { { "d", 1 }, { "b", 2 }, { "c", 3 } });

            Assert.That(collection1.Is(collection2), Is.False);

            collection2 = new MessagePackMap<string, int>(new Dictionary<string, int> { { "a", 2 }, { "b", 2 }, { "c", 3 } });

            Assert.That(collection1.Is(collection2), Is.False);

            collection1 = new MessagePackMap<string, int>(new Dictionary<string, int> { { "a", 2 }, { "b", 2 }, { "c", 3 } });

            Assert.That(collection1.Is(collection2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTests()
        {
            var collection = new MessagePackMap<string, double>(new Dictionary<string, double> { { "a", 1.0 }, { "b", 2.0 }, { "c", 3.0 } });

            var bytes = collection.ToMessagePackBytes(false);

            Assert.That(bytes, Is.Not.Null);

            Console.WriteLine(bytes.Length);

            var collection1 = bytes.FromMessagePackBytes<MessagePackMap<string, double>>(false);

            Assert.That(collection1, Is.Not.Null);
            Assert.That(collection.Is(collection1), Is.True);

        }
    }
}
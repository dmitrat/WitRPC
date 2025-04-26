using System;
using System.ComponentModel;
using OutWit.Common.MemoryPack.Tests.Utils;

namespace OutWit.Common.MemoryPack.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void MemoryPackSerializationGenericTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var bytes = mockData1.ToMemoryPackBytes();
            Assert.That(bytes, Is.Not.Null);

            var mockData2 = bytes.FromMemoryPackBytes<MockData>();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));

            var mockData3 = ((ReadOnlySpan<byte>)bytes.AsSpan()).FromMemoryPackBytes<MockData>();
            Assert.That(mockData3, Is.Not.Null);
            Assert.That(mockData3.Text, Is.EqualTo("Test"));
            Assert.That(mockData3.Value, Is.EqualTo(3.14));
            Assert.That(mockData3.Type, Is.EqualTo(typeof(MockData)));
        }


        [Test]
        public void MemoryPackSerializationTypedTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            object mockObject = mockData1;

            var bytes = mockObject.ToMemoryPackBytes(typeof(MockData));
            Assert.That(bytes, Is.Not.Null);

            var mockData2 = bytes.FromMemoryPackBytes(typeof(MockData)) as MockData;
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));

            var mockData3 = ((ReadOnlySpan<byte>)bytes.AsSpan()).FromMemoryPackBytes(typeof(MockData)) as MockData;
            Assert.That(mockData3, Is.Not.Null);
            Assert.That(mockData3.Text, Is.EqualTo("Test"));
            Assert.That(mockData3.Value, Is.EqualTo(3.14));
            Assert.That(mockData3.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void MemoryPackCloneTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var mockData2 = mockData1.MemoryPackClone();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void PropertyChangedEventArgsSerializationTest()
        {
            var arg1 = new PropertyChangedEventArgs("test arg");
            
            var bytes = arg1.ToMemoryPackBytes();
            
            Assert.That(bytes, Is.Not.Null);

            var args2 = bytes.FromMemoryPackBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);
            
            Assert.That(args2.PropertyName, Is.EqualTo(arg1.PropertyName));


            arg1 = new PropertyChangedEventArgs(null);

            bytes = arg1.ToMemoryPackBytes();

            Assert.That(bytes, Is.Not.Null);

            args2 = bytes.FromMemoryPackBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);

            Assert.That(args2.PropertyName, Is.Null);
        }
    }
}

using System;
using System.ComponentModel;
using OutWit.Common.ProtoBuf.Tests.Utils;

namespace OutWit.Common.ProtoBuf.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void ProtoBufSerializationGenericTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var bytes = mockData1.ToProtoBytes();
            Assert.That(bytes, Is.Not.Null);

            var mockData2 = bytes.FromProtoBytes<MockData>();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }


        [Test]
        public void ProtoBufSerializationTypedTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            object mockObject = mockData1;

            var bytes = mockObject.ToProtoBytes(typeof(MockData));
            Assert.That(bytes, Is.Not.Null);

            var mockData2 = bytes.FromProtoBytes(typeof(MockData)) as MockData;
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void ProtoBufCloneTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var mockData2 = mockData1.ProtoClone();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void PropertyChangedEventArgsSerializationTest()
        {
            var arg1 = new PropertyChangedEventArgs("test arg");
            
            var bytes = arg1.ToProtoBytes();
            
            Assert.That(bytes, Is.Not.Null);

            var args2 = bytes.FromProtoBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);
            
            Assert.That(args2.PropertyName, Is.EqualTo(arg1.PropertyName));


            arg1 = new PropertyChangedEventArgs(null);

            bytes = arg1.ToProtoBytes();

            Assert.That(bytes, Is.Not.Null);

            args2 = bytes.FromProtoBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);

            Assert.That(args2.PropertyName, Is.Null);


            arg1 = new PropertyChangedEventArgs("");

            bytes = arg1.ToProtoBytes();

            Assert.That(bytes, Is.Not.Null);

            args2 = bytes.FromProtoBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);

            Assert.That(args2.PropertyName, Is.Empty);
        }
    }
}

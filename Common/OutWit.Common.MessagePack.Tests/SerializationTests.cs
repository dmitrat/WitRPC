using System;
using System.ComponentModel;
using OutWit.Common.Collections;
using OutWit.Common.MessagePack.Tests.Utils;

namespace OutWit.Common.MessagePack.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void MessagePackSerializationGenericTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var bytes = mockData1.ToMessagePackBytes();
            Assert.That(bytes, Is.Not.Null);

            var mockData2 = bytes.FromMessagePackBytes<MockData>();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));

            var mockData3 = ((ReadOnlyMemory<byte>)bytes.AsMemory()).FromMessagePackBytes<MockData>();
            Assert.That(mockData3, Is.Not.Null);
            Assert.That(mockData3.Text, Is.EqualTo("Test"));
            Assert.That(mockData3.Value, Is.EqualTo(3.14));
            Assert.That(mockData3.Type, Is.EqualTo(typeof(MockData)));
        }


        [Test]
        public void MessagePackSerializationTypedTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            object mockObject = mockData1;

            var bytes = mockObject.ToMessagePackBytes(typeof(MockData));
            Assert.That(bytes, Is.Not.Null);

            var mockData2 = bytes.FromMessagePackBytes(typeof(MockData)) as MockData;
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));

            var mockData3 = ((ReadOnlyMemory<byte>)bytes.AsMemory()).FromMessagePackBytes(typeof(MockData)) as MockData;
            Assert.That(mockData3, Is.Not.Null);
            Assert.That(mockData3.Text, Is.EqualTo("Test"));
            Assert.That(mockData3.Value, Is.EqualTo(3.14));
            Assert.That(mockData3.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void MessagePackCloneTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var mockData2 = mockData1.MessagePackClone();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void PropertyChangedEventArgsSerializationTest()
        {
            var arg1 = new PropertyChangedEventArgs("test arg");
            
            var bytes = arg1.ToMessagePackBytes();
            
            Assert.That(bytes, Is.Not.Null);

            var args2 = bytes.FromMessagePackBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);
            
            Assert.That(args2.PropertyName, Is.EqualTo(arg1.PropertyName));


            arg1 = new PropertyChangedEventArgs(null);

            bytes = arg1.ToMessagePackBytes();

            Assert.That(bytes, Is.Not.Null);

            args2 = bytes.FromMessagePackBytes<PropertyChangedEventArgs>();
            Assert.That(args2, Is.Not.Null);

            Assert.That(args2.PropertyName, Is.Null);
        }

        [Test]
        public void MessagePackExportTest()
        {
            var data1 = new MockData[]
            {
                new MockData
                {
                    Text = "Test1",
                    Value = 3.141,
                    Type = typeof(MockData)
                },new MockData
                {
                    Text = "Test2",
                    Value = 3.142,
                    Type = typeof(MockData)
                },new MockData
                {
                    Text = "Test3",
                    Value = 3.143,
                    Type = typeof(MockData)
                },
            };

            var filePath = Path.GetTempFileName();
            data1.ExportAsMessagePack(filePath);

            IReadOnlyList<MockData>? data2 = MessagePackUtils.LoadAsMessagePack<MockData>(filePath);
            
            Assert.That(data2, Is.Not.Null);
            
            Assert.That(data2.Is(data1), Is.EqualTo(true));
        }

        [Test]
        public async Task MessagePackExportAsyncTest()
        {
            var data1 = new MockData[]
            {
                new MockData
                {
                    Text = "Test1",
                    Value = 3.141,
                    Type = typeof(MockData)
                },new MockData
                {
                    Text = "Test2",
                    Value = 3.142,
                    Type = typeof(MockData)
                },new MockData
                {
                    Text = "Test3",
                    Value = 3.143,
                    Type = typeof(MockData)
                },
            };

            var filePath = Path.GetTempFileName();
            await  data1.ExportAsMessagePackAsync(filePath);

            IReadOnlyList<MockData>? data2 = await MessagePackUtils.LoadAsMessagePackAsync<MockData>(filePath);

            Assert.That(data2, Is.Not.Null);

            Assert.That(data2.Is(data1), Is.EqualTo(true));
        }
    }
}

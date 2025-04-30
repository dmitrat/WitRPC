using OutWit.Common.Collections;
using OutWit.Common.Json.Tests.Utils;

namespace OutWit.Common.Json.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void JsonStringSerializationGenericTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var jsonString = mockData1.ToJsonString();
            Assert.That(jsonString, Is.Not.Null);
            
            Console.WriteLine(jsonString);

            var mockData2 = jsonString.FromJsonString<MockData>();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void JsonStringSerializationTypedTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            object mockObject = mockData1;

            var jsonString = mockObject.ToJsonString(typeof(MockData));
            Assert.That(jsonString, Is.Not.Null);

            Console.WriteLine(jsonString);

            var mockData2 = jsonString.FromJsonString(typeof(MockData)) as MockData;
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void JsonStringSerializationIndentedTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var jsonString = mockData1.ToJsonString(indented: true);
            Assert.That(jsonString, Is.Not.Null);

            Console.WriteLine(jsonString);

            var mockData2 = jsonString.FromJsonString<MockData>();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void JsonBytesSerializationGenericTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var jsonBytes = mockData1.ToJsonBytes();
            Assert.That(jsonBytes, Is.Not.Null);

            var mockData2 = jsonBytes.FromJsonBytes<MockData>();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));

            var mockData3 = ((ReadOnlySpan<byte>)jsonBytes.AsSpan()).FromJsonBytes<MockData>();
            Assert.That(mockData3, Is.Not.Null);
            Assert.That(mockData3.Text, Is.EqualTo("Test"));
            Assert.That(mockData3.Value, Is.EqualTo(3.14));
            Assert.That(mockData3.Type, Is.EqualTo(typeof(MockData)));
        }


        [Test]
        public void JsonBytesSerializationTypedTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            object mockObject = mockData1;

            var jsonBytes = mockObject.ToJsonBytes(typeof(MockData));
            Assert.That(jsonBytes, Is.Not.Null);

            var mockData2 = jsonBytes.FromJsonBytes(typeof(MockData)) as MockData;
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));

            var mockData3 = ((ReadOnlySpan<byte>)jsonBytes.AsSpan()).FromJsonBytes(typeof(MockData)) as MockData;
            Assert.That(mockData3, Is.Not.Null);
            Assert.That(mockData3.Text, Is.EqualTo("Test"));
            Assert.That(mockData3.Value, Is.EqualTo(3.14));
            Assert.That(mockData3.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void JsonCloneTest()
        {
            var mockData1 = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            var mockData2 = mockData1.JsonClone();
            Assert.That(mockData2, Is.Not.Null);
            Assert.That(mockData2.Text, Is.EqualTo("Test"));
            Assert.That(mockData2.Value, Is.EqualTo(3.14));
            Assert.That(mockData2.Type, Is.EqualTo(typeof(MockData)));
        }

        [Test]
        public void JsonBytesExportTest()
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
            data1.ExportAsJsonBytes(filePath);

            IReadOnlyList<MockData>? data2 = JsonUtils.LoadAsJsonBytes<MockData>(filePath);

            Assert.That(data2, Is.Not.Null);

            Assert.That(data2.Is(data1), Is.EqualTo(true));
        }

        [Test]
        public async Task JsonBytesExportAsyncTest()
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
            await data1.ExportAsJsonBytesAsync(filePath);

            IReadOnlyList<MockData>? data2 = await JsonUtils.LoadAsJsonBytesAsync<MockData>(filePath);

            Assert.That(data2, Is.Not.Null);

            Assert.That(data2.Is(data1), Is.EqualTo(true));
        }

        [Test]
        public void JsonStringExportTest()
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
            data1.ExportAsJsonString(filePath);

            IReadOnlyList<MockData>? data2 = JsonUtils.LoadAsJsonString<MockData>(filePath);

            Assert.That(data2, Is.Not.Null);

            Assert.That(data2.Is(data1), Is.EqualTo(true));
        }

        [Test]
        public async Task JsonStringExportAsyncTest()
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
            await data1.ExportAsJsonStringAsync(filePath);

            IReadOnlyList<MockData>? data2 = await JsonUtils.LoadAsJsonStringAsync<MockData>(filePath);

            Assert.That(data2, Is.Not.Null);

            Assert.That(data2.Is(data1), Is.EqualTo(true));
        }
    }
}

using OutWit.Common.MemoryPack.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.MemoryPack.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void TypeSerializationTest()
        {
            var mockData = new MockData
            {
                Text = "Test",
                Value = 3.14,
                Type = typeof(MockData)
            };

            object data = mockData;

            var bytes = data.ToMemoryPackBytes();

            var val = bytes?.FromMemoryPackBytes(typeof(MockData));

        }
    }
}

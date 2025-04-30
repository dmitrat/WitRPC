using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Serialization
{
    [TestFixture]
    public class ModelSerializationTests
    {
        [Test]
        public void JsonSerializationTest()
        {
            var complexType1 = new ComplexType
            {
                Int32Value = 1, StringValue = "test"
            };

            var bytes = complexType1.ToJsonBytes();
            
            Assert.That(bytes, Is.Not.Null);

            var complexType2 = bytes.FromJsonBytes<ComplexType>();
            
            Assert.That(complexType2, Is.Not.Null);
            Assert.That(complexType2.Int32Value, Is.EqualTo(1));
            Assert.That(complexType2.StringValue, Is.EqualTo("test"));


            var complexNumber1 = new ComplexNumber<int, double>(1, 2.34);

            bytes = complexNumber1.ToJsonBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexNumber2 = bytes.FromJsonBytes<ComplexNumber<int, double>>();

            Assert.That(complexNumber2, Is.Not.Null);
            Assert.That(complexNumber2.A, Is.EqualTo(1));
            Assert.That(complexNumber2.B, Is.EqualTo(2.34));
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var complexType1 = new ComplexType
            {
                Int32Value = 1,
                StringValue = "test"
            };

            var bytes = complexType1.ToMessagePackBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexType2 = bytes.FromMessagePackBytes<ComplexType>();

            Assert.That(complexType2, Is.Not.Null);
            Assert.That(complexType2.Int32Value, Is.EqualTo(1));
            Assert.That(complexType2.StringValue, Is.EqualTo("test"));
            
            
            var complexNumber1 = new ComplexNumber<int, double>(1, 2.34);

            bytes = complexNumber1.ToMessagePackBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexNumber2 = bytes.FromMessagePackBytes<ComplexNumber<int, double>>();

            Assert.That(complexNumber2, Is.Not.Null);
            Assert.That(complexNumber2.A, Is.EqualTo(1));
            Assert.That(complexNumber2.B, Is.EqualTo(2.34));

        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var complexType1 = new ComplexType
            {
                Int32Value = 1,
                StringValue = "test"
            };

            var bytes = complexType1.ToMemoryPackBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexType2 = bytes.FromMemoryPackBytes<ComplexType>();

            Assert.That(complexType2, Is.Not.Null);
            Assert.That(complexType2.Int32Value, Is.EqualTo(1));
            Assert.That(complexType2.StringValue, Is.EqualTo("test"));


            var complexNumber1 = new ComplexNumber<int, double>(1, 2.34);

            bytes = complexNumber1.ToMemoryPackBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexNumber2 = bytes.FromMemoryPackBytes<ComplexNumber<int, double>>();

            Assert.That(complexNumber2, Is.Not.Null);
            Assert.That(complexNumber2.A, Is.EqualTo(1));
            Assert.That(complexNumber2.B, Is.EqualTo(2.34));
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var complexType1 = new ComplexType
            {
                Int32Value = 1,
                StringValue = "test"
            };

            var bytes = complexType1.ToProtoBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexType2 = bytes.FromProtoBytes<ComplexType>();

            Assert.That(complexType2, Is.Not.Null);
            Assert.That(complexType2.Int32Value, Is.EqualTo(1));
            Assert.That(complexType2.StringValue, Is.EqualTo("test"));


            var complexNumber1 = new ComplexNumber<int, double>(1, 2.34);

            bytes = complexNumber1.ToProtoBytes();

            Assert.That(bytes, Is.Not.Null);

            var complexNumber2 = bytes.FromProtoBytes<ComplexNumber<int, double>>();

            Assert.That(complexNumber2, Is.Not.Null);
            Assert.That(complexNumber2.A, Is.EqualTo(1));
            Assert.That(complexNumber2.B, Is.EqualTo(2.34));
        }
    }
}

using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Communication.Model;
using OutWit.Communication.Tests.Mock.Model;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Model
{
    [TestFixture]
    public class ParameterTypeTests
    {
        [Test]
        public void ConstructorTest()
        {
            var type = new ParameterType("1", "2");
            Assert.That(type.Type, Is.EqualTo("1"));
            Assert.That(type.Assembly, Is.EqualTo("2"));

            type = new ParameterType(typeof(ComplexType));
            Assert.That(type.Type, Is.EqualTo("OutWit.Communication.Tests.Mock.Model.ComplexType"));
            Assert.That(type.Assembly, Is.EqualTo("OutWit.Communication.Tests"));
        }

        [Test]
        public void OperatorsTest()
        {
            var type1 = typeof(ComplexType);
            Assert.That(type1.FullName, Is.EqualTo("OutWit.Communication.Tests.Mock.Model.ComplexType"));
            Assert.That(type1.Assembly.GetName().Name, Is.EqualTo("OutWit.Communication.Tests"));

            var paramType = (ParameterType)type1;
            Assert.That(paramType.Type, Is.EqualTo("OutWit.Communication.Tests.Mock.Model.ComplexType"));
            Assert.That(paramType.Assembly, Is.EqualTo("OutWit.Communication.Tests"));

            var type2 = (Type?)paramType;
            Assert.That(type2, Is.Not.Null);
            Assert.That(type2.FullName, Is.EqualTo("OutWit.Communication.Tests.Mock.Model.ComplexType"));
            Assert.That(type2.Assembly.GetName().Name, Is.EqualTo("OutWit.Communication.Tests"));

        }

        [Test]
        public void IsTest()
        {
            var type1 = new ParameterType("1", "2");
            var type2 = new ParameterType("1", "2");
            Assert.That(type1.Is(type2), Is.True);

            type2 = new ParameterType("2", "2");
            Assert.That(type1.Is(type2), Is.False);

            type2 = new ParameterType("1", "3");
            Assert.That(type1.Is(type2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var type1 = new ParameterType("1", "2");
            var type2 = type1.Clone() as ParameterType;

            Assert.That(type1, Is.Not.SameAs(type2));
            Assert.That(type1.Is(type2), Is.True);
        }

        [Test]
        public void JsonCloneTest()
        {
            var type1 = new ParameterType("1", "2");
            var type2 = type1.JsonClone() as ParameterType;

            Assert.That(type2, Is.Not.Null);
            Assert.That(type1, Is.Not.SameAs(type2));
            Assert.That(type1.Is(type2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var type1 = new ParameterType("1", "2");

            var bytes = type1.ToPackBytes();
            Assert.That(bytes, Is.Not.Null);

            var type2 = bytes.FromPackBytes<ParameterType>();
            Assert.That(type2, Is.Not.Null);
            Assert.That(type1, Is.Not.SameAs(type2));
            Assert.That(type1.Is(type2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var type1 = new ParameterType("1", "2");

            var json = type1.ToJsonString();
            Assert.That(json, Is.Not.Null);

            var type2 = json.FromJsonString<ParameterType>();
            Assert.That(type2, Is.Not.Null);
            Assert.That(type1, Is.Not.SameAs(type2));
            Assert.That(type1.Is(type2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var type1 = new ParameterType("1", "2");

            var json = type1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var type2 = json.FromMemoryPackBytes<ParameterType>();
            Assert.That(type2, Is.Not.Null);
            Assert.That(type1, Is.Not.SameAs(type2));
            Assert.That(type1.Is(type2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var type1 = new ParameterType("1", "2");

            var json = type1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var type2 = json.FromProtoBytes<ParameterType>();
            Assert.That(type2, Is.Not.Null);
            Assert.That(type1, Is.Not.SameAs(type2));
            Assert.That(type1.Is(type2), Is.True);
        }
    }
}
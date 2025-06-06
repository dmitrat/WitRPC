using System;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;
using OutWit.Communication.Tests.Mock.Interfaces;
using ProtoBuf;

namespace OutWit.Communication.Tests.Mock.Model
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class ComplexType : ModelBase, IComplexType
    {
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is ComplexType request))
                return false;

            return Int32Value.Is(request.Int32Value) &&
                   StringValue.Is(request.StringValue);
        }

        public override ComplexType Clone()
        {
            return new ComplexType
            {
                Int32Value = Int32Value,
                StringValue = StringValue
            };
        }

        public int GetValue()
        {
            return Int32Value;
        }

        public IComplexType Create(int intValue, string stringValue)
        {
            return new ComplexType
            {
                Int32Value = intValue,
                StringValue = stringValue
            };
        }

        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        [ToString]
        public int Int32Value { get; set; }

        [Key(1)]
        [DataMember]
        [ProtoMember(2)]
        [ToString]
        public string? StringValue { get; set; }
    }
}

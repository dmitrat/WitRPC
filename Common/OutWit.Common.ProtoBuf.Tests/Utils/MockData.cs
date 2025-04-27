using ProtoBuf;
using OutWit.Common.Abstract;
using OutWit.Common.Values;


namespace OutWit.Common.ProtoBuf.Tests.Utils
{
    [ProtoContract]
    public class MockData : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MockData data))
                return false;

            return Text.Is(data.Text) &&
                   Value.Is(data.Value, tolerance) &&
                   Type?.Equals(data.Type) == true;
        }

        public override ModelBase Clone()
        {
            return new MockData {Text = Text, Value = Value, Type = Type};
        }

        [ProtoMember(1)]
        public string? Text { get; set; }

        [ProtoMember(2)]
        public double Value { get; set; }

        [ProtoMember(3)]
        public Type? Type { get; set; }
    }
}

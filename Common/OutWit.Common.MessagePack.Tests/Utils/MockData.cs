using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.MessagePack.Tests.Utils
{
    [MessagePackObject]
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

        [Key(0)]
        public string Text { get; set; }

        [Key(1)]
        public double Value { get; set; }

        [Key(2)]
        public Type Type { get; set; }
    }
}

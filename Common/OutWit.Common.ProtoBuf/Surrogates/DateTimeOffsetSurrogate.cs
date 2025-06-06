using System;
using ProtoBuf;

namespace OutWit.Common.ProtoBuf.Surrogates
{
    [ProtoContract(Name = nameof(DateTimeOffset))]
    internal class DateTimeOffsetSurrogate
    {

        #region Operators

        public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate s)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(s?.Value ?? 0);
        }

        public static implicit operator DateTimeOffset?(DateTimeOffsetSurrogate s)
        {
            return s == null || s.Value == null
                ? null
                : DateTimeOffset.FromUnixTimeMilliseconds(s.Value.Value);
        }

        public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset d)
        {
            return new DateTimeOffsetSurrogate { Value = d.ToUnixTimeMilliseconds() };
        }

        public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset? d)
        {
            return new DateTimeOffsetSurrogate { Value = d?.ToUnixTimeMilliseconds() };
        }
        
        #endregion

        #region Properties

        [ProtoMember(1)]
        public long? Value { get; set; }

        #endregion
    }
}

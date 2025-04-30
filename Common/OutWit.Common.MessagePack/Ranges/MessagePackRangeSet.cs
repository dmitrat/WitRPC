using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.MessagePack.Collections;

namespace OutWit.Common.MessagePack.Ranges
{
    [MessagePackObject]
    public class MessagePackRangeSet<TValue>  : MessagePackSet<MessagePackRange<TValue>>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private MessagePackRangeSet() :
            base()
        {
        }

        public MessagePackRangeSet(params MessagePackRange<TValue>[] values) :
            base(values)
        {
        }

        [SerializationConstructor]
        public MessagePackRangeSet(IEnumerable<MessagePackRange<TValue>> values) :
            base(values)
        {
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            if (Count == 0)
                return "";

            var str = "";

            for (int i = 0; i < Count; i++)
                str += $"{this[i]}, ";

            return str.Substring(0, str.Length - 2);
        }
        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MessagePackRangeSet<TValue> set))
                return false;

            return base.Is(set, tolerance);
        }

        public override ModelBase Clone()
        {
            return new MessagePackRangeSet<TValue>(Inner.Select(x => x.Clone()).OfType<MessagePackRange<TValue>>().ToList());
        }

        #endregion
    }
}

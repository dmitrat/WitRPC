using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using OutWit.Common.Abstract;
using OutWit.Common.ProtoBuf.Collections;

namespace OutWit.Common.ProtoBuf.Ranges
{
    [ProtoContract]
    public class ProtoBufRangeSet<TValue>  : ProtoBufSet<ProtoBufRange<TValue>>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private ProtoBufRangeSet() :
            base()
        {
        }

        public ProtoBufRangeSet(params ProtoBufRange<TValue>[] values) :
            base(values)
        {
        }

        public ProtoBufRangeSet(IEnumerable<ProtoBufRange<TValue>> inner) :
            base(inner)
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
            if (!(modelBase is ProtoBufRangeSet<TValue> set))
                return false;

            return base.Is(set, tolerance);
        }

        public override ModelBase Clone()
        {
            return new ProtoBufRangeSet<TValue>(Inner.Select(x => x.Clone()).OfType<ProtoBufRange<TValue>>().ToList());
        }

        #endregion
    }
}

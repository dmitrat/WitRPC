using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using OutWit.Common.Abstract;
using OutWit.Common.ProtoBuf.Collections;

namespace OutWit.Common.ProtoBuf.Ranges
{
    [ProtoContract]
    public class ProtoRangeSet<TValue>  : ProtoSet<ProtoRange<TValue>>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private ProtoRangeSet() :
            base()
        {
        }

        public ProtoRangeSet(params ProtoRange<TValue>[] values) :
            base(values)
        {
        }

        public ProtoRangeSet(IEnumerable<ProtoRange<TValue>> inner) :
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
            if (!(modelBase is ProtoRangeSet<TValue> set))
                return false;

            return base.Is(set, tolerance);
        }

        public override ModelBase Clone()
        {
            return new ProtoRangeSet<TValue>(Inner.Select(x => x.Clone()).OfType<ProtoRange<TValue>>().ToList());
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.MemoryPack.Collections;

namespace OutWit.Common.MemoryPack.Ranges
{
    [MemoryPackable]
    public partial class MemoryPackRangeSet<TValue>  : MemoryPackSet<MemoryPackRange<TValue>>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private MemoryPackRangeSet() :
            base()
        {
        }

        public MemoryPackRangeSet(params MemoryPackRange<TValue>[] values) :
            base(values)
        {
        }

        [MemoryPackConstructor]
        public MemoryPackRangeSet(IEnumerable<MemoryPackRange<TValue>> inner) :
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
            if (!(modelBase is MemoryPackRangeSet<TValue> set))
                return false;

            return base.Is(set, tolerance);
        }

        public override ModelBase Clone()
        {
            return new MemoryPackRangeSet<TValue>(Inner.Select(x => x.Clone()).OfType<MemoryPackRange<TValue>>().ToList());
        }

        #endregion
    }
}

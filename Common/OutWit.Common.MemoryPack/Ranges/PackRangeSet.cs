using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.MemoryPack.Collections;

namespace OutWit.Common.MemoryPack.Ranges
{
    [MemoryPackable]
    public partial class PackRangeSet<TValue>  : PackSet<PackRange<TValue>>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private PackRangeSet() :
            base()
        {
        }

        public PackRangeSet(params PackRange<TValue>[] values) :
            base(values)
        {
        }

        [MemoryPackConstructor]
        public PackRangeSet(IEnumerable<PackRange<TValue>> inner) :
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
            if (!(modelBase is PackRangeSet<TValue> set))
                return false;

            return base.Is(set, tolerance);
        }

        public override ModelBase Clone()
        {
            return new PackRangeSet<TValue>(Inner.Select(x => x.Clone()).OfType<PackRange<TValue>>().ToList());
        }

        #endregion
    }
}

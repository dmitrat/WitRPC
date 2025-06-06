using System;
using System.ComponentModel;
using ProtoBuf;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Interfaces;

namespace OutWit.Common.ProtoBuf.Ranges
{
    [ProtoContract]
    public class ProtoBufRange<TValue> : ModelBase, IRange<TValue>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private ProtoBufRange()
        {
        }

        public ProtoBufRange(TValue from, TValue to)
        {
            From = from;
            To = to;
        }

        #endregion

        #region Functions

        public void Reset(TValue from, TValue to)
        {
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"[{From}, {To}]";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is ProtoBufRange<TValue> range))
                return false;

            return range.From.CompareTo(From) == 0 && 
                   range.To.CompareTo(To) == 0;
        }

        public override ModelBase Clone()
        {
            return new ProtoBufRange<TValue>(From, To);
        }

        #endregion

        #region Properties

        [Notify]
        [ProtoMember(1)]
        public TValue From { get; private set; }

        [Notify]
        [ProtoMember(2)]
        public TValue To { get; private set; }

        #endregion

    }
}

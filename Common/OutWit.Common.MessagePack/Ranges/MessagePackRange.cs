using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Interfaces;

namespace OutWit.Common.MessagePack.Ranges
{
    [MessagePackObject]
    public class MessagePackRange<TValue> : ModelBase, IRange<TValue>, INotifyPropertyChanged
        where TValue : struct, IComparable<TValue>
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Constructors

        private MessagePackRange()
        {
        }

        [SerializationConstructor]
        public MessagePackRange(TValue from, TValue to)
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
            if (!(modelBase is MessagePackRange<TValue> range))
                return false;

            return range.From.CompareTo(From) == 0 && 
                   range.To.CompareTo(To) == 0;
        }

        public override ModelBase Clone()
        {
            return new MessagePackRange<TValue>(From, To);
        }

        #endregion

        #region Properties

        [Key(0)]
        [Notify]
        public TValue From { get; private set; }

        [Key(1)]
        [Notify]
        public TValue To { get; private set; }

        #endregion

    }
}

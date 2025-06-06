using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;

namespace OutWit.Common.MessagePack.Values
{
    [MessagePackObject]
    public class MessagePackValueInSet<TValue> : ModelBase
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private MessagePackValueInSet()
        {
        }

        public MessagePackValueInSet(TValue value, params TValue[] valuesSet) : 
            this(value, valuesSet.ToList().AsReadOnly())
        {

        }

        [SerializationConstructor]
        public MessagePackValueInSet(TValue value, IReadOnlyList<TValue> valuesSet)
        {
            Value = value;
            ValuesSet = valuesSet;
        }

        #endregion

        #region Functions

        public bool Reset(TValue value)
        {
            if (!ValuesSet.Contains(value))
                return false;

            Value = value;

            return true;
        }

        public override string ToString()
        {
            return $"{Value} [{string.Join(",", ValuesSet)}]";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MessagePackValueInSet<TValue> valueInSet))
                return false;

            if (valueInSet.Value.CompareTo(Value) != 0)
                return false;

            if (valueInSet.ValuesSet.Count != ValuesSet.Count)
                return false;

            for (int i = 0; i < ValuesSet.Count; i++)
            {
                if (valueInSet[i].CompareTo(ValuesSet[i]) != 0)
                    return false;
            }

            return true;
        }

        public override ModelBase Clone()
        {
            return new MessagePackValueInSet<TValue>(Value, ValuesSet.ToList().AsReadOnly());
        }

        #endregion

        #region Properties

        [IgnoreMember]
        public TValue this[int index] => ValuesSet[index];

        [IgnoreMember]
        public int Count => ValuesSet.Count;

        [Key(0)]
        [Notify]
        public TValue Value { get; set; }

        [Key(1)]
        [Notify]
        public IReadOnlyList<TValue> ValuesSet { get;  }

        #endregion
    }
}

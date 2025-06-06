using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.MessagePack.Collections
{
    [MessagePackObject]
    public class MessagePackSet<TValue> : ModelBase, IReadOnlyCollection<TValue>
    {
        #region Fields

        private readonly List<TValue> m_inner;

        #endregion

        #region Constructors

        protected MessagePackSet()
        {
            m_inner = new List<TValue>();
        }

        public MessagePackSet(params TValue[] values)
        {
            m_inner = new List<TValue>(values);
        }

        [SerializationConstructor]
        public MessagePackSet(IEnumerable<TValue> values)
        {
            m_inner = new List<TValue>(values);
        }

        #endregion

        #region Functions

        protected void Append(IEnumerable<TValue> values)
        {
            m_inner.AddRange(values);
        }

        public override string ToString()
        {
            if (Inner == null || Count == 0)
                return "Empty";

            return Inner.ToString();
        }

        public TValue[] ToArray()
        {
            return Inner.ToArray();
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TValue> GetEnumerator()
        {
            return Inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MessagePackSet<TValue> collection))
                return false;

            return Inner.Is(collection.Inner);
        }

        public override ModelBase Clone()
        {
            return new MessagePackSet<TValue>(Inner);
        }

        #endregion

        #region Properties

        [Key(0)] 
        public IReadOnlyCollection<TValue> Inner => m_inner;

        [IgnoreMember]
        public TValue this[int index] => m_inner[index];

        [IgnoreMember]
        public int Count => Inner.Count;

        #endregion

    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.ProtoBuf.Collections
{
    [ProtoContract]
    public class ProtoBufSet<TValue> : ModelBase, ICollection<TValue>
    {
        #region Fields

        [ProtoMember(1)]
        private readonly List<TValue> m_inner;

        #endregion

        #region Constructors

        protected ProtoBufSet()
        {
            m_inner = new List<TValue>();
        }

        public ProtoBufSet(params TValue[] values)
        {
            m_inner = new List<TValue>(values);
        }

        public ProtoBufSet(IEnumerable<TValue> inner)
        {
            m_inner = new List<TValue>(inner);
        }

        #endregion

        #region Functions

        protected void Append(IEnumerable<TValue> values)
        {
            m_inner.AddRange(values);
        }

        public override string ToString()
        {
            if (Count == 0)
                return "Empty";

            return Inner.ToString();
        }

        public TValue[] ToArray()
        {
            return Inner.ToArray();
        }

        #endregion

        #region ICollection

        public void Add(TValue item)
        {
            m_inner.Add(item);
        }

        public void Clear()
        {
            m_inner.Clear();
        }

        public bool Contains(TValue item)
        {
            return m_inner.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            m_inner.CopyTo(array, arrayIndex);
        }

        public bool Remove(TValue item)
        {
            return m_inner.Remove(item);
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
            if (!(modelBase is ProtoBufSet<TValue> collection))
                return false;

            return Inner.Is(collection.Inner);
        }

        public override ModelBase Clone()
        {
            return new ProtoBufSet<TValue>(Inner);
        }

        #endregion

        #region Properties

        [ProtoMember(1)]
        public IReadOnlyCollection<TValue> Inner => m_inner;

        [ProtoIgnore]
        public TValue this[int index] => m_inner[index];

        [ProtoIgnore]
        public int Count => Inner.Count;

        public bool IsReadOnly { get; } = false;

        #endregion

    }
}

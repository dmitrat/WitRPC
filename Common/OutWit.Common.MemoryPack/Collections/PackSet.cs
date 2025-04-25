using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.MemoryPack.Collections
{
    [MemoryPackable]
    public partial class PackSet<TValue> : ModelBase, IReadOnlyCollection<TValue>
    {
        #region Fields

        private readonly List<TValue> m_inner;

        #endregion

        #region Constructors

        protected PackSet()
        {
            m_inner = new List<TValue>();
        }

        public PackSet(params TValue[] values)
        {
            m_inner = new List<TValue>(values);
        }

        [MemoryPackConstructor]
        public PackSet(IEnumerable<TValue> inner)
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
            if (!(modelBase is PackSet<TValue> collection))
                return false;

            return Inner.Is(collection.Inner);
        }

        public override ModelBase Clone()
        {
            return new PackSet<TValue>(Inner);
        }

        #endregion

        #region Properties
 
        public IReadOnlyCollection<TValue> Inner => m_inner;

        [MemoryPackIgnore]
        public TValue this[int index] => m_inner[index];

        [MemoryPackIgnore]
        public int Count => Inner.Count;

        #endregion

    }
}

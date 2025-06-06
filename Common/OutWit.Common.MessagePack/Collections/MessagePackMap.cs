using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.MessagePack.Collections
{
    [MessagePackObject]
    public class MessagePackMap<TKey, TValue> : ModelBase, IReadOnlyDictionary<TKey, TValue>
    {
        #region Fields

        private readonly Dictionary<TKey, TValue> m_inner;

        #endregion

        #region Constructors

        protected MessagePackMap()
        {
        }

        [SerializationConstructor]
        public MessagePackMap(IReadOnlyDictionary<TKey, TValue> inner)
        {
            if(inner is Dictionary<TKey, TValue>)
                m_inner = (Dictionary<TKey, TValue>) inner;
            else
                m_inner = inner.ToDictionary(x => x.Key, x => x.Value);
        }

        #endregion

        #region Functions

        public bool ContainsKey(TKey key)
        {
            return m_inner.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Inner.TryGetValue(key, out value);
        }

        public override string ToString()
        {
            if (Count == 0)
                return "Empty";

            return $"{Inner}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MessagePackMap<TKey, TValue> map))
                return false;

            return Inner.Is(map.Inner);
        }

        public override ModelBase Clone()
        {
            return new MessagePackMap<TKey, TValue>(m_inner);
        }

        #endregion

        #region IEnumerable

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return Inner.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return Inner.GetEnumerator();
        }

        #endregion

        #region Properties

        [Key(0)]
        public IReadOnlyDictionary<TKey, TValue> Inner => m_inner;

        [IgnoreMember] 
        public TValue this[TKey key] => m_inner[key];

        [IgnoreMember]
        public IEnumerable<TKey> Keys => Inner.Keys;

        [IgnoreMember]
        public IEnumerable<TValue> Values => Inner.Values;

        [IgnoreMember]
        public int Count => Inner.Count;

        #endregion
    }
}

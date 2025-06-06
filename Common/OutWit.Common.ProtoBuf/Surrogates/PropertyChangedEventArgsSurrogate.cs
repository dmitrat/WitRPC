using System;
using System.ComponentModel;
using ProtoBuf;

namespace OutWit.Common.ProtoBuf.Surrogates
{
    [ProtoContract(Name = nameof(PropertyChangedEventArgs))]
    internal class PropertyChangedEventArgsSurrogate
    {

        #region Operators

        public static implicit operator PropertyChangedEventArgs(PropertyChangedEventArgsSurrogate s)
        {
            return new PropertyChangedEventArgs(s?.PropertyName);
        }

        public static implicit operator PropertyChangedEventArgsSurrogate(PropertyChangedEventArgs d)
        {
            return new PropertyChangedEventArgsSurrogate
            {
                PropertyName = d?.PropertyName,
                IsNull = d?.PropertyName == null
            };
        }
        
        #endregion

        #region Properties

        [ProtoMember(1)]
        public string PropertyName { get; set; }

        [ProtoMember(2)]
        public bool IsNull { get; set; }

        #endregion
    }
}

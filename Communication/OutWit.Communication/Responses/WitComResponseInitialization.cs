using System;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using ProtoBuf;

namespace OutWit.Communication.Responses
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class WitComResponseInitialization : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitComResponseInitialization request))
                return false;

            return SymmetricKey.Is(request.SymmetricKey) && 
                   Vector.Is(request.Vector);
        }

        public override WitComResponseInitialization Clone()
        {
            return new WitComResponseInitialization
            {
                SymmetricKey = SymmetricKey,
                Vector = Vector
            };
        }

        #endregion

        #region Properties

        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        public byte[]? SymmetricKey { get; set; }

        [Key(1)]
        [DataMember]
        [ProtoMember(2)]
        public byte[]? Vector { get; set; }

        #endregion
    }
}

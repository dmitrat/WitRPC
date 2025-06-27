using System;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using ProtoBuf;

namespace OutWit.Communication.Requests
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class WitRequestInitialization : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitRequestInitialization request))
                return false;

            return PublicKey.Is(request.PublicKey);
        }

        public override WitRequestInitialization Clone()
        {
            return new WitRequestInitialization
            {
                PublicKey = PublicKey?.ToArray()
            };
        }

        #region Properties

        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        public byte[]? PublicKey { get; set; }

        #endregion
    }
}

using System;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using ProtoBuf;

namespace OutWit.Communication.Requests
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
    [ProtoContract]
    public partial class WitRequestInitialization : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitRequestInitialization request))
                return false;

            return PublicKey.Is(request.PublicKey) &&
                   ProtocolVersion.Is(request.ProtocolVersion);
        }

        public override WitRequestInitialization Clone()
        {
            return new WitRequestInitialization
            {
                PublicKey = PublicKey?.ToArray(),
                ProtocolVersion = ProtocolVersion
            };
        }

        #region Properties

        [Key(0)]

        [MemoryPackOrder(0)]
        [DataMember]
        [ProtoMember(1)]
        public byte[]? PublicKey { get; set; }

        /// <summary>
        /// The protocol version the client speaks. A pre-3.0 client cannot send
        /// one, so a missing value reads as 0 and is refused as such.
        /// </summary>
        [Key(1)]
        [MemoryPackOrder(1)]
        [DataMember]
        [ProtoMember(2)]
        public int ProtocolVersion { get; set; }

        #endregion
    }
}

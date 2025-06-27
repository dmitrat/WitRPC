using System;
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
    public partial class WitRequestAuthorization : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitRequestAuthorization request))
                return false;

            return Token.Is(request.Token);
        }

        public override WitRequestAuthorization Clone()
        {
            return new WitRequestAuthorization
            {
                Token = Token
            };
        }

        #region Properties

        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        public string? Token { get; set; }

        #endregion
    }
}

using System;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using ProtoBuf;

namespace OutWit.Communication.Responses
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class WitResponseAuthorization : ModelBase
    {
        #region Constructors

        public WitResponseAuthorization()
        {
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitResponseAuthorization request))
                return false;

            return IsAuthorized.Is(request.IsAuthorized) && 
                   Message.Is(request.Message);
        }

        public override WitResponseAuthorization Clone()
        {
            return new WitResponseAuthorization
            {
                IsAuthorized = IsAuthorized,
                Message = Message
            };
        }

        #endregion

        #region Properties

        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        public bool IsAuthorized { get; set; }

        [Key(1)]
        [DataMember]
        [ProtoMember(2)]
        public string? Message { get; set; }

        #endregion
    }
}

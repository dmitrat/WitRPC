using System;
using System.Runtime.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Responses
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
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


        [MemoryPackOrder(0)]
        [DataMember]
        public bool IsAuthorized { get; set; }


        [MemoryPackOrder(1)]
        [DataMember]
        public string? Message { get; set; }

        #endregion
    }
}

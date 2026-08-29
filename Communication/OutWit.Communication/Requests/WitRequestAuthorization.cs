using System;
using System.Runtime.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Communication.Requests
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
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


        [MemoryPackOrder(0)]
        [DataMember]
        public string? Token { get; set; }

        #endregion
    }
}

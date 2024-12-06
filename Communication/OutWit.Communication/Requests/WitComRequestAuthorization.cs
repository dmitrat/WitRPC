using System;
using System.Runtime.Serialization;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Communication.Requests
{
    [MessagePackObject]
    [DataContract]
    public class WitComRequestAuthorization : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitComRequestAuthorization request))
                return false;

            return Token.Is(request.Token);
        }

        public override WitComRequestAuthorization Clone()
        {
            return new WitComRequestAuthorization
            {
                Token = Token
            };
        }

        #region Properties

        [Key(0)]
        [DataMember]
        public string? Token { get; set; }

        #endregion
    }
}

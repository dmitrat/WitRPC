using System;
using System.Linq;
using System.Runtime.Serialization;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Communication.Requests
{
    [MessagePackObject]
    [DataContract]
    public class WitComRequestInitialization : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitComRequestInitialization request))
                return false;

            return PublicKey.Is(request.PublicKey);
        }

        public override WitComRequestInitialization Clone()
        {
            return new WitComRequestInitialization
            {
                PublicKey = PublicKey?.ToArray()
            };
        }

        #region Properties

        [Key(0)]
        [DataMember]
        public byte[]? PublicKey { get; set; }

        #endregion
    }
}

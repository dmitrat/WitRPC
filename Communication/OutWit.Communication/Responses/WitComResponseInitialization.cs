using System;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Communication.Responses
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
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
        public byte[]? SymmetricKey { get; set; }

        [Key(1)]
        [DataMember]
        public byte[]? Vector { get; set; }

        #endregion
    }
}

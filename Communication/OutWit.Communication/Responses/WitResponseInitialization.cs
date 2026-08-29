using System;
using System.Runtime.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Communication.Responses
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class WitResponseInitialization : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitResponseInitialization request))
                return false;

            return SymmetricKey.Is(request.SymmetricKey) && 
                   Vector.Is(request.Vector) &&
                   ProtocolVersion.Is(request.ProtocolVersion) &&
                   ErrorMessage.Is(request.ErrorMessage);
        }

        public override WitResponseInitialization Clone()
        {
            return new WitResponseInitialization
            {
                SymmetricKey = SymmetricKey,
                Vector = Vector,
                ProtocolVersion = ProtocolVersion,
                ErrorMessage = ErrorMessage
            };
        }

        #endregion

        #region Properties


        [MemoryPackOrder(0)]
        [DataMember]
        public byte[]? SymmetricKey { get; set; }


        [MemoryPackOrder(1)]
        [DataMember]
        public byte[]? Vector { get; set; }

        /// <summary>The protocol version the server speaks.</summary>
        [MemoryPackOrder(2)]
        [DataMember]
        public int ProtocolVersion { get; set; }

        /// <summary>
        /// Set when the server refuses the handshake (a protocol mismatch, or an
        /// initialization request it could not read); the client surfaces it
        /// instead of guessing from a null key.
        /// </summary>
        [MemoryPackOrder(3)]
        [DataMember]
        public string? ErrorMessage { get; set; }

        #endregion
    }
}

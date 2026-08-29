using System;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using ProtoBuf;

namespace OutWit.Communication.Responses
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
    [ProtoContract]
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

        [Key(0)]

        [MemoryPackOrder(0)]
        [DataMember]
        [ProtoMember(1)]
        public byte[]? SymmetricKey { get; set; }

        [Key(1)]

        [MemoryPackOrder(1)]
        [DataMember]
        [ProtoMember(2)]
        public byte[]? Vector { get; set; }

        /// <summary>The protocol version the server speaks.</summary>
        [Key(2)]
        [MemoryPackOrder(2)]
        [DataMember]
        [ProtoMember(3)]
        public int ProtocolVersion { get; set; }

        /// <summary>
        /// Set when the server refuses the handshake (a protocol mismatch, or an
        /// initialization request it could not read); the client surfaces it
        /// instead of guessing from a null key.
        /// </summary>
        [Key(3)]
        [MemoryPackOrder(3)]
        [DataMember]
        [ProtoMember(4)]
        public string? ErrorMessage { get; set; }

        #endregion
    }
}

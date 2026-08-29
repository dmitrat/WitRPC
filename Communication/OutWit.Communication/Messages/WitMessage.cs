using System;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Communication.Messages
{
    /// <summary>
    /// The transport envelope: an id, a kind, and an opaque payload. Its binary
    /// layout is FROZEN -- it is deliberately not version-tolerant, so that any
    /// build can at least read the envelope of any other and answer with a
    /// readable refusal. Never add members here; protocol evolution happens in
    /// the payload models, which are version-tolerant.
    /// </summary>
    [DataContract]
    [MemoryPackable]
    public partial class WitMessage: ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Type: {Type}, Id: {Id}";
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitMessage message))
                return false;

            return Id.Is(message.Id) &&
                   Type.Is(message.Type) &&
                   Data.Is(message.Data);
        }

        public override WitMessage Clone()
        {
            return new WitMessage
            {
                Id = Id,
                Type = Type,
                Data = Data?.ToArray()
            };
        }

        #endregion

        #region Properties

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public WitMessageType Type { get; set; }

        [DataMember]
        public byte[]? Data { get; set; }

        #endregion
    }

    public enum WitMessageType
    {
        Unknown = 0,
        Request = 1,
        Callback = 2,
        Initialization = 3,
        Authorization = 4,
    }
}

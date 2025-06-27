using System;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using ProtoBuf;

namespace OutWit.Communication.Messages
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
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
        [Key(0)]
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [DataMember]
        [Key(1)]
        [ProtoMember(2)]
        public WitMessageType Type { get; set; }

        [DataMember]
        [Key(2)]
        [ProtoMember(3)]
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

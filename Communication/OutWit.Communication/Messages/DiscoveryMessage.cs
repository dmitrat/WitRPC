using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Communication.Messages
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class DiscoveryMessage : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"ID: {ServiceId}, {Timestamp}, Type: {Type}, Transport: {Transport}, ServiceName: {ServiceName}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not DiscoveryMessage message)
                return false;

            return (Timestamp?.UtcDateTime).Is(message.Timestamp?.UtcDateTime) &&
                   ServiceId.Is(message.ServiceId) &&
                   Type.Is(message.Type) &&
                   ServiceName.Is(message.ServiceName) &&
                   ServiceDescription.Is(message.ServiceDescription) &&
                   Transport.Is(message.Transport) &&
                   Data.Is(message.Data);
        }

        public override DiscoveryMessage Clone()
        {
            return new DiscoveryMessage
            {
                ServiceId = ServiceId,
                Timestamp = Timestamp,
                Type = Type,
                ServiceName = ServiceName,
                ServiceDescription = ServiceDescription,
                Transport = Transport,
                Data = Data?.ToDictionary(x => x.Key, x => x.Value)
            };
        }

        #endregion

        #region Properties


        [MemoryPackOrder(0)]
        [DataMember]
        public Guid? ServiceId { get; set; }


        [MemoryPackOrder(1)]
        [DataMember]
        public DateTimeOffset? Timestamp { get; set; }
        
        
        [MemoryPackOrder(2)]
        [DataMember]
        public DiscoveryMessageType? Type { get; set; }


        [MemoryPackOrder(3)]
        [DataMember]
        public string? ServiceName { get; set; }


        [MemoryPackOrder(4)]
        [DataMember]
        public string? ServiceDescription { get; set; }


        [MemoryPackOrder(5)]
        [DataMember]
        public string? Transport { get; set; }


        [MemoryPackOrder(6)]
        [DataMember]
        public Dictionary<string, string>? Data { get; set; }

        #endregion

    }

    public enum DiscoveryMessageType
    {
        Hello= 0,
        Heartbeat,
        Goodbye
    }
}

using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Common.MessagePack;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.MemoryPack;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerMemoryPack : IMessageSerializer
    {
        #region IMessageSerializer

        public byte[] Serialize(object message, Type type, ILogger? logger = null)
        {
            return message.ToMemoryPackBytes(logger: logger) ?? Array.Empty<byte>();
        }

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T: class
        {
            return bytes.FromMemoryPackBytes<T>(logger: logger);
        }

        public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
        {
            return bytes.Length == 0
                ? null
                : bytes.FromMemoryPackBytes(type, logger: logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToMemoryPackBytes(logger: logger) ?? Array.Empty<byte>();
        }

        #endregion
    }

}

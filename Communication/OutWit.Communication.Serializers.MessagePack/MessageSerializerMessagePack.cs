using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Common.MessagePack;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerMessagePack : IMessageSerializer
    {
        #region IMessageSerializer

        public byte[] Serialize(object message, Type type, ILogger? logger = null)
        {
            return message.ToMessagePackBytes(type, logger: logger) ?? Array.Empty<byte>();
        }

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T: class
        {
            return bytes.FromMessagePackBytes<T>(logger: logger);
        }

        public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
        {
            return bytes.Length == 0
                ? null
                : bytes.FromMessagePackBytes(type, logger: logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToMessagePackBytes(logger: logger) ?? Array.Empty<byte>();
        }

        #endregion
    }

}

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
            return message.ToPackBytes(logger: logger);
        }

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T: class
        {
            return bytes.FromPackBytes<T>(logger: logger);
        }

        public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
        {
            return bytes == null || bytes.Length == 0
                ? null
                : bytes.FromPackBytes(type, logger: logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToPackBytes(logger: logger);
        }

        #endregion
    }

}

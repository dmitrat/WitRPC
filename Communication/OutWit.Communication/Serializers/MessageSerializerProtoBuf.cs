using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Common.ProtoBuf;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerProtoBuf : IMessageSerializer
    {
        #region IMessageSerializer

        public byte[] Serialize(object message, Type type, ILogger? logger = null)
        {
            return message.ToProtoBytes(type, logger: logger) ?? Array.Empty<byte>();
        }

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T: class
        {
            return bytes.FromProtoBytes<T>(logger: logger);
        }

        public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
        {
            return bytes.Length == 0
                ? null
                : bytes.FromProtoBytes(type, logger: logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToProtoBytes(logger: logger) ?? Array.Empty<byte>();
        }

        #endregion
    }

}

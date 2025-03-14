using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Common.MessagePack;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerMessagePack : IMessageSerializer
    {
        #region IMessageSerializer

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T: class
        {
            return bytes.FromPackBytes<T>(logger: logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToPackBytes(logger: logger);
        }

        #endregion
    }

}

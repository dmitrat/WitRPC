using System;
using OutWit.Communication.Interfaces;
using OutWit.Common.MessagePack;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerMessagePack : IMessageSerializer
    {
        #region IMessageSerializer

        public T? Deserialize<T>(byte[] bytes) where T: class
        {
            return bytes.FromPackBytes<T>();
        }

        public byte[] Serialize<T>(T message) where T : class
        {
            return message.ToPackBytes();
        }

        #endregion
    }

}

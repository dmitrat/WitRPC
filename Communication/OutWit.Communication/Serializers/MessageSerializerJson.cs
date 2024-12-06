using System;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerJson : IMessageSerializer
    {
        #region IMessageSerializer

        public T? Deserialize<T>(byte[] bytes) where T : class
        {
            return bytes.FromJsonBytes<T>();
        }

        public byte[] Serialize<T>(T message) where T : class
        {
            return message.ToJsonBytes();
        }

        #endregion

    }

}

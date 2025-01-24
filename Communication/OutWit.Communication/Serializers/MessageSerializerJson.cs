using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Serializers
{
    public class MessageSerializerJson : IMessageSerializer
    {
        #region IMessageSerializer

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T : class
        {
            return bytes.FromJsonBytes<T>(logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToJsonBytes(logger);
        }

        #endregion

    }

}

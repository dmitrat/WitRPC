using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OutWit.Common.Json;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Serializers
{
    [JsonSerializable(typeof(WitComMessage))]
    [JsonSerializable(typeof(DiscoveryMessage))]

    [JsonSerializable(typeof(HostInfo))]
    [JsonSerializable(typeof(ParameterType))]
    [JsonSerializable(typeof(CommunicationStatus))]

    [JsonSerializable(typeof(WitComRequest))]
    [JsonSerializable(typeof(WitComRequestAuthorization))]
    [JsonSerializable(typeof(WitComRequestInitialization))]

    [JsonSerializable(typeof(WitComResponse))]
    [JsonSerializable(typeof(WitComResponseAuthorization))]
    [JsonSerializable(typeof(WitComResponseInitialization))]
    internal partial class MessageSerializerJsonContext : JsonSerializerContext
    {
    }
    
    public class MessageSerializerJson : IMessageSerializer
    {
        #region Static

        static MessageSerializerJson()
        {
            JsonUtils.AddContext(new MessageSerializerJsonContext());
        }

        #endregion

        #region IMessageSerializer

        public byte[] Serialize(object message, Type type, ILogger? logger = null)
        {
            return message.ToJsonBytes(type, logger) ?? Array.Empty<byte>();
        }

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T : class
        {
            return bytes.FromJsonBytes<T>(logger);
        }

        public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
        {
            return bytes.FromJsonBytes(type, logger: logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            return message.ToJsonBytes(logger) ?? Array.Empty<byte>();
        }

        #endregion
    }

}

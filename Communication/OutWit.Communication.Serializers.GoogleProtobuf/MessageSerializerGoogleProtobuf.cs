using System;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Serializers.GoogleProtobuf
{
    /// <summary>
    /// Serializes protoc-generated messages (<see cref="IMessage"/>) as
    /// protobuf wire bytes, exactly as gRPC would, and hands every other type
    /// (primitives, enums, <c>Guid</c>, plain DTOs) to a fallback serializer --
    /// JSON unless another is supplied. A service migrating from proto-first
    /// gRPC keeps its generated models untouched; the rest of the signature
    /// keeps working.
    /// </summary>
    public class MessageSerializerGoogleProtobuf : IMessageSerializer
    {
        #region Constructors

        public MessageSerializerGoogleProtobuf()
            : this(new MessageSerializerJson())
        {
        }

        public MessageSerializerGoogleProtobuf(IMessageSerializer fallback)
        {
            Fallback = fallback;
        }

        #endregion

        #region IMessageSerializer

        public byte[] Serialize(object message, Type type, ILogger? logger = null)
        {
            if (message is IMessage protoMessage)
                return protoMessage.ToByteArray();

            return Fallback.Serialize(message, type, logger);
        }

        public byte[] Serialize<T>(T message, ILogger? logger = null) where T : class
        {
            if (message is IMessage protoMessage)
                return protoMessage.ToByteArray();

            return Fallback.Serialize(message, logger);
        }

        public T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T : class
        {
            if (!IsProtoMessage(typeof(T)))
                return Fallback.Deserialize<T>(bytes, logger);

            return (T?)Parse(bytes, typeof(T), logger);
        }

        public object? Deserialize(byte[] bytes, Type type, ILogger? logger = null)
        {
            if (!IsProtoMessage(type))
                return Fallback.Deserialize(bytes, type, logger);

            return Parse(bytes, type, logger);
        }

        #endregion

        #region Tools

        private static bool IsProtoMessage(Type type)
        {
            return typeof(IMessage).IsAssignableFrom(type);
        }

        /// <summary>
        /// Every protoc-generated message has a public parameterless
        /// constructor and merges from wire bytes; this avoids reaching for the
        /// static <c>Parser</c> through reflection.
        /// </summary>
        private static object? Parse(byte[] bytes, Type type, ILogger? logger)
        {
            if (bytes.Length == 0)
                return null;

            try
            {
                var message = (IMessage)Activator.CreateInstance(type)!;
                message.MergeFrom(bytes);
                return message;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to parse a Google.Protobuf message of type {Type}", type.FullName);
                return null;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The serializer used for everything that is not an <see cref="IMessage"/>.
        /// </summary>
        public IMessageSerializer Fallback { get; }

        #endregion
    }
}

using System;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using OutWit.Common.MessagePack.Formatters;

namespace OutWit.Common.MessagePack
{
    public static class MessagePackUtils
    {
        #region Constructors

        static MessagePackUtils()
        {
            PlainOption = MessagePackSerializerOptions.Standard.WithResolver(MessagePackResolver.Instance);
            
            CompressionOption = PlainOption.WithCompression(MessagePackCompression.Lz4Block);

            Register<TypeFormatter>();
            Register<PropertyChangedEventArgsFormatter>();

            Register(StandardResolver.Instance);
        }

        #endregion

        #region Registration

        public static void Register<TFormatter>() 
            where TFormatter: IMessagePackFormatter, new()
        {
            MessagePackResolver.Instance.Register<TFormatter>();
        }

        public static void Register(IFormatterResolver resolver)
        {
            MessagePackResolver.Instance.Register(resolver);
        }

        #endregion

        #region Serialize

        public static byte[]? ToMessagePackBytes<TObject>(this TObject me, bool withCompression = true, ILogger? logger = null)
        {
            try
            {
                return withCompression
                    ? MessagePackSerializer.Serialize(me, CompressionOption)
                    : MessagePackSerializer.Serialize(me, PlainOption);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to MessagePack");
                return null;
            }

        }

        public static byte[]? ToMessagePackBytes(this object? me, Type type, bool withCompression = true, ILogger? logger = null)
        {
            try
            {
                return withCompression
                    ? MessagePackSerializer.Serialize(type, me, CompressionOption)
                    : MessagePackSerializer.Serialize(type, me, PlainOption);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to MessagePack");
                return null;
            }

        }

        #endregion

        #region Deserizlize

        public static TObject? FromMessagePackBytes<TObject>(this byte[] me, bool withCompression = true, ILogger? logger = null)
        {
            return ((ReadOnlyMemory<byte>)me.AsMemory()).FromMessagePackBytes<TObject>(withCompression, logger);
        }


        public static TObject? FromMessagePackBytes<TObject>(this ReadOnlyMemory<byte> me, bool withCompression = true, ILogger? logger = null)
        {
            try
            {
                return withCompression
                    ? MessagePackSerializer.Deserialize<TObject>(me, CompressionOption)
                    : MessagePackSerializer.Deserialize<TObject>(me, PlainOption);

            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from MessagePack");
                return default;
            }
        }

        public static object? FromMessagePackBytes(this byte[] me, Type type, bool withCompression = true, ILogger? logger = null)
        {
            return ((ReadOnlyMemory<byte>)me.AsMemory()).FromMessagePackBytes(type, withCompression, logger);
        }

        public static object? FromMessagePackBytes(this ReadOnlyMemory<byte> me, Type type, bool withCompression = true, ILogger? logger = null)
        {
            try
            {
                return withCompression
                    ? MessagePackSerializer.Deserialize(type, me, CompressionOption)
                    : MessagePackSerializer.Deserialize(type, me, PlainOption);

            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from MessagePack");
                return type.IsValueType
                    ? Activator.CreateInstance(type)
                    : null;
            }
        }

        #endregion

        #region Clone

        public static TObject? MessagePackClone<TObject>(this TObject me, bool withCompression = true, ILogger? logger = null)
        {
            byte[]? bytes = me.ToMessagePackBytes(withCompression, logger);
            return bytes == null
                ? default(TObject)
                : bytes.FromMessagePackBytes<TObject>(withCompression, logger);
        }

        #endregion

        #region Export
        
        public static async Task ExportAsMessagePackAsync<T>(this IEnumerable<T> me, string filePath)
        {
            byte[]? bytes = me.ToArray().ToMessagePackBytes();
            if (bytes != null)
                await File.WriteAllBytesAsync(filePath, bytes);
        }
        
        public static void ExportAsMessagePack<T>(this IEnumerable<T> me, string filePath)
        {
            byte[]? bytes = me.ToArray().ToMessagePackBytes();
            if (bytes != null)
                File.WriteAllBytes(filePath, bytes);
        }

        #endregion

        #region Load

        public static async Task<IReadOnlyList<T>?> LoadAsMessagePackAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            return bytes.FromMessagePackBytes<IReadOnlyList<T>>();
        }

        public static IReadOnlyList<T>? LoadAsMessagePack<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = File.ReadAllBytes(filePath);

            return bytes.FromMessagePackBytes<IReadOnlyList<T>>();
        }

        #endregion

        #region Properties

        private static MessagePackSerializerOptions CompressionOption { get; }

        private static MessagePackSerializerOptions PlainOption { get; } 
        
        #endregion

    }
}

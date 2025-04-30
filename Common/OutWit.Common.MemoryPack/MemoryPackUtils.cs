using System;
using MemoryPack;
using Microsoft.Extensions.Logging;
using OutWit.Common.MemoryPack.Formatters;

namespace OutWit.Common.MemoryPack
{
    public static class MemoryPackUtils
    {
        #region Constructors

        static MemoryPackUtils()
        {
            Register(new PropertyChangedEventArgsFormatter());
        }

        #endregion

        #region Registration

        public static void Register<T>(MemoryPackFormatter<T> formatter)
        {
            MemoryPackFormatterProvider.Register(formatter);
        }

        #endregion

        #region Serialization

        public static byte[]? ToMemoryPackBytes<TObject>(this TObject me, StringEncoding encoding = StringEncoding.Utf8, ILogger? logger = null)
        {
            try
            {
                return MemoryPackSerializer.Serialize(me, new MemoryPackSerializerOptions { StringEncoding = encoding });
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to MessagePack");
                return null;
            }
        }

        public static byte[]? ToMemoryPackBytes(this object? me, Type type, StringEncoding encoding = StringEncoding.Utf8, ILogger? logger = null)
        {
            try
            {
                return MemoryPackSerializer.Serialize(type, me, new MemoryPackSerializerOptions { StringEncoding = encoding });
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to MessagePack");
                return null;
            }
        }

        #endregion

        #region Deserealization

        public static TObject? FromMemoryPackBytes<TObject>(this byte[] me, ILogger? logger = null)
        {
            return ((ReadOnlySpan<byte>)me.AsSpan()).FromMemoryPackBytes<TObject>(logger);
        }

        public static TObject? FromMemoryPackBytes<TObject>(this ReadOnlySpan<byte> me, ILogger? logger = null)
        {
            try
            {
                return MemoryPackSerializer.Deserialize<TObject>(me);

            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from MessagePack");
                return default(TObject);
            }
        }

        public static object? FromMemoryPackBytes(this byte[] me, Type type, ILogger? logger = null)
        {
            return ((ReadOnlySpan<byte>)me.AsSpan()).FromMemoryPackBytes(type, logger);
        }
        
        public static object? FromMemoryPackBytes(this ReadOnlySpan<byte> me, Type type, ILogger? logger = null)
        {
            try
            {
                return MemoryPackSerializer.Deserialize(type, me);

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

        public static TObject? MemoryPackClone<TObject>(this TObject me, ILogger? logger = null)
        {
            var bytes = me.ToMemoryPackBytes(logger: logger);
            return bytes == null
                ? default(TObject)
                : ((ReadOnlySpan<byte>)bytes.AsSpan()).FromMemoryPackBytes<TObject>(logger: logger);
        }

        #endregion

        #region Export

        public static async Task ExportAsMemoryPackAsync<T>(this IEnumerable<T> me, string filePath)
        {
            byte[]? bytes = me.ToArray().ToMemoryPackBytes();
            if (bytes != null)
                await File.WriteAllBytesAsync(filePath, bytes);
        }

        public static void ExportAsMemoryPack<T>(this IEnumerable<T> me, string filePath)
        {
            byte[]? bytes = me.ToArray().ToMemoryPackBytes();
            if (bytes != null)
                File.WriteAllBytes(filePath, bytes);
        }

        #endregion

        #region Load

        public static async Task<IReadOnlyList<T>?> LoadAsMemoryPackAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            return bytes.FromMemoryPackBytes<IReadOnlyList<T>>();
        }

        public static IReadOnlyList<T>? LoadAsMemoryPack<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = File.ReadAllBytes(filePath);

            return bytes.FromMemoryPackBytes<IReadOnlyList<T>>();
        }

        #endregion

    }
}

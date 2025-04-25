using System;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace OutWit.Common.MemoryPack
{
    public static class MemoryPackUtils
    {
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

        public static TObject? FromMemoryPackBytes<TObject>(this byte[] me, ILogger? logger = null)
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

        public static TObject? MemoryPackClone<TObject>(this TObject me, ILogger? logger = null)
        {
            var bytes = me.ToMemoryPackBytes(logger: logger);
            return bytes == null
                ? default(TObject)
                : bytes.FromMemoryPackBytes<TObject>(logger: logger);
        }

    }
}

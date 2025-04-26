using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OutWit.Common.Json.Converters;

namespace OutWit.Common.Json
{
    public static class JsonUtils
    {
        #region Constructors

        static JsonUtils()
        {
            Context = new JsonContextMerged();

            OptionsDefault = new JsonSerializerOptions
            {
                Converters =
                {
                    new TypeJsonConverter(),
                    new RSAParametersJsonConverter()
                },
                WriteIndented = false,
                TypeInfoResolver = Context
            };

            OptionsIndented = new JsonSerializerOptions
            {
                Converters =
                {
                    new TypeJsonConverter(),
                    new RSAParametersJsonConverter()
                },
                WriteIndented = true,
                TypeInfoResolver = Context
            };

            AddContext(new JsonContextDefault());
        }

        #endregion

        #region Context

        public static void AddContext(JsonSerializerContext context)
        {
            Context.AddContext(context);
        }

        #endregion

        #region Json

        public static string? ToJsonString<TObject>(this TObject me, bool indented = false, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.Serialize(me, indented? OptionsIndented : OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to string");
                return null;
            }
        }

        public static string? ToJsonString(this object? me, Type type, bool indented = false, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.Serialize(me, type, indented ? OptionsIndented : OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to json string");
                return null;
            }
        }

        public static byte[]? ToJsonBytes<TObject>(this TObject me, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(me, OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to json string");
                return null;
            }
        }

        public static byte[]? ToJsonBytes(this object? me, Type type, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(me, type, OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to serialize to json string");
                return null;
            }
        }

        public static TObject? FromJsonString<TObject>(this string me, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.Deserialize<TObject>(me, OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from json string");
                return default;
            }
        }

        public static object? FromJsonString(this string me, Type type, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.Deserialize(me, type, OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from json string");
                return null;
            }
        }

        public static TObject? FromJsonBytes<TObject>(this byte[] me, ILogger? logger = null)
        {
            return ((ReadOnlySpan<byte>)me.AsSpan()).FromJsonBytes<TObject>(logger);
        }

        public static TObject? FromJsonBytes<TObject>(this ReadOnlySpan<byte> me, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.Deserialize<TObject>(me, OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from json bytes");
                return default;
            }
        }

        public static object? FromJsonBytes(this byte[] me, Type type, ILogger? logger = null)
        {
            return ((ReadOnlySpan<byte>)me.AsSpan()).FromJsonBytes(type, logger);
        }

        public static object? FromJsonBytes(this ReadOnlySpan<byte> me, Type type, ILogger? logger = null)
        {
            try
            {
                return JsonSerializer.Deserialize(me, type, OptionsDefault);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to deserialize from json bytes");
                return null;
            }
        }

        public static TObject? JsonClone<TObject>(this TObject me)
            where TObject : class
        {
            return me.ToJsonBytes()?.FromJsonBytes<TObject>();
        }

        #endregion

        #region MyRegion
        
        private static JsonContextMerged Context { get; }

        private static JsonSerializerOptions OptionsDefault { get; }
        
        private static JsonSerializerOptions OptionsIndented { get; }

        #endregion


    }
}

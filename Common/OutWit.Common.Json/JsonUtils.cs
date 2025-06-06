using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

            Register(new JsonContextDefault());
        }

        #endregion

        #region Registration

        public static void Register(JsonSerializerContext context)
        {
            Context.AddContext(context);
        }

        public static void Register(Action<JsonOptions> optionsBuilder)
        {
            var options = new JsonOptions();
            optionsBuilder(options);
            
            if(options.Contexts.Count == 0)
                return;
            
            foreach (var context in options.Contexts)
                Register(context);
        }

        #endregion

        #region String Serialization

        public static string ToJsonString<TObject>(this TObject me, bool indented = false, ILogger logger = null)
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

        public static string ToJsonString(this object me, Type type, bool indented = false, ILogger logger = null)
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

        #endregion

        #region Bytes Serialization

        public static byte[] ToJsonBytes<TObject>(this TObject me, ILogger logger = null)
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

        public static byte[] ToJsonBytes(this object me, Type type, ILogger logger = null)
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

        #endregion

        #region String Deserealization

        public static TObject FromJsonString<TObject>(this string me, ILogger logger = null)
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

        public static object FromJsonString(this string me, Type type, ILogger logger = null)
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

        #endregion

        #region Bytes Deserealization

        public static TObject FromJsonBytes<TObject>(this byte[] me, ILogger logger = null)
        {
            return ((ReadOnlySpan<byte>)me.AsSpan()).FromJsonBytes<TObject>(logger);
        }

        public static TObject FromJsonBytes<TObject>(this ReadOnlySpan<byte> me, ILogger logger = null)
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

        public static object FromJsonBytes(this byte[] me, Type type, ILogger logger = null)
        {
            return ((ReadOnlySpan<byte>)me.AsSpan()).FromJsonBytes(type, logger);
        }

        public static object FromJsonBytes(this ReadOnlySpan<byte> me, Type type, ILogger logger = null)
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

        #endregion

        #region Clone

        public static TObject JsonClone<TObject>(this TObject me)
            where TObject : class
        {
            return me.ToJsonBytes()?.FromJsonBytes<TObject>();
        }

        #endregion

        #region Export

#if NET6_0_OR_GREATER
        
        public static async Task ExportAsJsonBytesAsync<T>(this IEnumerable<T> me, string filePath)
        {
            byte[] bytes = me.ToArray().ToJsonBytes();
            if (bytes != null)
                await File.WriteAllBytesAsync(filePath, bytes);
        }
        
#endif
        public static void ExportAsJsonBytes<T>(this IEnumerable<T> me, string filePath)
        {
            byte[] bytes = me.ToArray().ToJsonBytes();
            if (bytes != null)
                File.WriteAllBytes(filePath, bytes);
        }
        
#if NET6_0_OR_GREATER
        
        public static async Task ExportAsJsonStringAsync<T>(this IEnumerable<T> me, string filePath)
        {
            string json = me.ToArray().ToJsonString(indented: true);
            if (!string.IsNullOrEmpty(json))
                await File.WriteAllTextAsync(filePath, json);
        }
#endif
        public static void ExportAsJsonString<T>(this IEnumerable<T> me, string filePath)
        {
            string json = me.ToArray().ToJsonString(indented: true);
            if (!string.IsNullOrEmpty(json))
                File.WriteAllText(filePath, json);
        }

        #endregion

        #region Load
        
#if NET6_0_OR_GREATER
        public static async Task<IReadOnlyList<T>> LoadAsJsonBytesAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            return bytes.FromJsonBytes<IReadOnlyList<T>>();
        }
#endif
        public static IReadOnlyList<T> LoadAsJsonBytes<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = File.ReadAllBytes(filePath);

            return bytes.FromJsonBytes<IReadOnlyList<T>>();
        }
#if NET6_0_OR_GREATER
        public static async Task<IReadOnlyList<T>> LoadAsJsonStringAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            string json = await File.ReadAllTextAsync(filePath);

            return json.FromJsonString<IReadOnlyList<T>>();
        }
#endif   
        public static IReadOnlyList<T> LoadAsJsonString<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);

            return json.FromJsonString<IReadOnlyList<T>>();
        }

        #endregion

        #region Properties

        private static JsonContextMerged Context { get; }

        private static JsonSerializerOptions OptionsDefault { get; }
        
        private static JsonSerializerOptions OptionsIndented { get; }

        #endregion


    }
}

using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using OutWit.Common.ProtoBuf.Surrogates;
using ProtoBuf.Meta;

namespace OutWit.Common.ProtoBuf
{
    public static class ProtoBufUtils
    {
        #region Constructors

        static ProtoBufUtils()
        {
            Model = RuntimeTypeModel.Create();
            Model.AutoAddMissingTypes = true;
            
            RegisterSurrogate<DateTimeOffset, DateTimeOffsetSurrogate>();
            RegisterSurrogate<DateTimeOffset?, DateTimeOffsetSurrogate>();
            
            RegisterSurrogate<PropertyChangedEventArgs?, PropertyChangedEventArgsSurrogate>();
        }

        #endregion

        #region Registration

        public static void Register(Action<ProtoBufOptions> optionsBuilder)
        {
            var options = new ProtoBufOptions();
            optionsBuilder(options);
        }

        public static void RegisterSurrogate<TObject, TSurrogate>()
        {
            Model
                .Add(typeof(TObject), false)
                .SetSurrogate(typeof(TSurrogate));
        }

        public static void CompileModel()
        {
            Model.CompileInPlace();
        }

        #endregion

        #region Serialization

        public static byte[]? ToProtoBytes<T>(this T obj, ILogger? logger = null)
        {
            try
            {
                using var ms = new MemoryStream();

                Model.Serialize(ms, obj);

                return ms.ToArray();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Protobuf serialize failed");
                return null;
            }
        }

        public static byte[]? ToProtoBytes(this object obj, Type type, ILogger? logger = null)
        {
            try
            {
                using var ms = new MemoryStream();

                Model.Serialize(ms, obj, type);

                return ms.ToArray();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Protobuf serialize failed");
                return null;
            } 
           
        }

        #endregion

        #region Deserealization

        public static TObject? FromProtoBytes<TObject>(this byte[] data, ILogger? logger = null)
        {
            try
            {
                if (data.Length == 0)
                    return default;
                
                using var ms = new MemoryStream(data);
                return (TObject?)Model.Deserialize(ms, null, typeof(TObject));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Protobuf deserialize failed");
                return default;
            }
        }

        public static object? FromProtoBytes(this byte[] data, Type type, ILogger? logger = null)
        {
            try
            {
                if (data.Length == 0)
                    return null;
                
                using var ms = new MemoryStream(data);
                return Model.Deserialize(ms, null, type);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Protobuf deserialize failed");
                return type.IsValueType 
                    ? Activator.CreateInstance(type)! 
                    : null;
            }
        }

        #endregion

        #region Clone

        public static TObject? ProtoClone<TObject>(this TObject obj, ILogger? logger = null)
            where TObject : class
        {
            return obj.ToProtoBytes(logger: logger)?.FromProtoBytes<TObject>(logger: logger);
        }

        #endregion

        #region Export

        public static async Task ExportAsProtoBufAsync<T>(this IEnumerable<T> me, string filePath)
        {
            byte[]? bytes = me.ToArray().ToProtoBytes();
            if (bytes != null)
                await File.WriteAllBytesAsync(filePath, bytes);
        }

        public static void ExportAsProtoBuf<T>(this IEnumerable<T> me, string filePath)
        {
            byte[]? bytes = me.ToArray().ToProtoBytes();
            if (bytes != null)
                File.WriteAllBytes(filePath, bytes);
        }

        #endregion

        #region Load

        public static async Task<IReadOnlyList<T>?> LoadAsProtoBufAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            return bytes.FromProtoBytes<IReadOnlyList<T>>();
        }

        public static IReadOnlyList<T>? LoadAsProtoBuf<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = File.ReadAllBytes(filePath);

            return bytes.FromProtoBytes<IReadOnlyList<T>>();
        }

        #endregion
        
        #region Properties

        private static RuntimeTypeModel Model { get; }

        #endregion

    }
}

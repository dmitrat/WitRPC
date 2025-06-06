using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Json.Converters
{
    internal class TypeJsonConverter : JsonConverter<Type>
    {
        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                string typeName = reader.GetString();
                if (string.IsNullOrEmpty(typeName))
                    return null;
                try
                {
                    return Type.GetType(typeName, throwOnError: true, ignoreCase: false);
                }
                catch (Exception ex)
                {
                    throw new JsonException($"Failed to load type '{typeName}'. See inner exception for details.", ex);
                }
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Type.");
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.AssemblyQualifiedName);
            
        }
    }
}

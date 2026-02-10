using OutWit.Communication.Client.Blazor.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Communication.Client.Blazor.Encryption
{
    /// <summary>
    /// JSON converter that handles RSA parameters conversion between JWK format and server format.
    /// - Read: Deserializes JWK format (n, e, etc.) using JsonPropertyName attributes
    /// - Write: Serializes using C# property names (mod, exp, etc.) with Base64Url to Base64 conversion
    /// </summary>
    internal sealed class DualNameJsonConverter : JsonConverter<RSAParametersWeb>
    {
        public override RSAParametersWeb? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<RSAParametersWeb>(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, RSAParametersWeb value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var property in typeof(RSAParametersWeb).GetProperties())
            {
                if (property.CanRead)
                {
                    var propertyValue = AdjustBase64(property.GetValue(value));
                    writer.WritePropertyName(property.Name);
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }

        private object? AdjustBase64(object? value)
        {
            if (value == null)
                return value;

            if (value is not string urlString)
                return value;

            if (string.IsNullOrEmpty(urlString))
                return value;

            return urlString.Base64UrlToBase64();

        }
    }
}

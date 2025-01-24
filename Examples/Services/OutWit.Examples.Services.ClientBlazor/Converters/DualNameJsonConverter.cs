using System.Text.Json;
using System.Text.Json.Serialization;
using OutWit.Examples.Services.ClientBlazor.Utils;

namespace OutWit.Examples.Services.ClientBlazor.Converters
{
    public class DualNameJsonConverter<T> : JsonConverter<T> where T : new()
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var property in typeof(T).GetProperties())
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

            if(value is not string urlString)
                return value;

            if (string.IsNullOrEmpty(urlString))
                return value;

            return urlString.Base64UrlToBase64();

        }
    }
}

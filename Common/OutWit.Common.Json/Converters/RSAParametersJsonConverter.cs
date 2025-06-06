using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Json.Converters
{
    internal class RSAParametersJsonConverter : JsonConverter<RSAParameters>
    {
        #region Constants

        private const string MODULUS_PROP = "mod";
        
        private const string EXPONENT_PROP = "exp";
        
        private const string D_PROP = "d";
        
        private const string P_PROP = "p";
        
        private const string Q_PROP = "q";
        
        private const string DP_PROP = "dp";
        
        private const string DQ_PROP = "dq";
        
        private const string INVERSE_Q_PROP = "iq";

        #endregion

        public override RSAParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of RSA parameters object.");
            

            var rsaParams = new RSAParameters();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return rsaParams;
                

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name in RSA parameters object.");
                

                string propertyName = reader.GetString();
                reader.Read(); 

                byte[] value = null;
                if (reader.TokenType == JsonTokenType.String)
                {
                    try
                    {
                        value = Convert.FromBase64String(reader.GetString()!);
                    }
                    catch (FormatException ex)
                    {
                        throw new JsonException($"Invalid Base64 string for property '{propertyName}'.", ex);
                    }
                }
                else if (reader.TokenType != JsonTokenType.Null)
                {
                    throw new JsonException($"Expected Base64 string or null for property '{propertyName}'.");
                }

                switch (propertyName)
                {
                    case MODULUS_PROP: rsaParams.Modulus = value; break;
                    case EXPONENT_PROP: rsaParams.Exponent = value; break;
                    case D_PROP: rsaParams.D = value; break; 
                    case P_PROP: rsaParams.P = value; break; 
                    case Q_PROP: rsaParams.Q = value; break; 
                    case DP_PROP: rsaParams.DP = value; break; 
                    case DQ_PROP: rsaParams.DQ = value; break; 
                    case INVERSE_Q_PROP: rsaParams.InverseQ = value; break; 
                }
            }

            throw new JsonException("Unexpected end of JSON while reading RSA parameters object.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            RSAParameters value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            WriteBase64Property(writer, MODULUS_PROP, value.Modulus);
            WriteBase64Property(writer, EXPONENT_PROP, value.Exponent);

            WriteBase64Property(writer, D_PROP, value.D);
            WriteBase64Property(writer, P_PROP, value.P);
            WriteBase64Property(writer, Q_PROP, value.Q);
            WriteBase64Property(writer, DP_PROP, value.DP);
            WriteBase64Property(writer, DQ_PROP, value.DQ);
            WriteBase64Property(writer, INVERSE_Q_PROP, value.InverseQ);

            writer.WriteEndObject();
        }

        private void WriteBase64Property(Utf8JsonWriter writer, string propertyName, byte[] value)
        {
            if (value == null || value.Length <= 0) 
                return;
            
            writer.WritePropertyName(propertyName);
            writer.WriteStringValue(Convert.ToBase64String(value));
        }
    }
}

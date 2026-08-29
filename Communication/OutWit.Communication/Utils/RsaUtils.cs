using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace OutWit.Communication.Utils
{
    /// <summary>
    /// RSA key (de)serialization for the encryption handshake. The JSON shape
    /// is the JWK-compatible form the 2.x converter produced ("mod", "exp",
    /// "d", "p", "q", "dp", "dq", "iq"), written and read by hand so the
    /// handshake carries no reflection-based serialization -- it must survive
    /// NativeAOT on the client and hostile input on the server.
    /// </summary>
    public static class RsaUtils
    {
        #region Serialization

        public static byte[] ToBytes(this RSAParameters rsaParams)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();

                WriteField(writer, "mod", rsaParams.Modulus);
                WriteField(writer, "exp", rsaParams.Exponent);
                WriteField(writer, "d", rsaParams.D);
                WriteField(writer, "p", rsaParams.P);
                WriteField(writer, "q", rsaParams.Q);
                WriteField(writer, "dp", rsaParams.DP);
                WriteField(writer, "dq", rsaParams.DQ);
                WriteField(writer, "iq", rsaParams.InverseQ);

                writer.WriteEndObject();
            }

            return stream.ToArray();
        }

        public static RSAParameters? ToRsaParameters(this byte[] data)
        {
            if (data.Length <= 1)
                return null;

            try
            {
                var result = new RSAParameters();
                var reader = new Utf8JsonReader(data);

                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    return null;

                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? name = reader.GetString();

                    if (!reader.Read())
                        return null;

                    byte[]? value = reader.TokenType == JsonTokenType.Null
                        ? null
                        : reader.GetBytesFromBase64();

                    // JWK aliases ("n", "e", "qi") accepted alongside the wire
                    // names for web-crypto producers.
                    switch (name)
                    {
                        case "mod": case "n": result.Modulus = value; break;
                        case "exp": case "e": result.Exponent = value; break;
                        case "d": result.D = value; break;
                        case "p": result.P = value; break;
                        case "q": result.Q = value; break;
                        case "dp": result.DP = value; break;
                        case "dq": result.DQ = value; break;
                        case "iq": case "qi": result.InverseQ = value; break;
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Encryption

        public static byte[] EncryptRsa(this byte[] me, RSAParameters? key)
        {
            if (key == null)
                return me;
            using var rsa = RSA.Create();

            rsa.ImportParameters(key.Value);
            return rsa.Encrypt(me, RSAEncryptionPadding.OaepSHA256);
        }

        public static byte[] DecryptRsa(this byte[] me, RSAParameters? key)
        {
            if(key == null)
                return me;

            using var rsa = RSA.Create();

            rsa.ImportParameters(key.Value);
            return rsa.Decrypt(me, RSAEncryptionPadding.OaepSHA256);
        }

        #endregion

        #region Tools

        private static void WriteField(Utf8JsonWriter writer, string name, byte[]? value)
        {
            if (value != null)
                writer.WriteBase64String(name, value);
        }

        #endregion
    }
}

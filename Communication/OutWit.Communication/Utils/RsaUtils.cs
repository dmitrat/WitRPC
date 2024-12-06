using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace OutWit.Communication.Utils
{
    public static class RsaUtils
    {
        public static byte[] ToBytes(this RSAParameters rsaParams)
        {
            string json = JsonConvert.SerializeObject(rsaParams);
            return Encoding.UTF8.GetBytes(json);
        }

        public static RSAParameters ToRsaParameters(this byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<RSAParameters>(json);
        }

        public static byte[] EncryptRsa(this byte[] me, RSAParameters key)
        {
            using var rsa = RSA.Create();

            rsa.ImportParameters(key);
            return rsa.Encrypt(me, RSAEncryptionPadding.OaepSHA256);
        }

        public static byte[] DecryptRsa(this byte[] me, RSAParameters key)
        {
            using var rsa = RSA.Create();

            rsa.ImportParameters(key);
            return rsa.Decrypt(me, RSAEncryptionPadding.OaepSHA256);
        }
    }
}

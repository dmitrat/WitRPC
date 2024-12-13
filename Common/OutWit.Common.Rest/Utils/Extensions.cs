using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OutWit.Common.Exceptions;
using OutWit.Common.Rest.Interfaces;

namespace OutWit.Common.Rest.Utils
{
    public static class Extensions
    {
        #region Constants

        private const char DOUBLE_QUOTES = '"';

        private const string JSON_MEDIA_TYPE = "application/json";

        #endregion

        #region Query

        public static string ToEnumString<T>(this T me)
            where T : Enum
        {
            return JsonConvert.SerializeObject(me, new StringEnumConverter()).Trim(DOUBLE_QUOTES);
        }

        public static string ToBoolString(this bool me)
        {
            return me.ToString(CultureInfo.InvariantCulture).ToLower();
        }

        public static string ToDateTimeString(this DateTime me, string format)
        {
            if (me.Kind == DateTimeKind.Unspecified)
                me = DateTime.SpecifyKind(me, DateTimeKind.Utc);

            return me.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string ToIntegerString(this long me)
        {
            return me.ToString("D", CultureInfo.InvariantCulture);
        }

        public static string ToDoubleString(this double me)
        {
            return me.ToString("F9", CultureInfo.InvariantCulture);
        }

        #endregion

        #region Serialization

        public static async Task<TValue> DeserializeAsync<TValue>(this HttpResponseMessage me)
            where TValue : class
        {
            await using var stream = await me.Content.ReadAsStreamAsync();

            using var reader = new JsonTextReader(new StreamReader(stream));

            me.EnsureSuccessStatusCode();

            return reader.Deserialize<TValue>();
        }

        private static TValue Deserialize<TValue>(this JsonTextReader me)
        {
            var serializer = new JsonSerializer { Culture = CultureInfo.InvariantCulture };

            var value = serializer.Deserialize<TValue>(me);

            if (value == null)
                throw new ExceptionOf<JsonSerializer>("Unable to deserialize JSON response message.");

            return value;
        }

        #endregion

        #region Messages

        public static HttpRequestMessage ToRequestMessage(this IRequestMessage me)
        {
            var message = new HttpRequestMessage
            {
                RequestUri = me.Build(),
                Content = me.BuildContent(),
                Method = me.Method
            };

            message.Headers.Authorization = me.BuildHeader();

            return message;
        }

        #endregion

        #region Content

        public static HttpContent? JsonContent(this object me)
        {
            try
            {
                return new StringContent(JsonConvert.SerializeObject(me, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }), Encoding.UTF8, JSON_MEDIA_TYPE);
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        #endregion

    }
}

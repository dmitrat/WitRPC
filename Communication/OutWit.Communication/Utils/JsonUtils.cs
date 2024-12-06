using System;
using System.Text;
using MessagePack.Formatters;
using Newtonsoft.Json;

namespace OutWit.Communication.Utils
{
    public static class JsonUtils
    {
        #region Json

        public static string ToJsonString<TObject>(this TObject me)
            where TObject : class
        {
            return JsonConvert.SerializeObject(me);
        }

        public static TObject? FromJsonString<TObject>(this string me)
            where TObject : class
        {
            return JsonConvert.DeserializeObject<TObject>(me);
        }

        public static object? FromJsonString(this string me, Type type)
        {
            return JsonConvert.DeserializeObject(me, type);
        }


        public static byte[] ToJsonBytes<TObject>(this TObject me)
            where TObject : class
        {
            return Encoding.UTF8.GetBytes(me.ToJsonString());
        }

        public static TObject? FromJsonBytes<TObject>(this byte[] me)
            where TObject : class
        {
            return JsonConvert.DeserializeObject<TObject>(Encoding.UTF8.GetString(me));
        }

        public static object? FromJsonBytes(this byte[] me, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(me), type);
        }

        public static TObject? JsonClone<TObject>(this TObject me)
            where TObject : class
        {
            var json = me.ToJsonString();
            if (string.IsNullOrEmpty(json))
                return null;

            return json.FromJsonString<TObject>();
        }

        #endregion
    }
}

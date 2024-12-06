using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OutWit.Communication.Converters
{
    public sealed class ValueConverterJson : ValueConverterBase
    {
        protected override bool TryUnpack(object origValue, Type destType, out object? destValue)
        {
            destValue = null;

            if (origValue is not JObject && origValue is not JArray)
                return false;
            
            try
            {
                destValue = ((JToken)origValue).ToObject(destType);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        protected override bool TryRestore(object origValue, Type destType, out object? destValue)
        {
            try
            {
                string json = JsonConvert.SerializeObject(origValue);
                destValue = JsonConvert.DeserializeObject(json, destType);
                return true;
            }
            catch
            {
                destValue = null;
                return false;
            }
        }
    }
}

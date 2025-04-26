using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace OutWit.Common.Json
{ 
    internal class JsonContextMerged : IJsonTypeInfoResolver
    {
        #region Fields

        private readonly List<JsonSerializerContext> m_contexts = new();

        private readonly DefaultJsonTypeInfoResolver m_fallback = new();

        #endregion

        #region Functions

        public void AddContext(JsonSerializerContext context)
        {
            m_contexts.Add(context);
        }

        #endregion

        #region IJsonTypeInfoResolver

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            foreach (var context in m_contexts.Cast<IJsonTypeInfoResolver>())
            {
                var typeInfo = context.GetTypeInfo(type, options);
                if (typeInfo is not null)
                    return typeInfo;
            }

            return m_fallback.GetTypeInfo(type, options);
        }

        #endregion
    }
}

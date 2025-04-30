using System;
using System.Text.Json.Serialization;

namespace OutWit.Common.Json
{
    public class JsonOptions
    {
        internal JsonOptions()
        {
            
        }
        
        public ICollection<JsonSerializerContext> Contexts { get; } = new List<JsonSerializerContext>();
    }
}

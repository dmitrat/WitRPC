
using System.Text.Json.Serialization;
using OutWit.Examples.Services.ClientBlazor.Converters;

namespace OutWit.Examples.Services.ClientBlazor.Encryption
{
    
    public class RSAParametersWeb
    {
        #region Properties

        [JsonPropertyName("n")]
        public string? mod { get; set; }


        [JsonPropertyName("e")]
        public string? exp { get; set; }


        [JsonPropertyName("d")]
        public string? d { get; set; }


        [JsonPropertyName("P")]
        public string? p { get; set; }


        [JsonPropertyName("q")]
        public string? q { get; set; }


        [JsonPropertyName("dp")]
        public string? dp { get; set; }


        [JsonPropertyName("dq")]
        public string? dq { get; set; }


        [JsonPropertyName("qi")]
        public string? iq { get; set; }

        #endregion
    }
}

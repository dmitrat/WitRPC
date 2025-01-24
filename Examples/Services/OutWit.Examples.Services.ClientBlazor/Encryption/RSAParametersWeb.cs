
using System.Text.Json.Serialization;
using OutWit.Examples.Services.ClientBlazor.Converters;

namespace OutWit.Examples.Services.ClientBlazor.Encryption
{
    
    public class RSAParametersWeb
    {
        #region Properties

        [JsonPropertyName("n")]
        public string? Modulus { get; set; }


        [JsonPropertyName("e")]
        public string? Exponent { get; set; }


        [JsonPropertyName("d")]
        public string? D { get; set; }


        [JsonPropertyName("P")]
        public string? P { get; set; }


        [JsonPropertyName("q")]
        public string? Q { get; set; }


        [JsonPropertyName("dp")]
        public string? DP { get; set; }


        [JsonPropertyName("dq")]
        public string? DQ { get; set; }


        [JsonPropertyName("qi")]
        public string? InverseQ { get; set; }

        #endregion
    }
}

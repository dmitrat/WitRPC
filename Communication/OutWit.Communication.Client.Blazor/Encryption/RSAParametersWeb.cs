using System.Text.Json.Serialization;

namespace OutWit.Communication.Client.Blazor.Encryption
{
    /// <summary>
    /// RSA key parameters for browser-to-server communication.
    /// JsonPropertyName attributes map JWK format names (n, e, d, etc.) for reading from browser.
    /// Property names match .NET RSAParameters for serialization to server.
    /// </summary>
    public sealed class RSAParametersWeb
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

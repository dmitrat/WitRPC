using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Provides access tokens for WitRPC channel authentication.
    /// Integrates with Blazor WASM authentication system.
    /// </summary>
    public sealed class ChannelTokenProvider : OutWit.Communication.Interfaces.IAccessTokenProvider
    {
        #region Constructors

        public ChannelTokenProvider(
            IAccessTokenProvider? tokenProvider, 
            ILogger<ChannelTokenProvider> logger)
        {
            TokenProvider = tokenProvider;
            Logger = logger;
        }

        #endregion

        #region IAccessTokenProvider

        public async Task<string> GetToken()
        {
            if (TokenProvider == null)
                return "";

            var result = await TokenProvider.RequestAccessToken(new AccessTokenRequestOptions());

            if (result.TryGetToken(out var token))
                return token.Value;
            
            Logger.LogError("Failed to get access token for WitRPC communication");

            return "";
        }

        #endregion

        #region Properties

        private IAccessTokenProvider? TokenProvider { get; }
        
        private ILogger<ChannelTokenProvider> Logger { get; }

        #endregion
    }
}

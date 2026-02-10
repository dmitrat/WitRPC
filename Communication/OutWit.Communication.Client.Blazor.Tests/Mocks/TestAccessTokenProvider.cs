using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace OutWit.Communication.Client.Blazor.Tests.Mocks
{
    internal sealed class TestAccessTokenProvider : IAccessTokenProvider
    {
        #region Fields

        private readonly string? m_token;

        #endregion

        #region Constructors

        public TestAccessTokenProvider(string? token = "test-access-token")
        {
            m_token = token;
        }

        #endregion

        #region IAccessTokenProvider

        public ValueTask<AccessTokenResult> RequestAccessToken()
        {
            return RequestAccessToken(new AccessTokenRequestOptions());
        }

        #pragma warning disable CS0618
        public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
        {
            if (m_token == null)
            {
                var result = new AccessTokenResult(
                    AccessTokenResultStatus.RequiresRedirect,
                    new AccessToken(),
                    "/login");

                return new ValueTask<AccessTokenResult>(result);
            }

            var token = new AccessToken { Value = m_token };
            var success = new AccessTokenResult(
                AccessTokenResultStatus.Success,
                token,
                null!);

            return new ValueTask<AccessTokenResult>(success);
        }
        #pragma warning restore CS0618

        #endregion
    }
}

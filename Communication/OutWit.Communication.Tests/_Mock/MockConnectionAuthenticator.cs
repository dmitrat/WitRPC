using System.Security.Claims;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Server.Authorization;

namespace OutWit.Communication.Tests.Mock
{
    /// <summary>
    /// A token validator that also names the principal: any token of the form
    /// <c>user:&lt;name&gt;</c> is valid and yields a principal with that name. What a
    /// JWT validator does in a real server, without the JWT.
    /// </summary>
    public sealed class MockConnectionAuthenticator : IAccessTokenValidator, IConnectionAuthenticator
    {
        #region Constants

        public const string PREFIX = "user:";

        #endregion

        #region IAccessTokenValidator

        public bool IsRequestTokenValid(string token)
        {
            return token.StartsWith(PREFIX, System.StringComparison.Ordinal);
        }

        public bool IsAuthorizationTokenValid(string token)
        {
            return IsRequestTokenValid(token);
        }

        #endregion

        #region IConnectionAuthenticator

        public bool TryAuthenticate(string token, out ClaimsPrincipal? principal)
        {
            AuthenticateCalls++;

            if (!IsAuthorizationTokenValid(token))
            {
                principal = null;
                return false;
            }

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, token.Substring(PREFIX.Length)) }, "mock");
            principal = new ClaimsPrincipal(identity);
            return true;
        }

        #endregion

        #region Properties

        public int AuthenticateCalls { get; private set; }

        #endregion
    }
}

using System.Security.Claims;

namespace OutWit.Communication.Server.Authorization
{
    /// <summary>
    /// An optional extension of <see cref="Interfaces.IAccessTokenValidator"/>: a validator that also
    /// implements this interface establishes a <see cref="ClaimsPrincipal"/> when a connection
    /// authorizes, and the server keeps it on the connection for
    /// <see cref="Connections.ConnectionContext.Principal"/>. A validator that does not implement it
    /// is used exactly as before, and the principal stays null.
    /// </summary>
    public interface IConnectionAuthenticator
    {
        /// <summary>
        /// Validates the authorization token of a connecting client and, when it is valid, returns the
        /// principal it identifies.
        /// </summary>
        /// <param name="token">The token from the authorization request.</param>
        /// <param name="principal">The principal when the token is valid; null otherwise, or when the
        /// validator cannot name one.</param>
        /// <returns>True when the token is valid and the connection may be authorized.</returns>
        bool TryAuthenticate(string token, out ClaimsPrincipal? principal);
    }
}

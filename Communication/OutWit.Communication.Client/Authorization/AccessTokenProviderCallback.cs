using System;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Authorization
{
    /// <summary>
    /// Access token provider that uses a callback function to get the token.
    /// Useful for scenarios where the token needs to be refreshed dynamically.
    /// </summary>
    public class AccessTokenProviderCallback : IAccessTokenProvider
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance with an async callback for token retrieval.
        /// </summary>
        /// <param name="getTokenAsync">Async function that returns the current valid token.</param>
        public AccessTokenProviderCallback(Func<Task<string>> getTokenAsync)
        {
            GetTokenAsync = getTokenAsync ?? throw new ArgumentNullException(nameof(getTokenAsync));
        }

        /// <summary>
        /// Creates a new instance with a sync callback for token retrieval.
        /// </summary>
        /// <param name="getToken">Function that returns the current valid token.</param>
        public AccessTokenProviderCallback(Func<string> getToken)
        {
            if (getToken == null)
                throw new ArgumentNullException(nameof(getToken));
                
            GetTokenAsync = () => Task.FromResult(getToken());
        }

        #endregion

        #region IAccessTokenProvider

        public Task<string> GetToken()
        {
            return GetTokenAsync();
        }

        #endregion

        #region Properties

        private Func<Task<string>> GetTokenAsync { get; }

        #endregion
    }
}

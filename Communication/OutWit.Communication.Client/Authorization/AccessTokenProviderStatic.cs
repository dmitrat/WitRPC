using System;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Authorization
{
    public class AccessTokenProviderStatic : IAccessTokenProvider
    {
        #region Constructors

        public AccessTokenProviderStatic(string accessToken)
        {
            AccessToken = accessToken;
        }

        #endregion

        #region IAccessTokenProvider

        public async Task<string> GetToken()
        {
            return await Task.FromResult(AccessToken);
        }

        #endregion

        #region Properties

        public string AccessToken { get; }

        #endregion
    }
}

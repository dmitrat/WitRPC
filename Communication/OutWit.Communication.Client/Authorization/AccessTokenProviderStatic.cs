using System;
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

        public string GetToken()
        {
            return AccessToken;
        }

        #endregion

        #region Properties

        public string AccessToken { get; }

        #endregion
    }
}

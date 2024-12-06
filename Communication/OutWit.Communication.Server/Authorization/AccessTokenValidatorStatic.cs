using System;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Authorization
{
    public class AccessTokenValidatorStatic : IAccessTokenValidator
    {
        #region Constructors

        public AccessTokenValidatorStatic(string accessToken)
        {
            AccessToken = accessToken;
        }

        #endregion

        #region IAccessTokenProvider

        public bool IsRequestTokenValid(string token)
        {
            return token == AccessToken;
        }

        public bool IsAuthorizationTokenValid(string token)
        {
            return token == AccessToken;
        }

        #endregion

        #region Properties

        public string AccessToken { get; }

        #endregion
    }
}

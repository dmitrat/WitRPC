using System;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Authorization
{
    public class AccessTokenValidatorPlain : IAccessTokenValidator
    {
        #region IAccessTokenProvider

        public bool IsRequestTokenValid(string token)
        {
            return token == "0";
        }

        public bool IsAuthorizationTokenValid(string token)
        {
            return token == "0";
        }

        #endregion
    }
}

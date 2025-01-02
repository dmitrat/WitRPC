using System;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.Authorization
{
    public class AccessTokenProviderPlain : IAccessTokenProvider
    {
        #region IAccessTokenProvider

        public string GetToken()
        {
            return "";
        }

        #endregion
    }
}

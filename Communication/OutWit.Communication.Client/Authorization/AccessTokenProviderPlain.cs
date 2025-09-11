using OutWit.Communication.Interfaces;
using System;
using System.Threading.Tasks;

namespace OutWit.Communication.Client.Authorization
{
    public class AccessTokenProviderPlain : IAccessTokenProvider
    {
        #region IAccessTokenProvider

        public async Task<string> GetToken()
        {
            return await Task.FromResult("");
        }

        #endregion
    }
}

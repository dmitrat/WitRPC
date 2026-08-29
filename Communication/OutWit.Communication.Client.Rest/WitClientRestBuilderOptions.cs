using System;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.Rest
{
    /// <summary>
    /// Everything a REST client is built from: where to call, how (POST only
    /// or GET where the mode allows), the per-call timeout, and the token
    /// provider for the <c>Authorization: Bearer</c> header.
    /// </summary>
    public class WitClientRestBuilderOptions
    {
        #region Constructors

        public WitClientRestBuilderOptions()
        {
            TokenProvider = new AccessTokenProviderPlain();
        }

        #endregion

        #region Properties

        public HostInfo? Host { get; set; }

        public RestClientRequestModes Mode { get; set; }

        public TimeSpan? Timeout { get; set; }

        public IAccessTokenProvider TokenProvider { get; set; }

        #endregion
    }
}

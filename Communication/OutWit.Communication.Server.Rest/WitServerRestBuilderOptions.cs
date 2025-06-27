using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Server.Authorization;

namespace OutWit.Communication.Server.Rest
{
    public class WitServerRestBuilderOptions
    {
        #region Constructors

        public WitServerRestBuilderOptions()
        {
            TokenValidator = new AccessTokenValidatorPlain();

            TransportOptions = null;
            Logger = null;
            Timeout = null;
        }

        #endregion

        #region Properties


        public RestServerTransportOptions? TransportOptions { get; set; }

        public IRequestProcessor? RequestProcessor { get; set; }

        public IAccessTokenValidator TokenValidator { get; set; }

        public ILogger? Logger { get; set; }

        public TimeSpan? Timeout { get; set; }

        #endregion
    }
}

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The service contract types the REST layer binds readable calls against
        /// (method names, parameter names and declared types). Filled by
        /// <c>WithService</c>; a custom processor must list its contracts.
        /// </summary>
        public List<Type> Contracts { get; } = new();

        public IAccessTokenValidator TokenValidator { get; set; }

        public ILogger? Logger { get; set; }

        public TimeSpan? Timeout { get; set; }

        /// <summary>The largest request body accepted, in bytes. A larger body is refused with 413.</summary>
        public long MaxBodyBytes { get; set; } = WitServerRest.DEFAULT_MAX_BODY_BYTES;

        /// <summary>The most requests processed at once. Unbounded by default.</summary>
        public int MaxConcurrentRequests { get; set; } = int.MaxValue;

        #endregion
    }
}

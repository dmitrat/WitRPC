using System;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Options for WitRPC server hosted service.
    /// </summary>
    public sealed class WitServerHostedServiceOptions
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the server configuration.
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to auto-start on startup.
        /// </summary>
        public bool AutoStart { get; set; } = true;

        #endregion
    }
}

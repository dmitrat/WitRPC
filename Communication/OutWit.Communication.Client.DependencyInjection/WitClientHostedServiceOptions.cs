using System;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Options for WitRPC client hosted service.
    /// </summary>
    public sealed class WitClientHostedServiceOptions
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the client to manage.
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to auto-connect on startup.
        /// </summary>
        public bool AutoConnect { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        #endregion
    }
}

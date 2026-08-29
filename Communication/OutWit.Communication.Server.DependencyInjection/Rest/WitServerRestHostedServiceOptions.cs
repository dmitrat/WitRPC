using System;

namespace OutWit.Communication.Server.DependencyInjection
{
    public sealed class WitServerRestHostedServiceOptions
    {
        #region Properties

        public string ServerName { get; set; } = string.Empty;

        public bool AutoStart { get; set; } = true;

        #endregion
    }
}

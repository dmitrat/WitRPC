using System;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;

namespace OutWit.Communication.Server.Rest
{
    public class RestServerTransportOptions : ModelBase, IServerOptions
    {
        #region Constants

        private const string TRANSPORT = "REST";

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Url: {Host?.BuildConnection(true)}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is RestServerTransportOptions options))
                return false;

            return Host.Check(options.Host);
        }

        public override RestServerTransportOptions Clone()
        {
            return new RestServerTransportOptions
            {
                Host = Host?.Clone()
            };
        }

        #endregion

        #region IServerOptions

        public string Transport { get; } = TRANSPORT;

        public Dictionary<string, string> Data =>
            Host == null 
                ? new Dictionary<string, string>()
                : new()
                {
                    { $"{nameof(HostInfo.Port)}", $"{Host.Port}" },
                    { $"{nameof(HostInfo.Path)}", $"{Host.Path}" },
                    { $"{nameof(HostInfo.UseSsl)}", $"{Host.UseSsl}" },
                    { $"{nameof(HostInfo.UseWebSocket)}", $"{Host.UseWebSocket}" },
                };

        #endregion

        #region Properties

        public HostInfo? Host { get; set; }

        #endregion
    }
}

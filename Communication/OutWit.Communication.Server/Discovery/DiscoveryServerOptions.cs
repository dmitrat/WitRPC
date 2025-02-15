using System;
using System.Net;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.Discovery
{
    public class DiscoveryServerOptions : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is DiscoveryServerOptions options))
                return false;

            return IpAddress?.GetAddressBytes().Is(options.IpAddress?.GetAddressBytes()) == true &&
                   Port.Is(options.Port) &&
                   Period.Is(options.Period) &&
                   Mode.Is(options.Mode);
        }

        public override DiscoveryServerOptions Clone()
        {
            return new DiscoveryServerOptions
            {
                IpAddress = IpAddress,
                Port = Port,
                Period = Period,
                Mode = Mode
            };
        }

        #endregion

        #region Properties

        public IPAddress? IpAddress { get; set; }

        public int Port { get; set; }

        public TimeSpan? Period { get; set; }

        public DiscoveryServerMode Mode { get; set; }

        #endregion
    }

    public enum DiscoveryServerMode
    {
        StartStop = 0,
        Continuous
    }
}

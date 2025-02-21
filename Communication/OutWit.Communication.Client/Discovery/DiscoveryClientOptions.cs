using System;
using System.Net;
using Microsoft.Extensions.Logging;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Client.Discovery
{
    public class DiscoveryClientOptions : ModelBase
    {
        #region Constants

        private const string DEFAULT_DISCOVERY_IP = "239.255.255.250";
        private const int DEFAULT_DISCOVERY_PORT = 3702;

        #endregion

        #region Constructors

        public DiscoveryClientOptions()
        {
            Serializer = new MessageSerializerJson();

            IpAddress = IPAddress.Parse(DEFAULT_DISCOVERY_IP);
            Port = DEFAULT_DISCOVERY_PORT;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is DiscoveryClientOptions options))
                return false;

            return IpAddress?.GetAddressBytes().Is(options.IpAddress?.GetAddressBytes()) == true &&
                   Port.Is(options.Port) &&

                   ((Logger == null && options.Logger == null) ||
                    ((Logger != null && options.Logger != null) &&
                     (Logger.GetType() == options.Logger.GetType()))) &&

                   ((Serializer == null && options.Serializer == null) ||
                    ((Serializer != null && options.Serializer != null) &&
                     (Serializer.GetType() == options.Serializer.GetType())));
        }

        public override DiscoveryClientOptions Clone()
        {
            return new DiscoveryClientOptions
            {
                IpAddress = IpAddress,
                Port = Port,
                Serializer = Serializer,
                Logger = Logger
            };
        }

        #endregion

        #region Properties

        public IPAddress? IpAddress { get; set; }

        public int? Port { get; set; }

        public ILogger? Logger { get; set; }

        public IMessageSerializer? Serializer { get; set; }

        #endregion
    }
}

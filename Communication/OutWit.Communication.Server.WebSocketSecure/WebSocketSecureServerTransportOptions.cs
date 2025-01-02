using System;
using System.Security.Cryptography.X509Certificates;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Model;

namespace OutWit.Communication.Server.WebSocketSecure
{
    public class WebSocketSecureServerTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Url: {HostInfo?.BuildConnection()}, MaxNumberOfClients: {MaxNumberOfClients}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WebSocketSecureServerTransportOptions options))
                return false;

            return HostInfo.Check(options.HostInfo) && 
                   MaxNumberOfClients.Is(options.MaxNumberOfClients);
        }

        public override WebSocketSecureServerTransportOptions Clone()
        {
            return new WebSocketSecureServerTransportOptions
            {
                HostInfo = HostInfo?.Clone(),
                MaxNumberOfClients = MaxNumberOfClients,
                Certificate = Certificate
            };
        }

        #endregion

        #region Properties

        public int MaxNumberOfClients { get; set; }
        
        public HostInfo? HostInfo { get; set; }

        public X509Certificate? Certificate { get; set; }

        #endregion
    }
}

using System;
using System.Security.Cryptography.X509Certificates;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.WebSocket
{
    public class WebSocketSecureServerTransportOptions : WebSocketServerTransportOptions
    {

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WebSocketSecureServerTransportOptions options))
                return false;

            return base.Is(options, tolerance);
        }

        public override WebSocketSecureServerTransportOptions Clone()
        {
            return new WebSocketSecureServerTransportOptions
            {
                Url = Url,
                MaxNumberOfClients = MaxNumberOfClients,
                Certificate = Certificate
            };
        }

        #endregion

        #region Properties

        public X509Certificate? Certificate { get; set; }

        #endregion
    }
}

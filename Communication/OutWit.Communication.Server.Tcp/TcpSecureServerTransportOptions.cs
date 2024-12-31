using System;
using OutWit.Common.Abstract;
using System.Security.Cryptography.X509Certificates;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpSecureServerTransportOptions : TcpServerTransportOptions
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is TcpSecureServerTransportOptions options))
                return false;

            return base.Is(options, tolerance);
        }

        public override TcpSecureServerTransportOptions Clone()
        {
            return new TcpSecureServerTransportOptions
            {
                Port = Port,
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

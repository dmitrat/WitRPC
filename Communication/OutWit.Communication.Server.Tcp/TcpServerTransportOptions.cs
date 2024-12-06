using OutWit.Common.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.Tcp
{
    public class TcpServerTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Pipe: {Port}, MaxNumberOfClients: {MaxNumberOfClients}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is TcpServerTransportOptions options))
                return false;

            return Port.Is(options.Port) && 
                   MaxNumberOfClients.Is(options.MaxNumberOfClients);
        }

        public override TcpServerTransportOptions Clone()
        {
            return new TcpServerTransportOptions
            {
                Port = Port
            };
        }

        #endregion

        #region Properties

        public int MaxNumberOfClients { get; set; }
        
        public int? Port { get; set; }

        #endregion
    }
}

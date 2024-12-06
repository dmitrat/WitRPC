using OutWit.Common.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Pipe: {PipeName}, MaxNumberOfClients: {MaxNumberOfClients}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is NamedPipeServerTransportOptions options))
                return false;

            return PipeName.Is(options.PipeName) && 
                   MaxNumberOfClients.Is(options.MaxNumberOfClients);
        }

        public override NamedPipeServerTransportOptions Clone()
        {
            return new NamedPipeServerTransportOptions
            {
                PipeName = PipeName
            };
        }

        #endregion

        #region Properties

        public int MaxNumberOfClients { get; set; }
        
        public string? PipeName { get; set; }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Interfaces;


namespace OutWit.Communication.Server.Pipes
{
    public class NamedPipeServerTransportOptions : ModelBase, IServerOptions
    {
        #region Constants

        private const string TRANSPORT = "NamedPipe";

        #endregion

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
                PipeName = PipeName,
                MaxNumberOfClients = MaxNumberOfClients,
            };
        }

        #endregion

        #region IServerOptions

        public string Transport { get; } = TRANSPORT;

        public Dictionary<string, string> Data =>
            new()
            {
                { $"{nameof(PipeName)}", $"{PipeName}" },
                { $"{nameof(MaxNumberOfClients)}", $"{MaxNumberOfClients}" },
            };

        #endregion

        #region Properties

        public int MaxNumberOfClients { get; set; }
        
        public string? PipeName { get; set; }

        #endregion
    }
}

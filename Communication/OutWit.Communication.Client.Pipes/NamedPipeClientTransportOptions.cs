using System;
using Microsoft.Extensions.Logging;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Client.Pipes
{
    public class NamedPipeClientTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Server: {ServerName}, Pipe: {PipeName}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is NamedPipeClientTransportOptions options))
                return false;

            return ServerName.Is(options.ServerName) &&
                   PipeName.Is(options.PipeName);
        }

        public override NamedPipeClientTransportOptions Clone()
        {
            return new NamedPipeClientTransportOptions
            {
                MaxMessageSize = MaxMessageSize,
                ServerName = ServerName,
                PipeName = PipeName
            };
        }

        #endregion

        #region Properties

        /// <summary>The largest frame accepted from the peer, in bytes. Larger frames close the connection instead of being allocated.</summary>
        public long MaxMessageSize { get; set; } = StreamFrameReader.DEFAULT_MAX_MESSAGE_SIZE;

        public string? ServerName { get; set; }

        public string? PipeName { get; set; }

        #endregion
    }
}

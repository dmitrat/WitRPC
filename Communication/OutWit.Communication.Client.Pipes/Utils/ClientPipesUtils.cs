using OutWit.Communication.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Client.Pipes.Utils
{
    public static class ClientPipesUtils
    {
        public static WitClientBuilderOptions WithNamedPipe(this WitClientBuilderOptions me, NamedPipeClientTransportOptions options)
        {
            me.Transport = new NamedPipeClientTransport(options);
            return me;
        }

        public static WitClientBuilderOptions WithNamedPipe(this WitClientBuilderOptions me, string server, string pipe)
        {
            return me.WithNamedPipe(new NamedPipeClientTransportOptions
            {
                PipeName = pipe,
                ServerName = server
            });
        }

        public static WitClientBuilderOptions WithNamedPipe(this WitClientBuilderOptions me, string pipe)
        {
            return me.WithNamedPipe(new NamedPipeClientTransportOptions
            {
                PipeName = pipe,
                ServerName = "."
            });
        }
    }
}

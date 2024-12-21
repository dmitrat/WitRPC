using OutWit.Communication.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Server.Pipes.Utils
{
    public static class ServerPipesUtils
    {
        public static WitComServerBuilderOptions WithNamedPipe(this WitComServerBuilderOptions me, NamedPipeServerTransportOptions options)
        {
            me.TransportFactory = new NamedPipeServerTransportFactory(options);
            return me;
        }

        public static WitComServerBuilderOptions WithNamedPipe(this WitComServerBuilderOptions me, string pipe, int maxNumberOfClients)
        {
            return me.WithNamedPipe(new NamedPipeServerTransportOptions
            {
                PipeName = pipe,
                MaxNumberOfClients = maxNumberOfClients
            });
        }

        public static WitComServerBuilderOptions WithNamedPipe(this WitComServerBuilderOptions me, string pipe)
        {
            return me.WithNamedPipe(new NamedPipeServerTransportOptions
            {
                PipeName = pipe,
                MaxNumberOfClients = 1
            });
        }
    }
}

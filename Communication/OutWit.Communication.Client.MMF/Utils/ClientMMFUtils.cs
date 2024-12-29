using OutWit.Communication.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Client.MMF.Utils
{
    public static class ClientMMFUtils
    {
        public static WitComClientBuilderOptions WithMemoryMappedFile(this WitComClientBuilderOptions me, MemoryMappedFileClientTransportOptions options)
        {
            me.Transport = new MemoryMappedFileClientTransport(options);
            return me;
        }

        public static WitComClientBuilderOptions WithMemoryMappedFile(this WitComClientBuilderOptions me, string name)
        {
            return me.WithMemoryMappedFile(new MemoryMappedFileClientTransportOptions
            {
                Name = name
            });
        }
    }
}

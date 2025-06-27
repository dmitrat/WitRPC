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
        public static WitClientBuilderOptions WithMemoryMappedFile(this WitClientBuilderOptions me, MemoryMappedFileClientTransportOptions options)
        {
            me.Transport = new MemoryMappedFileClientTransport(options);
            return me;
        }

        public static WitClientBuilderOptions WithMemoryMappedFile(this WitClientBuilderOptions me, string name)
        {
            return me.WithMemoryMappedFile(new MemoryMappedFileClientTransportOptions
            {
                Name = name
            });
        }
    }
}

using OutWit.Communication.Interfaces;
using System;

namespace OutWit.Communication.Server.MMF.Utils
{
    public static class ServerMMFUtils
    {
        public static WitComServerBuilderOptions WithMemoryMappedFile(this WitComServerBuilderOptions me, MemoryMappedFileServerTransportOptions options)
        {
            me.TransportFactory = new MemoryMappedFileServerTransportFactory(options);
            return me;
        }

        public static WitComServerBuilderOptions WithMemoryMappedFile(this WitComServerBuilderOptions me, string name, long size)
        {
            return me.WithMemoryMappedFile(new MemoryMappedFileServerTransportOptions
            {
                Name = name,
                Size = size
            });
        }

        public static WitComServerBuilderOptions WithMemoryMappedFile(this WitComServerBuilderOptions me, string name)
        {
            return me.WithMemoryMappedFile(new MemoryMappedFileServerTransportOptions
            {
                Name = name,
                Size = 1024 * 1024
            });
        }
    }
}

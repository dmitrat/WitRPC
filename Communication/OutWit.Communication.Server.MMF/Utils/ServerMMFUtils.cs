using OutWit.Communication.Interfaces;
using System;

namespace OutWit.Communication.Server.MMF.Utils
{
    public static class ServerMMFUtils
    {
        public static WitServerBuilderOptions WithMemoryMappedFile(this WitServerBuilderOptions me, MemoryMappedFileServerTransportOptions options)
        {
            me.TransportFactory = new MemoryMappedFileServerTransportFactory(options);
            return me;
        }

        public static WitServerBuilderOptions WithMemoryMappedFile(this WitServerBuilderOptions me, string name, long size)
        {
            return me.WithMemoryMappedFile(new MemoryMappedFileServerTransportOptions
            {
                Name = name,
                Size = size
            });
        }

        public static WitServerBuilderOptions WithMemoryMappedFile(this WitServerBuilderOptions me, string name)
        {
            return me.WithMemoryMappedFile(new MemoryMappedFileServerTransportOptions
            {
                Name = name,
                Size = 1024 * 1024
            });
        }
    }
}

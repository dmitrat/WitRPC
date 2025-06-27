using System;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Messages;

namespace OutWit.Communication.Client.MMF.Utils
{
    public static class DiscoveryUtils
    {
        private const string TRANSPORT = "MemoryMappedFile";

        public static bool IsMemoryMappedFile(this DiscoveryMessage me)
        {
            return me.Transport == TRANSPORT;
        }

        public static string MemoryMappedFileName(this DiscoveryMessage me)
        {
            if(!me.IsMemoryMappedFile())
                throw new WitException($"Wrong transport type: {me.Transport}");

            if(me.Data == null)
                throw new WitException($"Discovery data is empty", new ArgumentNullException(nameof(me.Data)));

            if (!me.Data.TryGetValue(nameof(MemoryMappedFileClientTransportOptions.Name), out var name))
                throw new WitException($"Cannot find parameter value for parameter: {nameof(MemoryMappedFileClientTransportOptions.Name)}");

            return name;
        }

        public static WitClientBuilderOptions WithMemoryMappedFile(this WitClientBuilderOptions me, DiscoveryMessage message)
        {
            return me.WithMemoryMappedFile(message.MemoryMappedFileName());
        }
    }
}

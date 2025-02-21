using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Discovery.Server.Model
{
    public enum TransportType
    {
        MemoryMappedFile,
        NamedPipe,
        TCP,
        WebSocket
    }
}

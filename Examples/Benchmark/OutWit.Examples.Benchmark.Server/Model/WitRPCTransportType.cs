﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Benchmark.Server.Model
{
    public enum WitRPCTransportType
    {
        MemoryMappedFile,
        NamedPipe,
        TCP,
        WebSocket
    }
}

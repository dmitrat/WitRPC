# OutWit.Communication.Server.MMF

## Overview
The `OutWit.Communication.Server.MMF` package provides server-side support for Memory-Mapped File (MMF) transport in WitRPC.

### Key Features
- Ideal for IPC on the same machine.
- Lightweight and efficient.

### Usage
```csharp
using OutWit.Communication.Server.MMF;

var server = WitServerBuilder.Build(options =>
{
    options.WithMemoryMappedFile("MySharedMemory", 1024 * 1024);
});
```

# OutWit.Communication.Client.MMF

## Overview
The `OutWit.Communication.Client.MMF` package provides client-side support for Memory-Mapped File (MMF) transport in WitRPC. MMF transport is ideal for high-performance inter-process communication (IPC) on the same machine.

### Key Features
- High-speed communication for processes on the same machine.
- Lightweight and efficient.

### Usage
```csharp
using OutWit.Communication.Client.MMF;

var client = WitClientBuilder.Build(options =>
{
    options.WithMemoryMappedFile("MySharedMemory");
});
```

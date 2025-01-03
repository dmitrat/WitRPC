# OutWit.Communication.Client.Pipes

## Overview
The `OutWit.Communication.Client.Pipes` package provides client-side support for Named Pipe transport in WitCom. Named Pipes are versatile for local and network-based IPC scenarios.

### Key Features
- Supports single or multiple clients.
- Works within local networks or on the same machine.

### Usage
```csharp
using OutWit.Communication.Client.Pipes;

var client = WitComClientBuilder.Build(options =>
{
    options.WithNamedPipe("MyPipe");
});
```

# OutWit.Communication.Server.Pipes

## Overview
The `OutWit.Communication.Server.Pipes` package provides server-side support for Named Pipe transport in WitRPC.

### Key Features
- Supports multiple clients.
- Works on local networks or the same machine.

### Usage
```csharp
using OutWit.Communication.Server.Pipes;

var server = WitServerBuilder.Build(options =>
{
    options.WithNamedPipe("MyPipe", 5);
});
```

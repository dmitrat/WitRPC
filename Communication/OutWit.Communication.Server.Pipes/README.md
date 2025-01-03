# OutWit.Communication.Server.Pipes

## Overview
The `OutWit.Communication.Server.Pipes` package provides server-side support for Named Pipe transport in WitCom.

### Key Features
- Supports multiple clients.
- Works on local networks or the same machine.

### Usage
```csharp
using OutWit.Communication.Server.Pipes;

var server = WitComServerBuilder.Build(options =>
{
    options.WithNamedPipe("MyPipe", 5);
});
```

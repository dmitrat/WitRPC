# OutWit.Communication.Server.Tcp

## Overview
The `OutWit.Communication.Server.Tcp` package provides server-side support for TCP transport in WitRPC.

### Key Features
- Reliable communication across networks.
- Supports secure connections with SSL/TLS.

### Usage
```csharp
using OutWit.Communication.Server.Tcp;

var server = WitServerBuilder.Build(options =>
{
    options.WithTcp(8080, 10);
});
```

For secure connections:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithTcpSecure(443, 10, myCertificate);
});
```

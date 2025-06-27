# OutWit.Communication.Client.Tcp

## Overview
The `OutWit.Communication.Client.Tcp` package provides client-side support for TCP transport in WitRPC. TCP is suitable for reliable communication across networks.

### Key Features
- Works over LAN, WAN, or the internet.
- Supports secure connections via SSL/TLS.

### Usage
```csharp
using OutWit.Communication.Client.Tcp;

var client = WitClientBuilder.Build(options =>
{
    options.WithTcp("127.0.0.1", 8080);
});
```

For secure connections:
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithTcpSecure("127.0.0.1", 443, "example.com", null);
});
```

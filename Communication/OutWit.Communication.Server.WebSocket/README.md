# OutWit.Communication.Server.WebSocket

## Overview
The `OutWit.Communication.Server.WebSocket` package provides server-side support for WebSocket transport in WitRPC.

### Key Features
- Real-time communication over the internet.
- Scalable and efficient for multiple clients.

### Usage
```csharp
using OutWit.Communication.Server.WebSocket;

var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://example.com/socket", 50);
});

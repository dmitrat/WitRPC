# OutWit.Communication.Client.WebSocket

## Overview
The `OutWit.Communication.Client.WebSocket` package provides client-side support for WebSocket transport in WitRPC. WebSocket is excellent for real-time communication over the web.

### Key Features
- Full-duplex communication.
- Works seamlessly over the internet.

### Usage
```csharp
using OutWit.Communication.Client.WebSocket;

var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://example.com/socket");
});
```

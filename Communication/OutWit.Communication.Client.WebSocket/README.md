

# OutWit.Communication.Client.WebSocket

WebSocket transport client for WitRPC, enabling real-time, full-duplex communication over WebSocket connections (great for internet or browser-based clients).

### Overview

**OutWit.Communication.Client.WebSocket** allows a WitRPC client to connect to a server using the **WebSocket** protocol. WebSockets provide persistent, bi-directional communication over HTTP infrastructure, which is great for real-time updates and working through web proxies or firewalls. This transport is useful if you want a long-lived connection on standard web ports (like 80 or 443) or plan to interact with a server from web-based clients.

Like TCP, WebSocket supports full duplex communication, meaning the server can push events to the client at any time and the client can send requests anytime, all over a single persistent connection. WebSocket is essentially TCP tunneled through an HTTP handshake, making it friendly to web environments.

**Use cases:**

-   Communicating with a WitRPC server from a **browser** (with an appropriate JavaScript client, since browsers can do WebSocket). This can enable interactive web dashboards or controls that talk to a .NET service.
    
-   Situations where **network infrastructure** might block custom TCP ports but allows WebSocket (which often uses port 80 or 443 and can pass through proxies as it's seen as Web traffic).
    
-   Any scenario requiring **real-time** bidirectional messaging in a standardized way.
    

**Note:** Use **OutWit.Communication.Server.WebSocket** on the server side to accept WebSocket clients. Typically, the server is configured with an `http://` (or `https://`) URL to listen on, and clients use the corresponding `ws://` (or `wss://`) URL to connect.

### Installation

```shell
Install-Package OutWit.Communication.Client.WebSocket
```

### Usage

To use the WebSocket transport, configure the client with the WebSocket URI of the server:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket;
using OutWit.Communication.Serializers;
using OutWit.Communication.Client.Encryption;

var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000/service"); // WebSocket endpoint URI
    options.WithJson();
    options.WithEncryption();   // optional: enable message encryption on top of WebSocket
    // options.WithAccessToken("SecureToken"); // if server requires a token
});
await client.ConnectAsync(TimeSpan.FromSeconds(5));

IMyService service = client.GetService<IMyService>();
```

In this example, the client will perform a WebSocket handshake to `ws://localhost:5000/service`. On the server side, you should have something like:

```csharp
// Server-side (OutWit.Communication.Server.WebSocket):
options.WithWebSocket("http://localhost:5000/service", maxNumberOfClients: 10);
```

Notice that the server uses an `http://` URL and the client uses `ws://`. The server's HTTP listener will upgrade incoming connections to WebSockets. If you want to secure the WebSocket with TLS, use an `https://` URL on the server (with a valid certificate set up) and `wss://` on the client. For example:

```csharp
options.WithWebSocket("https://myserver.com/service", maxNumberOfClients: 10);
```

and on the client:

```csharp
options.WithWebSocket("wss://myserver.com/service");
```

This will encrypt the WebSocket traffic. (In this case, you might not need WitRPC's own `.WithEncryption()`, since TLS already provides encryption.)

After connecting, usage of `service` is the same as always. You can call methods and receive events. The WebSocket stays open, allowing the server to send notifications spontaneously. The client will remain connected until you explicitly disconnect or dispose it, or a network issue occurs.

**Token Authentication:** If you set an access token on the server, the WebSocket client will include it when connecting (most likely as an HTTP header during the handshake). Be sure to call `.WithAccessToken` on the client with the correct token.

**Performance:** WebSockets have a bit more overhead at the start (the HTTP handshake), but after that, performance is comparable to raw TCP for most purposes. They are very suitable for applications with frequent message exchanges or needing low latency updates.

### Further Documentation

See the official [WitRPC documentation](https://witrpc.io/) for additional examples and information on the WebSocket transport, including troubleshooting tips for WebSocket connections.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Client.WebSocket in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
# OutWit.Communication.Server.WebSocket

**NuGet Description:** WebSocket transport server for WitRPC, enabling a server to accept clients over WebSocket (great for real-time, browser-friendly communication).

### Overview

**OutWit.Communication.Server.WebSocket** allows a WitRPC server to accept client connections using the **WebSocket** protocol. Under the hood, this means the server starts an HTTP listener on a specified URL and upgrades incoming requests to WebSockets. WebSockets are ideal for scenarios that require real-time, full-duplex communication with clients, especially when those clients might be web browsers or need to use standard web ports. This transport combines the performance of a persistent socket with the accessibility of HTTP.

Use cases include:

-   **Web Dashboard or UI**: If you have a web-based frontend that should receive updates from the server or call server methods, WebSocket is the transport to use. Browsers can maintain WebSocket connections easily.
    
-   **Cross-Network Communication**: WebSockets often work even in restrictive network environments (they use HTTP ports and can traverse proxies), making them a good choice for internet-facing services. For example, if clients are connecting over the internet or through corporate networks, using WebSocket (especially secure WebSocket, wss://) is usually easier than opening custom TCP ports.
    
-   **General Full-Duplex Needs**: Any time you want the server to be able to send data to the client spontaneously (event notifications) and keep latency low, a WebSocket is a good fit.
    

### Installation

```shell
Install-Package OutWit.Communication.Server.WebSocket
```

### Usage

To host a WitRPC service over WebSocket, configure an HTTP URL and a client limit:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.WebSocket;

var server = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
    // Listen via WebSocket on port 5000, path "/ws":
    options.WithWebSocket("http://localhost:5000/ws", maxNumberOfClients: 100);
    options.WithJson();
    options.WithAccessToken("AuthToken123"); // optional token-based auth
    options.WithEncryption();                // optional WitRPC message encryption
});
server.StartWaitingForConnection();
Console.WriteLine("WebSocket server listening on ws://localhost:5000/ws");
```

**Important URL note:** On the server, use an `http://` (or `https://`) URL in `WithWebSocket`. On the client side, the equivalent would be `ws://` (or `wss://`). In the example above, the server is listening on `http://localhost:5000/ws`, so clients should connect to `ws://localhost:5000/ws`. The server's HttpListener will handle the WebSocket upgrade at that path.

In the configuration:

-   `maxNumberOfClients: 100` allows up to 100 concurrent WebSocket client connections.
    
-   We used JSON for serialization. (Typically, WebSocket transport can also work with MessagePack or others, but JSON is common for web compatibility.)
    
-   We set an `AuthToken123` as an access token. The WitRPC WebSocket handshake will expect clients to provide this token (for example, via an `Authorization` header or a query parameter during the upgrade request, depending on implementation). Clients should also use `.WithAccessToken("AuthToken123")` to be accepted.
    
-   We enabled `.WithEncryption()`. If the connection is `ws://` (not secure WebSocket), WitRPC's encryption will ensure the messages are encrypted. If we were using `wss://` (secure WebSocket over TLS), we might skip the extra encryption, as TLS already secures the channel.
    

**Using Secure WebSockets (wss):** To enable `wss://`, the server must listen on `https://`. For example:

```csharp
options.WithWebSocket("https://mydomain.com:5001/ws", maxNumberOfClients: 50);
```

This requires a valid TLS certificate for `mydomain.com` bound to port 5001. Once set up, clients would connect with `wss://mydomain.com:5001/ws`. All data would be encrypted at the transport layer. Running WebSocket over TLS (wss) is the recommended approach for production if clients are connecting over untrusted networks, as it provides standard security.

Once the server is running, it will accept WebSocket handshake requests at the specified URL. Each connected client is managed independently. If your service sends out events, the server will push those over each client's WebSocket as needed.

**Resource Management:** A WebSocket server keeps connections open, which consume resources (threads or async I/O handles, memory, etc.). The `maxNumberOfClients` should be set according to what your server hardware and application can handle. WitRPC will not accept new connections beyond this number.

### Further Documentation

For more details about using WebSockets with WitRPC (and how to configure things like custom headers or handle large message sizes), please refer to the official documentation on [witrpc.io](https://witrpc.io/). It also provides troubleshooting tips for WebSocket connection issues and examples of integrating with web clients.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server.WebSocket in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
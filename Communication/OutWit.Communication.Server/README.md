# OutWit.Communication.Server

Base server library for the WitRPC framework, providing core functionality to host services and handle incoming RPC connections over various transports.

### Overview

**OutWit.Communication.Server** is the foundation for building WitRPC servers. It provides the core server-side runtime that listens for client connections, authenticates/authorizes clients, and dispatches incoming RPC calls to your service implementations. With this library, you can host one or more service objects and allow remote clients to invoke their methods or subscribe to their events in real time.

Key capabilities include:

-   **Service Hosting:** You register your service instances (objects implementing your service interfaces) with the server using `options.WithService(...)`. The server will expose those services to clients, ensuring that method calls from clients are routed to the correct service object and results (or exceptions) are sent back.
    
-   **Composite Services:** You can register **multiple service interfaces** using `WithServices()` builder. This allows clients to request proxies for different interfaces from a single server connection, enabling modular service design without creating a "super-interface".
    
-   **Multi-Client Management:** The server can handle multiple concurrent client connections (depending on the transport and configuration). You can control the maximum number of clients for certain transports (e.g. set a client limit for TCP or WebSocket listeners). The server manages each connected client's requests and keeps track of subscriptions to events, allowing the server to push events to all subscribed clients.
    
-   **Security and Authorization:** The server can easily enforce security policies. By using `options.WithEncryption()` on the server (and the client doing the same), all communication will be encrypted end-to-end using AES for payloads and RSA for key exchange. You can also require clients to present an access token or API key: for example, call `options.WithAccessToken("YourSecretToken")` to set a required token (the server will then automatically reject clients that don't provide a matching token). This token-based auth ensures only authorized clients can connect and invoke your services.
    
-   **Serialization Flexibility:** The server decides how to serialize data sent to/from clients. JSON is the default (human-readable and good for interoperability), but you can switch to MessagePack or others by calling `options.WithMessagePack()`, `options.WithMemoryPack()`, etc. The chosen serializer must correspond to the client's choice.
    
-   **Logging and Diagnostics:** The core server has hooks for logging (you can pass in an `ILogger` via `options.WithLogger(...)` if you use Microsoft.Extensions.Logging). It can log key events like connections, disconnections, and errors, which is useful for monitoring. You can also set timeouts (`options.WithTimeout(TimeSpan)`) to guard against hanging calls or inactive clients.
    

This base server package is transport-agnostic. In practice, you will combine OutWit.Communication.Server with one of the specific server transport packages (e.g. Server.Tcp, Server.Pipes, Server.WebSocket, etc.) to actually accept connections. Those transport packages plug into this server and handle the low-level listening and I/O.

### Installation

```shell
Install-Package OutWit.Communication.Server
```

> **Note:** Usually you will add a specific server transport package to your project, which will include this base library. For example, if you want to host over TCP, add **OutWit.Communication.Server.Tcp** (which brings in OutWit.Communication.Server automatically).

### Basic Usage

Using the server involves providing your service implementation and choosing a transport to listen on. For example, to host a simple service over TCP:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.Tcp;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Server.Authorization;

var server = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());               // Register the service instance to host
    options.WithTcp(port: 5000, maxNumberOfClients: 100); // Listen on TCP port 5000, up to 100 clients
    options.WithJson();                                 // Use JSON serialization for data
    options.WithEncryption();                           // Enable encryption (AES/RSA)
    options.WithAccessToken("Secr3tToken");             // Require clients to provide this token
});
server.StartWaitingForConnection();
Console.WriteLine("Server is now listening for clients on TCP port 5000...");
```

### Composite Services (Multiple Interfaces)

You can register **multiple service interfaces** on a single server, allowing clients to request proxies for different interfaces:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.Tcp;

var server = WitServerBuilder.Build(options =>
{
    options.WithServices()                              // Start composite service registration
        .AddService<IUserService>(new UserService())
        .AddService<IOrderService>(new OrderService())
        .AddService<INotificationService>(new NotificationService())
        .Build();                                       // Complete registration
    
    options.WithTcp(port: 5000, maxNumberOfClients: 100);
    options.WithJson();
    options.WithEncryption();
});
server.StartWaitingForConnection();
```

Clients can then request proxies for any registered interface:

```csharp
var userService = client.GetService<IUserService>();
var orderService = client.GetService<IOrderService>();
var notificationService = client.GetService<INotificationService>();
```

This approach is cleaner than creating a single "super-interface" that inherits from all service interfaces.

### Server Lifecycle

When clients connect and invoke methods:

-   The WitRPC server will accept the connection and handle the handshake (including token verification and encryption setup if those are enabled).
    
-   When a client calls a service method, the server receives the request, deserializes it to a `WitRequest` object, and invokes the corresponding method on your service object. The return value (or any exception) is captured and sent back as a response.
    
-   If your service raises an event (e.g., calls an event delegate to notify of some change), the server framework will automatically forward that event to all connected clients that have subscribed to it. This allows for real-time push notifications from server to clients.

To stop the server when your application is shutting down:

```csharp
server.StopWaitingForConnection();
server.Dispose();
```

This will stop listening for new connections, close all existing client connections, and release resources like ports or pipe handles.

### Further Documentation

Refer to the [WitRPC documentation](https://witrpc.io/) for more on server configuration, advanced options (like custom authentication via `WithAccessTokenValidator` or service discovery), and best practices for hosting WitRPC services.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
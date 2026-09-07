# OutWit.Communication.Server

Base server library for the WitRPC framework, providing core functionality to host services and handle incoming RPC connections over various transports.

### Overview

**OutWit.Communication.Server** is the foundation for building WitRPC servers. It provides the core server-side runtime that listens for client connections, authenticates/authorizes clients, and dispatches incoming RPC calls to your service implementations. With this library, you can host one or more service objects and allow remote clients to invoke their methods or subscribe to their events in real time.

Key capabilities include:

-   **Service Hosting:** You register your service instances (objects implementing your service interfaces) with the server using `options.WithService(...)`. The server will expose those services to clients, ensuring that method calls from clients are routed to the correct service object and results (or exceptions) are sent back.
    
-   **Composite Services:** You can register **multiple service interfaces** using `WithServices()` builder. This allows clients to request proxies for different interfaces from a single server connection, enabling modular service design without creating a "super-interface".
    
-   **Multi-Client Management:** The server can handle multiple concurrent client connections (depending on the transport and configuration). You can control the maximum number of clients for certain transports (e.g. set a client limit for TCP or WebSocket listeners). The server manages each connected client's requests and keeps track of subscriptions to events, allowing the server to push events to all subscribed clients.
    
-   **Security and Authorization:** The server can easily enforce security policies. By using `options.WithEncryption()` on the server (and the client doing the same), messages are encrypted with authenticated **AES-256-GCM** (separate keys per direction, RSA key exchange); tampered, replayed or reordered frames are rejected. Treat message-layer encryption as **defence in depth**: on network transports run TLS as the primary protection, on local transports (MMF, pipes) it is the natural channel protection. You can also require clients to present an access token or API key: for example, call `options.WithAccessToken("YourSecretToken")` to set a required token (the server will then automatically reject clients that don't provide a matching token). This token-based auth ensures only authorized clients can connect and invoke your services.
    
-   **Serialization Flexibility:** The server decides how to serialize data sent to/from clients. JSON is the default (human-readable and good for interoperability); `options.WithMemoryPack()` is in the core, and `options.WithMessagePack()`, `options.WithProtoBuf()` and `options.WithGoogleProtobuf()` come from the opt-in `OutWit.Communication.Serializers.*` packages (since 3.1.0). The chosen serializer must correspond to the client's choice.
    
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

> The parameterless `GetService<T>()` requires the client to reference the opt-in [OutWit.Communication.Client.DynamicProxy](https://www.nuget.org/packages/OutWit.Communication.Client.DynamicProxy/) package (since 2.4.0); source-generated proxies (`[ProxyTarget]` + `OutWit.Common.Proxy.Generator`) need no extra package.

This approach is cleaner than creating a single "super-interface" that inherits from all service interfaces.

### Server Lifecycle

When clients connect and invoke methods:

-   The WitRPC server will accept the connection and handle the handshake (including token verification and encryption setup if those are enabled).
    
-   When a client calls a service method, the server receives the request, deserializes it to a `WitRequest` object, and invokes the corresponding method on your service object. The return value (or any exception) is captured and sent back as a response.
    
-   If your service raises an event (e.g., calls an event delegate to notify of some change), the server framework will automatically forward that event to all connected clients that have subscribed to it. This allows for real-time push notifications from server to clients. Since 3.2 a service can also address an event to one client or a set of clients (see below).

To stop the server when your application is shutting down:

```csharp
server.StopWaitingForConnection();
server.Dispose();
```

This will stop listening for new connections, close all existing client connections, and release resources like ports or pipe handles.

### Connection context and targeted events (3.2)

A service method can find out which connection it is serving, and an event can be sent to one connection instead of everyone. Nothing changes on the wire and nothing changes for a service that does not use it: an event raised the ordinary way still reaches every connected client.

**Who is calling.** `ConnectionContext.Current` is set for the duration of every request (an `AsyncLocal`, so it follows the method through its `await`s and into tasks it starts) and is `null` outside one:

```csharp
public Guid WhoAmI() => ConnectionContext.Current!.ConnectionId;
```

It carries the connection id, the server (`ServerId`, `ServerName`, `Transport`) and, when your token validator also implements `IConnectionAuthenticator`, the `ClaimsPrincipal` established when the connection authorized. Prefer `IConnectionContextAccessor` (register `ConnectionContextAccessor` as a singleton) when the service takes it by injection, and `ConnectionContext.BeginScope(...)` to simulate a connection in a unit test.

**Events for one client.** Open a `CallbackScope` around the raise; the server that hosts the service reads the target when the callback reaches it:

```csharp
public void Notify(Guid connectionId, string message)
{
    using var scope = CallbackScope.Target(connectionId);   // or Target(ids), or TargetCaller()
    Notified(message);                                      // the contract's ordinary event
    // scope.Report says what happened now; await scope.Completion for the final word:
    // Sent, UnknownConnection, NotAuthorized, QueueFull, SendFailed, SendTimedOut
}
```

`TargetCaller()` is "reply to whoever is calling" without knowing the id. A service registered in several servers is raised in each; the report lists the outcome per server, and the one that owns the connection delivers.

**Delivery options.** Every connection has one ordered outbound queue: responses, handshake replies and events leave in the order they were queued, and an event raised inside a method before it returns is written before that method's response. The queue can be bounded per connection:

```csharp
options.WithCallbackDelivery(delivery =>
{
    delivery.MaxPendingCallbacks = 256;                         // 0 = unbounded (default)
    delivery.MaxPendingCallbackBytes = 64 * 1024 * 1024;        // 0 = unbounded (default)
    delivery.OverflowPolicy = CallbackOverflowPolicy.CloseConnection;   // Log (default) | CloseConnection | DropNewest
});
options.WithTargetedCallbacksOnly();   // refuse an event raised outside a CallbackScope
```

Responses are never counted against the bound and never dropped. With the default `Log` policy an overflow and a send timeout are logged and the connection stays open, exactly as before 3.2; `CloseConnection` closes a client that cannot keep up (and one whose send times out), which is what keeps every other connection of the server unaffected; `DropNewest` is for events the service declares lossy. `WithTargetedCallbacksOnly()` is for a server whose clients must never see each other's events: a raise without a target is refused and logged.

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
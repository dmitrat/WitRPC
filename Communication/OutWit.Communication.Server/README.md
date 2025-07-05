
# OutWit.Communication.Server

Base server library for the WitRPC framework, providing core functionality to host services and handle incoming RPC connections over various transports.

### Overview

**OutWit.Communication.Server** is the foundation for building WitRPC servers. It provides the core server-side runtime that listens for client connections, authenticates/authorizes clients, and dispatches incoming RPC calls to your service implementations. With this library, you can host one or more service objects and allow remote clients to invoke their methods or subscribe to their events in real time.

Key capabilities include:

-   **Service Hosting:** You register your service instances (objects implementing your service interfaces) with the server using `options.WithService(...)`. The server will expose those services to clients, ensuring that method calls from clients are routed to the correct service object and results (or exceptions) are sent back. You can register multiple services if your application has several interfaces to expose.
    
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

In this example, we create a server that will listen on TCP port 5000 and handle up to 100 simultaneous clients. We register an instance of `MyService` (which implements some service interface `IMyService`) so that clients can call its methods. We also enabled JSON serialization, turned on encryption, and set an access token for security. Once `StartWaitingForConnection()` is called, the server begins listening asynchronously for incoming client connections. (This call is non-blocking; the server runs in the background, so your program can continue executing other tasks or wait for a shutdown signal.)

When clients connect and invoke methods:

-   The WitRPC server will accept the connection and handle the handshake (including token verification and encryption setup if those are enabled).
    
-   When a client calls a service method, the server receives the request, deserializes it to a `WitRequest` object, and invokes the corresponding method on your `MyService` object. The return value (or any exception) is captured and sent back as a response.
    
-   If your service raises an event (e.g., calls an event delegate to notify of some change), the server framework will automatically forward that event to all connected clients that have subscribed to it. This allows for real-time push notifications from server to clients.
    
You can host multiple services by calling `options.WithService(...)` multiple times (with different service instances implementing different interfaces). Clients will specify which interface they want to use when calling `GetService<T>()`, and WitRPC will route calls appropriately.

To stop the server when your application is shutting down:

```csharp
server.StopWaitingForConnection();
server.Dispose();
```

This will stop listening for new connections, close all existing client connections, and release resources like ports or pipe handles.

### Further Documentation

Refer to the [WitRPC documentation](https://witrpc.io/) for more on server configuration, advanced options (like custom authentication via `WithAccessTokenValidator` or service discovery), and best practices for hosting WitRPC services.
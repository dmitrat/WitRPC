
# OutWit.Communication.Client

Base client library for the WitRPC framework, providing functionality to connect to WitRPC servers and create dynamic proxies for calling remote services over various transports.

### Overview

**OutWit.Communication.Client** is the core client-side library in the WitRPC ecosystem. It enables your application to act as a **WitRPC client** that can connect to remote services hosted by a WitRPC server. This package provides the fundamental client logic, including managing the connection, sending requests, receiving responses, and generating proxy objects that implement your service interfaces on the client side. Using these proxies, you can call remote methods and handle server events as if they were happening locally.

Key capabilities of this client library include:

-   **Dynamic Service Proxies:** The client produces a proxy for your service interface (e.g. `IMyService`) via `client.GetService<IMyService>()`. This proxy forwards your method calls to the server and raises server-sent events on the client, supporting a natural, object-oriented usage of remote services (no need to manually serialize calls).
    
-   **Transport Agnostic:** The client library works with any supported transport (Named Pipes, TCP, WebSocket, Memory-Mapped Files, REST, etc.). The actual transport is provided by a transport-specific package, but the client API remains the same. You simply configure the desired transport during setup (for example, using `.WithTcp(...)`, `.WithWebSocket(...)`, etc.) and the library handles the details.
    
-   **Serialization & Encryption:** The client supports all WitRPC serialization options (JSON by default, or MessagePack/others) and can enable encryption and authentication to match the server's configuration. These are toggled via builder options like `.WithJson()`, `.WithMessagePack()`, `.WithEncryption()`, and `.WithAccessToken("token")` for providing credentials.
    
-   **Error Handling & Timeouts:** You can specify timeouts for connection and calls. The framework will return error statuses or throw exceptions if calls fail or time out, making it easier to handle network issues. The `ConnectAsync` method allows you to gracefully attempt connections within a specified timeframe.
    
Under the hood, OutWit.Communication.Client works with the core OutWit.Communication components to format requests and decode responses. In most cases, you will use this package by including a transport-specific client package which depends on it, rather than using OutWit.Communication.Client alone.

### Installation

```shell
Install-Package OutWit.Communication.Client
```

> **Note:** In practice, you will typically install a specific client transport package (such as **OutWit.Communication.Client.Tcp**, **OutWit.Communication.Client.WebSocket**, etc.) which will automatically include this base library. Choose a transport package that matches the server's transport. For example, if the server uses TCP, install *OutWit.Communication.Client.Tcp* in the client application.

### Basic Usage

To use the WitRPC client, first ensure you have a matching server running (using the corresponding OutWit.Communication.Server.\* package). In your client code, you build and connect a client as follows:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.Pipes;   // example transport: Named Pipes
using OutWit.Communication.Serializers;
using OutWit.Communication.Client.Encryption;

var client = WitClientBuilder.Build(options =>
{
    // Configure the transport (Named Pipe in this example):
    options.WithNamedPipe("MyAppPipe");
    // Use JSON serialization and enable encryption to match the server:
    options.WithJson();
    options.WithEncryption();
    // If the server requires an access token for authorization:
    // options.WithAccessToken("SecureTokenValue");
});

bool connected = await client.ConnectAsync(TimeSpan.FromSeconds(5));
if (!connected)
{
    Console.Error.WriteLine("Unable to connect to the RPC server.");
    return;
}

// Obtain a proxy to the remote service interface
IMyService service = client.GetService<IMyService>();

// Now you can call methods on the service and subscribe to events as if it were local:
service.SomeMethod("Hello");              
service.SomeEvent += data => Console.WriteLine($"Event from server: {data}");
```

In this example, we built a client to connect via a named pipe called `"MyAppPipe"`. The server must also be configured to listen on `"MyAppPipe"` (using OutWit.Communication.Server.Pipes) for the connection to succeed. We enabled JSON serialization and encryption in the client to align with the server's settings. We also demonstrate calling a method `SomeMethod` on the service and subscribing to an event `SomeEvent`. The call to `SomeMethod` will be sent to the server's implementation, and any `SomeEvent` raised on the server will invoke our console `WriteLine` on the client side.

**Transport Selection:** The above used Named Pipes for illustration, but you would use the transport relevant to your scenario. For example, to connect via TCP: use `options.WithTcp("server-address", port)`, or for WebSocket: `options.WithWebSocket("ws://server:port/path")`. Just ensure the server is using the corresponding server transport with matching parameters (pipe name, port number, URL path, etc.).

**Dynamic Proxies:** After connecting, calling `client.GetService<T>()` returns an object of interface `T` that acts as a live proxy to the server. You do not need to write any networking code: simply call methods on this proxy as you would on a local object. The WitRPC client takes care of packaging the call, sending it to the server, and unwrapping the response. If the server triggers an event, the proxy raises that event on the client side.

**Tip:** This base client library works behind the scenes. In a typical application, you will primarily interact with the builder (to configure transports/security) and the proxy object. The heavy lifting of managing sockets, threads, or pipe streams is handled internally by this library and the transport plugins.

### Further Documentation

See the [WitRPC documentation](https://witrpc.io/) for advanced client usage, including using the source generator (OutWit.Common.Proxy.Generator) for compile-time proxy generation and debugging tips.
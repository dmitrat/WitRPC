# WitRPC

[![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.svg)](https://www.nuget.org/packages/OutWit.Communication/)
[![License](https://img.shields.io/github/license/dmitrat/WitRPC)](LICENSE)

WitRPC is a modern API for client-server communication designed to simplify development and provide a robust, extensible framework. It offers a seamless way to handle real-time and event-driven interactions with minimal setup, acting as a powerful alternative to traditional frameworks like WCF and SignalR.

> Current versions (2026-08-29): core and transports **3.1.x** (`OutWit.Communication`, `Server`, `Client`, `Client.DynamicProxy` and the `Tcp` pair at 3.1.1), REST **3.2.3**, DependencyInjection **3.2.1**. Protocol 3 is a coordinated major: a 3.0 client needs a 3.0 server (the server refuses older clients with a readable version message). See [CHANGELOG.md](CHANGELOG.md) for what changed and the migration notes.

## Features

- **Proxy Mechanism**: Client-side proxies mirror server interfaces, enabling natural interaction with server-side objects via method calls, property access, and event subscriptions. Runtime proxy generation (Castle.Core) ships as the opt-in `OutWit.Communication.Client.DynamicProxy` package; the core client stays free of Castle.Core, supports source-generated proxies (`OutWit.Common.Proxy.Generator`), and publishes cleanly under NativeAOT.
- **Multiple Transport Options**:
  - Memory-Mapped Files
  - Named Pipes
  - TCP (with TLS support)
  - WebSocket
  - REST — not a transport of the WitRPC protocol but a stateless HTTP + JSON **compatibility layer** with its own host (`WitServerRest`), for callers that are not WitRPC at all
- **Composite Services**: Host multiple service interfaces on a single server, clients can request proxies for any registered interface.
- **Security**:
  - Authenticated message-layer encryption — AES-256-GCM per direction with an RSA handshake; tampered, replayed or reordered frames are rejected. Defence in depth: run TLS on network transports, use it as the channel protection on local ones (MMF, pipes)
  - Cross-platform BouncyCastle encryption (works in Blazor WebAssembly)
  - Token-based authorization
  - Protocol version handshake — a mismatched client is refused with a readable reason
- **Serialization** (for your payloads; the envelope is always MemoryPack):
  - JSON (default) and MemoryPack — in the core
  - MessagePack, protobuf-net, Google.Protobuf — opt-in plugin packages (since 3.1.0), so SignalR-style, protobuf-net.Grpc and proto-first gRPC models move over unchanged and nobody else carries those dependencies
- **Resilience**:
  - Auto-reconnection with configurable retry strategies
  - Retry policies for failed calls — retries apply to client-local failures (`Timeout`, `TransportError`) and only to methods declared idempotent; a stable `InvocationId` plus server-side de-duplication means a retried call never executes twice
- **Health Checks**: Built-in health check support for ASP.NET Core applications
- **Cross-Platform Support**: Works across all .NET-supported platforms, including Blazor WebAssembly.
- **Extensibility**: Default implementations for serialization, encryption, and authorization can be replaced with custom ones.

## Packages

### Core
| Package | Description | NuGet |
|---------|-------------|-------|
| `OutWit.Communication` | Core communication library | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.svg)](https://www.nuget.org/packages/OutWit.Communication/) |
| `OutWit.Communication.Client` | Base client library | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.svg)](https://www.nuget.org/packages/OutWit.Communication.Client/) |
| `OutWit.Communication.Server` | Base server library | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.svg)](https://www.nuget.org/packages/OutWit.Communication.Server/) |

### Client Transports
| Package | Description | NuGet |
|---------|-------------|-------|
| `OutWit.Communication.Client.Pipes` | Named Pipes transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.Pipes.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.Pipes/) |
| `OutWit.Communication.Client.Tcp` | TCP transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.Tcp.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.Tcp/) |
| `OutWit.Communication.Client.WebSocket` | WebSocket transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.WebSocket.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.WebSocket/) |
| `OutWit.Communication.Client.MMF` | Memory-Mapped Files transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.MMF.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.MMF/) |
| `OutWit.Communication.Client.Rest` | REST compatibility layer (plain HTTP + JSON, stateless) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.Rest.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.Rest/) |

### Server Transports
| Package | Description | NuGet |
|---------|-------------|-------|
| `OutWit.Communication.Server.Pipes` | Named Pipes transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.Pipes.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.Pipes/) |
| `OutWit.Communication.Server.Tcp` | TCP transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.Tcp.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.Tcp/) |
| `OutWit.Communication.Server.WebSocket` | WebSocket transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.WebSocket.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.WebSocket/) |
| `OutWit.Communication.Server.MMF` | Memory-Mapped Files transport | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.MMF.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.MMF/) |
| `OutWit.Communication.Server.Rest` | REST compatibility layer (plain HTTP + JSON, own host) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.Rest.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.Rest/) |

### Extensions
| Package | Description | NuGet |
|---------|-------------|-------|
| `OutWit.Communication.Client.DynamicProxy` | Runtime dynamic proxies (Castle.Core) for `GetService<T>()` | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.DynamicProxy.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.DynamicProxy/) |
| `OutWit.Communication.Client.DependencyInjection` | DI support for client | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.DependencyInjection.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.DependencyInjection/) |
| `OutWit.Communication.Server.DependencyInjection` | DI support for server | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.DependencyInjection.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.DependencyInjection/) |
| `OutWit.Communication.Client.HealthChecks` | Health checks for client | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.HealthChecks.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.HealthChecks/) |
| `OutWit.Communication.Client.Encryption.BouncyCastle` | Cross-platform encryption (Blazor WASM) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.Encryption.BouncyCastle.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.Encryption.BouncyCastle/) |
| `OutWit.Communication.Client.Blazor` | Blazor WebAssembly channel factory (WebSocket + Web Crypto API encryption) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Client.Blazor.svg)](https://www.nuget.org/packages/OutWit.Communication.Client.Blazor/) |
| `OutWit.Communication.Server.Encryption.BouncyCastle` | Cross-platform encryption | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Server.Encryption.BouncyCastle.svg)](https://www.nuget.org/packages/OutWit.Communication.Server.Encryption.BouncyCastle/) |

### Serializer Plugins (client + server)
| Package | Description | NuGet |
|---------|-------------|-------|
| `OutWit.Communication.Serializers.MessagePack` | MessagePack-CSharp payloads (SignalR-style models) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Serializers.MessagePack.svg)](https://www.nuget.org/packages/OutWit.Communication.Serializers.MessagePack/) |
| `OutWit.Communication.Serializers.ProtoBuf` | protobuf-net payloads (code-first, protobuf-net.Grpc models) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Serializers.ProtoBuf.svg)](https://www.nuget.org/packages/OutWit.Communication.Serializers.ProtoBuf/) |
| `OutWit.Communication.Serializers.GoogleProtobuf` | Google.Protobuf payloads (proto-first gRPC, protoc-generated models) | [![NuGet](https://img.shields.io/nuget/v/OutWit.Communication.Serializers.GoogleProtobuf.svg)](https://www.nuget.org/packages/OutWit.Communication.Serializers.GoogleProtobuf/) |

### InterProcess
| Package | Description | NuGet |
|---------|-------------|-------|
| `OutWit.InterProcess` | Core inter-process communication | [![NuGet](https://img.shields.io/nuget/v/OutWit.InterProcess.svg)](https://www.nuget.org/packages/OutWit.InterProcess/) |
| `OutWit.InterProcess.Host` | Host for inter-process communication | [![NuGet](https://img.shields.io/nuget/v/OutWit.InterProcess.Host.svg)](https://www.nuget.org/packages/OutWit.InterProcess.Host/) |
| `OutWit.InterProcess.Agent` | Agent for inter-process communication | [![NuGet](https://img.shields.io/nuget/v/OutWit.InterProcess.Agent.svg)](https://www.nuget.org/packages/OutWit.InterProcess.Agent/) |

## Getting Started

### Installation

Install the transport package you need. For example, for WebSocket:

```bash
# Server
dotnet add package OutWit.Communication.Server.WebSocket

# Client
dotnet add package OutWit.Communication.Client.WebSocket
```

### Defining an Interface

```csharp
public interface IExampleService
{
    event Action ProcessingStarted;
    event Action<double> ProgressChanged;
    event Action<string> ProcessingCompleted;

    bool StartProcessing();
    void StopProcessing();
    Task<string> ProcessDataAsync(string data);
}
```

### Implementing the Service

```csharp
public class ExampleService : IExampleService
{
    public event Action ProcessingStarted = delegate { };
    public event Action<double> ProgressChanged = delegate { };
    public event Action<string> ProcessingCompleted = delegate { };

    public bool StartProcessing()
    {
        ProcessingStarted();
        return true;
    }

    public void StopProcessing() { }

    public async Task<string> ProcessDataAsync(string data)
    {
        await Task.Delay(100);
        return $"Processed: {data}";
    }
}
```

### Setting Up the Server

```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithService(new ExampleService());
    options.WithWebSocket("http://localhost:5000", maxClients: 100);
    options.WithJson();
    options.WithEncryption();
    options.WithAccessToken("your-secret-token");
});

server.StartWaitingForConnection();
```

### Connecting the Client

```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithEncryption();
    options.WithAccessToken("your-secret-token");
});

await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
var service = client.GetService<IExampleService>();

// Subscribe to events
service.ProcessingStarted += () => Console.WriteLine("Processing Started");
service.ProgressChanged += progress => Console.WriteLine($"Progress: {progress}%");
service.ProcessingCompleted += result => Console.WriteLine($"Completed: {result}");

// Call methods
service.StartProcessing();
var result = await service.ProcessDataAsync("Hello");
```

> `client.GetService<T>()` generates the proxy at runtime and requires the `OutWit.Communication.Client.DynamicProxy` package. AOT/trimmed clients use source-generated proxies instead: mark the interface with `[ProxyTarget("ExampleServiceProxy")]` (see `OutWit.Common.Proxy.Generator`) and call `client.GetService<IExampleService>(interceptor => new ExampleServiceProxy(interceptor))` — no extra package needed.

## Advanced Features

### Composite Services (Multiple Interfaces)

Host multiple service interfaces on a single server:

```csharp
// Server
var server = WitServerBuilder.Build(options =>
{
    options.WithServices()
        .AddService<IUserService>(new UserService())
        .AddService<IOrderService>(new OrderService())
        .AddService<INotificationService>(new NotificationService())
        .Build();
    
    options.WithTcp(5000, maxClients: 100);
    options.WithJson();
});

// Client
var userService = client.GetService<IUserService>();
var orderService = client.GetService<IOrderService>();
```

### Blazor WebAssembly Support

Use BouncyCastle encryption for Blazor WebAssembly clients:

```csharp
// Client (Blazor WebAssembly)
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://server:5000");
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Works in WASM!
});

// Server (must also use BouncyCastle)
var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://0.0.0.0:5000", maxClients: 100);
    options.WithJson();
    options.WithBouncyCastleEncryption();
    options.WithService(myService);
});
```

### Auto-Reconnection

Enable automatic reconnection for resilient connections:

```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithTcp("localhost", 5000);
    options.WithJson();
    options.WithAutoReconnect(reconnect =>
    {
        reconnect.MaxAttempts = 5;
        reconnect.InitialDelay = TimeSpan.FromSeconds(1);
        reconnect.MaxDelay = TimeSpan.FromSeconds(30);
    });
});
```

## Documentation

For more detailed documentation, visit [witrpc.io](https://witrpc.io/).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a list of changes.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use WitRPC in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.

# OutWit.Communication.Client.DependencyInjection

Microsoft.Extensions.DependencyInjection integration for WitRPC client, providing easy registration and lifecycle management of WitRPC client services.

### Overview

**OutWit.Communication.Client.DependencyInjection** provides seamless integration between WitRPC clients and the Microsoft Dependency Injection container. It allows you to register WitRPC clients as named services, manage their lifecycle through hosted services, and inject service proxies directly into your application components.

Key features:
- **Named client registration** - Register multiple WitRPC clients with different configurations
- **Typed service registration** - Inject service interfaces directly into your classes
- **Service resolution from DI** - Access `IServiceProvider` during configuration to resolve loggers, tokens, etc.
- **Auto-connect support** - Automatically connect clients when the application starts
- **Factory pattern** - Use `IWitClientFactory` to create and manage clients dynamically

### Installation

```shell
Install-Package OutWit.Communication.Client.DependencyInjection
```

### Usage

#### Basic Registration

Register a WitRPC client with a name:

```csharp
services.AddWitRpcClient("my-service", ctx =>
{
    ctx.WithWebSocket("ws://localhost:5000");
    ctx.WithJson();
    ctx.WithEncryption();
    ctx.WithAccessToken("your-token");
});
```

#### Typed Service Registration

Register a client and automatically create a service proxy:

```csharp
services.AddWitRpcClient<IMyService>("my-service", ctx =>
{
    ctx.WithNamedPipe("MyServicePipe");
    ctx.WithJson();
    ctx.WithEncryption();
});

// Now you can inject IMyService directly
public class MyController
{
    private readonly IMyService _service;
    
    public MyController(IMyService service)
    {
        _service = service;
    }
}
```

#### Auto-Connect on Startup

Enable automatic connection when the application starts:

```csharp
services.AddWitRpcClient("my-service", ctx =>
{
    ctx.WithTcp("127.0.0.1", 5000);
    ctx.WithJson();
    ctx.WithAutoReconnect(); // Enable auto-reconnect
}, autoConnect: true, connectionTimeout: TimeSpan.FromSeconds(30));
```

The `autoConnect` parameter registers an `IHostedService` that connects the client during application startup.
You can also combine typed registration with auto-connect:

```csharp
services.AddWitRpcClient<IMyService>("my-service", ctx =>
{
    ctx.WithWebSocket("ws://localhost:5000");
    ctx.WithJson();
    ctx.WithEncryption();
}, autoConnect: true, connectionTimeout: TimeSpan.FromSeconds(10));
```

#### Using the Factory

For more control, use `IWitClientFactory`:

```csharp
public class MyService
{
    private readonly IWitClientFactory _factory;
    
    public MyService(IWitClientFactory factory)
    {
        _factory = factory;
    }
    
    public async Task DoWork()
    {
        var client = _factory.GetClient("my-service");
        await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        
        var service = _factory.GetService<IMyService>("my-service");
        var result = await service.GetDataAsync();
    }
}
```

#### Multiple Clients

Register multiple clients with different configurations:

```csharp
services.AddWitRpcClient("service-a", ctx =>
{
    ctx.WithWebSocket("ws://server-a:5000");
    ctx.WithJson();
});

services.AddWitRpcClient("service-b", ctx =>
{
    ctx.WithTcp("server-b", 6000);
    ctx.WithMessagePack();
});

services.AddWitRpcClient("service-c", ctx =>
{
    ctx.WithNamedPipe("MyLocalPipe");
    ctx.WithProtoBuf();
});
```

#### With Service Provider Access

Access the service provider during configuration to resolve dependencies like loggers, token providers, or configuration:

```csharp
services.AddWitRpcClient("my-service", ctx =>
{
    var config = ctx.ServiceProvider.GetRequiredService<IConfiguration>();
    
    ctx.WithWebSocket(config["WitRpc:Url"]!);
    ctx.WithJson();
    ctx.WithEncryption();
    ctx.WithLogger<ILogger<MyApp>>();
    ctx.WithAccessTokenProvider<IMyTokenProvider>();
});
```

The `WitClientBuilderContext` inherits from `WitClientBuilderOptions` and adds `IServiceProvider` (via `ctx.ServiceProvider`), so all standard builder extension methods (e.g. `WithJson`, `WithNamedPipe`, `WithTcp`) work directly on the context. Additional extension methods can resolve services without explicitly passing the service provider:

| Method | Resolves |
|--------|----------|
| `ctx.WithLogger<T>()` | Logger instance (e.g. `ILogger<MyApp>`) |
| `ctx.WithLogger(categoryName)` | Logger via `ILoggerFactory` |
| `ctx.WithAccessTokenProvider<T>()` | `IAccessTokenProvider` implementation |
| `ctx.WithEncryptor<T>()` | `IEncryptorClient` implementation |

This also works with typed registration and auto-connect:

```csharp
services.AddWitRpcClient<IMyService>("my-service", ctx =>
{
    ctx.WithTcp("127.0.0.1", 5000);
    ctx.WithJson();
    ctx.WithLogger<ILogger<MyApp>>();
}, autoConnect: true, connectionTimeout: TimeSpan.FromSeconds(30));
```

### Further Documentation

For more about WitRPC and dependency injection patterns, see the official documentation on [witrpc.io](https://witrpc.io/).

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Client.DependencyInjection in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
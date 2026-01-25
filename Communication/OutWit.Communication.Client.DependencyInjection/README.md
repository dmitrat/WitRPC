# OutWit.Communication.Client.DependencyInjection

Microsoft.Extensions.DependencyInjection integration for WitRPC client, providing easy registration and lifecycle management of WitRPC client services.

### Overview

**OutWit.Communication.Client.DependencyInjection** provides seamless integration between WitRPC clients and the Microsoft Dependency Injection container. It allows you to register WitRPC clients as named services, manage their lifecycle through hosted services, and inject service proxies directly into your application components.

Key features:
- **Named client registration** - Register multiple WitRPC clients with different configurations
- **Typed service registration** - Inject service interfaces directly into your classes
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
services.AddWitRpcClient("my-service", options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithEncryption();
    options.WithAccessToken("your-token");
});
```

#### Typed Service Registration

Register a client and automatically create a service proxy:

```csharp
services.AddWitRpcClient<IMyService>("my-service", options =>
{
    options.WithNamedPipe("MyServicePipe");
    options.WithJson();
    options.WithEncryption();
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
services.AddWitRpcClient("my-service", options =>
{
    options.WithTcp("127.0.0.1", 5000);
    options.WithJson();
    options.WithAutoReconnect(); // Enable auto-reconnect
}, autoConnect: true, connectionTimeout: TimeSpan.FromSeconds(30));
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
services.AddWitRpcClient("service-a", options =>
{
    options.WithWebSocket("ws://server-a:5000");
    options.WithJson();
});

services.AddWitRpcClient("service-b", options =>
{
    options.WithTcp("server-b", 6000);
    options.WithMessagePack();
});
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
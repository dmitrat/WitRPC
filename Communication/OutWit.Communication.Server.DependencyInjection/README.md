# OutWit.Communication.Server.DependencyInjection

Microsoft.Extensions.DependencyInjection integration for WitRPC server, providing easy registration and lifecycle management of WitRPC server services.

### Overview

**OutWit.Communication.Server.DependencyInjection** provides seamless integration between WitRPC servers and the Microsoft Dependency Injection container. It allows you to register WitRPC servers as named services, manage their lifecycle through hosted services, and resolve service implementations from the DI container.

Key features:
- **Named server registration** - Register multiple WitRPC servers with different configurations
- **Service resolution from DI** - Automatically resolve service implementations from the container
- **Composite services with DI** - Register multiple service interfaces with automatic DI resolution
- **Auto-start support** - Automatically start servers when the application starts
- **Factory pattern** - Use `IWitServerFactory` to create and manage servers dynamically

### Installation

```shell
Install-Package OutWit.Communication.Server.DependencyInjection
```

### Usage

#### Basic Registration

Register a WitRPC server with a name:

```csharp
services.AddWitRpcServer("my-server", options =>
{
    options.WithWebSocket("http://localhost:5000", maxNumberOfClients: 100);
    options.WithJson();
    options.WithEncryption();
    options.WithAccessToken("server-token");
});
```

#### Typed Service Registration

Register a server with a service implementation from DI:

```csharp
// Register your service implementation
services.AddSingleton<IMyService, MyServiceImpl>();

// Register the WitRPC server
services.AddWitRpcServer<IMyService, MyServiceImpl>("my-server", options =>
{
    options.WithNamedPipe("MyServicePipe", maxNumberOfClients: 10);
    options.WithJson();
    options.WithEncryption();
});
```

#### Composite Services with DI (Multiple Interfaces)

Register multiple service interfaces on a single server, with automatic resolution from DI:

```csharp
// Option 1: Services already registered in DI
services.AddSingleton<IUserService, UserServiceImpl>();
services.AddSingleton<IOrderService, OrderServiceImpl>();
services.AddSingleton<INotificationService, NotificationServiceImpl>();

services.AddWitRpcServerWithServices("api-server",
    options =>
    {
        options.WithTcp(5000, maxNumberOfClients: 100);
        options.WithJson();
        options.WithEncryption();
    },
    composite =>
    {
        composite.AddService<IUserService>();       // Resolved from DI
        composite.AddService<IOrderService>();      // Resolved from DI
        composite.AddService<INotificationService>(); // Resolved from DI
    });
```

```csharp
// Option 2: Register services and add to composite in one step
services.AddWitRpcServerWithServices("api-server",
    options =>
    {
        options.WithTcp(5000, maxNumberOfClients: 100);
        options.WithJson();
    },
    composite =>
    {
        // Registers implementation in DI AND adds to composite
        composite.AddService<IUserService, UserServiceImpl>();
        composite.AddService<IOrderService, OrderServiceImpl>();
        composite.AddService<INotificationService, NotificationServiceImpl>();
    });
```

```csharp
// Option 3: Use factory functions
services.AddWitRpcServerWithServices("api-server",
    options =>
    {
        options.WithTcp(5000, maxNumberOfClients: 100);
        options.WithJson();
    },
    composite =>
    {
        composite.AddService<IUserService>(sp => 
            new UserServiceImpl(sp.GetRequiredService<ILogger<UserServiceImpl>>()));
        composite.AddService<IOrderService>(sp => 
            new OrderServiceImpl(sp.GetRequiredService<IDbContext>()));
    });
```

Clients can then access any registered service:

```csharp
var userService = client.GetService<IUserService>();
var orderService = client.GetService<IOrderService>();
```

#### Auto-Start on Startup

Enable automatic server start when the application starts:

```csharp
services.AddWitRpcServer("my-server", options =>
{
    options.WithTcp(5000, maxNumberOfClients: 50);
    options.WithJson();
}, autoStart: true);

// Also works with typed registration
services.AddWitRpcServer<IMyService, MyServiceImpl>("my-server", options =>
{
    options.WithNamedPipe("MyServicePipe", maxNumberOfClients: 10);
    options.WithJson();
}, autoStart: true);

// Also works with composite services
services.AddWitRpcServerWithServices("api-server",
    options => { /* ... */ },
    composite => { /* ... */ },
    autoStart: true);
```

#### Using the Factory

For more control, use `IWitServerFactory`:

```csharp
public class ServerManager
{
    private readonly IWitServerFactory _factory;
    
    public ServerManager(IWitServerFactory factory)
    {
        _factory = factory;
    }
    
    public void StartServer()
    {
        var server = _factory.GetServer("my-server");
        server.StartWaitingForConnection();
    }
    
    public void StopServer()
    {
        var server = _factory.GetServer("my-server");
        server.StopWaitingForConnection();
    }
}
```

#### Multiple Servers

Register multiple servers with different configurations:

```csharp
services.AddWitRpcServer("tcp-server", options =>
{
    options.WithTcp(5000, maxNumberOfClients: 100);
    options.WithJson();
}, autoStart: true);

services.AddWitRpcServer("websocket-server", options =>
{
    options.WithWebSocket("http://localhost:8080", maxNumberOfClients: 1000);
    options.WithMessagePack();
}, autoStart: true);

services.AddWitRpcServer("pipe-server", options =>
{
    options.WithNamedPipe("MyServicePipe", maxNumberOfClients: 10);
    options.WithProtoBuf();
}, autoStart: true);
```

#### With Service Provider Access

Access the service provider during configuration:

```csharp
services.AddWitRpcServer("my-server", (options, serviceProvider) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var port = config.GetValue<int>("WitRpc:Port");
    
    options.WithTcp(port, maxNumberOfClients: 100);
    options.WithJson();
});
```

### Further Documentation

For more about WitRPC and dependency injection patterns, see the official documentation on [witrpc.io](https://witrpc.io/).

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server.DependencyInjection in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
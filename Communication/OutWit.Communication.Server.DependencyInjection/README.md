# OutWit.Communication.Server.DependencyInjection

Microsoft.Extensions.DependencyInjection integration for WitRPC server, providing easy registration and lifecycle management of WitRPC server services.

### Overview

**OutWit.Communication.Server.DependencyInjection** provides seamless integration between WitRPC servers and the Microsoft Dependency Injection container. It allows you to register WitRPC servers as named services, manage their lifecycle through hosted services, and resolve service implementations from the DI container.

Key features:
- **Named server registration** - Register multiple WitRPC servers with different configurations
- **Service resolution from DI** - Automatically resolve service implementations from the container
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
    options.WithWebSocket("http://localhost:5000", maxClients: 100);
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
    options.WithNamedPipe("MyServicePipe", maxClients: 10);
    options.WithJson();
    options.WithEncryption();
});
```

#### Auto-Start on Startup

Enable automatic server start when the application starts:

```csharp
services.AddWitRpcServer("my-server", options =>
{
    options.WithTcp(5000, maxClients: 50);
    options.WithJson();
}, autoStart: true);
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
    options.WithTcp(5000, maxClients: 100);
    options.WithJson();
}, autoStart: true);

services.AddWitRpcServer("websocket-server", options =>
{
    options.WithWebSocket("http://localhost:8080", maxClients: 1000);
    options.WithMessagePack();
}, autoStart: true);
```

#### With Service Provider Access

Access the service provider during configuration:

```csharp
services.AddWitRpcServer("my-server", (options, serviceProvider) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var port = config.GetValue<int>("WitRpc:Port");
    
    options.WithTcp(port, maxClients: 100);
    options.WithJson();
});
```

### Further Documentation

For more about WitRPC and dependency injection patterns, see the official documentation on [witrpc.io](https://witrpc.io/).
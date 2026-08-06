# Changelog

All notable changes to the WitRPC project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.3] - 2026-04-23

### Fixed

- **Empty or undeserializable payloads crashed the client.** `WitClient` threw
  `WitException("Received empty message")` when a transport handed it an empty
  buffer, which tore down the receive loop instead of skipping the frame. Empty
  payloads, and payloads that do not deserialize into a `WitMessage`, are now logged
  and ignored. WebSocket client and server transports no longer forward empty frames
  in the first place. Adds `WitClientIncomingPayloadTests`.

## [2.3.2] - 2026-04-22

### Added

#### Blazor WebAssembly client

- **New Package**: `OutWit.Communication.Client.Blazor` - a channel factory for
  Blazor WebAssembly clients. `IChannelFactory` / `ChannelFactory` with
  `ChannelFactoryOptions`, `ChannelReconnectOptions`, `ChannelRetryOptions` and
  `ChannelTokenProvider`, plus `ServiceCollectionExtensions` for one-line
  registration. Browser-side encryption runs through `EncryptorClientWeb` over the
  Web Crypto API, so no BouncyCastle assembly is needed in the browser.
- The channel factory accepts a custom URL, and exposes a `BufferSize` option.

#### Dependency injection surface reworked

- New types on both sides: `IConfigureWitClient` / `ConfigureWitClient` and
  `IConfigureWitServer` / `ConfigureWitServer`, `IWitClientFactory` /
  `IWitServerFactory`, the builder contexts `WitClientBuilderContext` and
  `WitServerBuilderContext` with their extension methods, and
  `WitClientHostedServiceOptions` / `WitServerHostedServiceOptions`.
- The registration extensions were simplified around those contexts and a redundant
  overload was removed.

### Fixed

- **Stale frames after a disconnect.** `WitServer` now resolves the connection
  through `TryGetConnection` and ignores messages addressed to a disconnected or
  unknown client, instead of processing them against missing state. Initialization,
  authorization and encryption now take the resolved connection explicitly.
- **Transport lifecycle across a restart.** `ITransportServerFactory` is now
  `IDisposable`, and the memory-mapped file, named pipe, TCP and WebSocket server
  transport factories release their listeners on disposal. `WitServer.Dispose` became
  idempotent: it unsubscribes from the transport factory, the request processor and
  the discovery server, and stops listening. Adds `TransportFactoryLifecycleTests`.
- Crypto interop fix in the Blazor client.
- Server-side service resolution from the DI container.

## [2.3.1] - 2026-01-25

### Changed

- Licensing and packaging metadata across every package: `PackageLicenseExpression`
  set to `Apache-2.0`, a `NOTICE` file added to each package, and `LICENSE`, README
  and csproj package metadata refreshed. No functional changes.

## [2.3.0] - 2025-12-09

### Added

#### BouncyCastle Cross-Platform Encryption
- **New Package**: `OutWit.Communication.Client.Encryption.BouncyCastle` - Cross-platform encryption client using BouncyCastle cryptography library
- **New Package**: `OutWit.Communication.Server.Encryption.BouncyCastle` - Cross-platform encryption server using BouncyCastle cryptography library
- Pure C# implementation that works everywhere, including **Blazor WebAssembly** without JavaScript interop
- RSA-OAEP (SHA-256) for key exchange, AES-256-CBC for symmetric encryption
- Extension methods `WithBouncyCastleEncryption()` for easy configuration

```csharp
// Client (works in Blazor WebAssembly!)
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Cross-platform encryption
});

// Server
var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://localhost:5000", maxClients: 100);
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Must match client
    options.WithService(myService);
});
```

> **Important**: BouncyCastle encryption is NOT compatible with standard .NET encryption. Both client and server must use `WithBouncyCastleEncryption()`.

#### Composite Services (Multiple Interfaces per Server)
- **New Class**: `CompositeRequestProcessor` - Request processor that handles multiple service interfaces
- **New Builder**: `CompositeServiceBuilder` - Fluent builder for registering multiple services
- Allows clients to request proxies for different interfaces from a single server connection
- Eliminates the need to create "super-interfaces" that inherit from multiple service interfaces

```csharp
// Server: Register multiple services
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

// Client: Access any registered service
var userService = client.GetService<IUserService>();
var orderService = client.GetService<IOrderService>();
var notificationService = client.GetService<INotificationService>();
```

#### Composite Services with Dependency Injection
- **New Method**: `AddWitRpcServerWithServices()` - Register composite services with automatic DI resolution
- **New Class**: `CompositeServiceRegistration` - Helper for configuring composite services in DI context
- Services can be resolved from DI container or registered inline
- Three registration patterns: pre-registered services, inline registration, and factory functions

```csharp
// Option 1: Services already registered in DI
services.AddSingleton<IUserService, UserServiceImpl>();
services.AddSingleton<IOrderService, OrderServiceImpl>();

services.AddWitRpcServerWithServices("api-server",
    options =>
    {
        options.WithTcp(5000, maxClients: 100);
        options.WithJson();
    },
    svcs =>
    {
        svcs.AddService<IUserService>();    // Resolved from DI
        svcs.AddService<IOrderService>();   // Resolved from DI
    });

// Option 2: Register and add in one step
services.AddWitRpcServerWithServices("api-server",
    options => { /* ... */ },
    svcs =>
    {
        svcs.AddService<IUserService, UserServiceImpl>();
        svcs.AddService<IOrderService, OrderServiceImpl>();
    });

// Option 3: Factory functions
services.AddWitRpcServerWithServices("api-server",
    options => { /* ... */ },
    svcs =>
    {
        svcs.AddService<IUserService>(sp => new UserServiceImpl(
            sp.GetRequiredService<ILogger<UserServiceImpl>>()));
    });
```

#### Dependency Injection Integration
- **New Package**: `OutWit.Communication.Client.DependencyInjection` - Microsoft.Extensions.DependencyInjection support for WitRPC clients
- **New Package**: `OutWit.Communication.Server.DependencyInjection` - Microsoft.Extensions.DependencyInjection support for WitRPC servers
- Seamless integration with ASP.NET Core and other DI-based applications
- Extension methods for `IServiceCollection`

```csharp
// Register WitRPC client with DI
services.AddWitRpcClient("my-client", (options, sp) =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithEncryption();
});

// Inject and use
public class MyController
{
    private readonly IWitClient _client;
    
    public MyController(IWitClientFactory factory)
    {
        _client = factory.GetClient("my-client");
    }
}
```

#### Auto-Reconnection
- **New Class**: `ReconnectionOptions` - Configurable reconnection settings
- Automatic reconnection with exponential backoff
- Configurable max attempts, initial delay, max delay, and jitter
- Extension method `WithAutoReconnect()` for easy configuration

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
        reconnect.UseJitter = true;
    });
});
```

#### Retry Policy / Resilience
- **New Class**: `RetryPolicy` - Configurable retry settings for failed RPC calls
- Support for fixed and exponential backoff strategies
- Configurable retry conditions (which exceptions to retry)
- Extension method `WithRetryPolicy()` for easy configuration

```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithTcp("localhost", 5000);
    options.WithJson();
    options.WithRetryPolicy(retry =>
    {
        retry.MaxRetries = 3;
        retry.InitialDelay = TimeSpan.FromMilliseconds(100);
        retry.BackoffMultiplier = 2.0;
        retry.MaxDelay = TimeSpan.FromSeconds(5);
    });
});
```

#### Health Checks
- **New Package**: `OutWit.Communication.Client.HealthChecks` - ASP.NET Core Health Checks support
- Monitor WitRPC client connection status
- Integration with standard ASP.NET Core health check infrastructure

```csharp
// Register health check
services.AddHealthChecks()
    .AddWitRpcClientCheck("my-client", tags: new[] { "rpc", "ready" });

// Use in ASP.NET Core
app.MapHealthChecks("/health");
```

#### Server Encryption Interface Enhancement
- Added `EncryptForClient(byte[] data, byte[] clientPublicKey)` method to `IEncryptorServer` interface
- Enables server-side encryption implementations to encrypt data using client's public key
- Required for BouncyCastle encryption support

#### CI/CD
- **New Workflow**: `publish-package.yml` - GitHub Action for publishing individual NuGet packages
- **New Workflow**: `publish-all-packages.yml` - GitHub Action for publishing all packages in parallel
- Supports publishing to both nuget.org and GitHub Packages
- Includes all Communication and InterProcess packages

#### Test Infrastructure
- Added `[assembly: Parallelizable(ParallelScope.None)]` to disable parallel test execution
- Prevents port/resource conflicts when running tests in bulk
- New test interfaces and implementations for composite service testing
- Integration tests for BouncyCastle encryption and composite services

### Changed

#### Documentation
- Updated `README.md` for `OutWit.Communication.Server` with composite services documentation
- Updated `README.md` for `OutWit.Communication.Client` with multiple service access documentation
- Updated `README.md` for `OutWit.Communication.Server.DependencyInjection` with composite services DI documentation
- Created comprehensive README files for BouncyCastle encryption packages with compatibility tables
- Updated main `README.md` with full package list, badges, and advanced features documentation
- Created `CHANGELOG.md` for tracking version history

### Fixed

- Improved test stability when running multiple tests sequentially

---

## [2.2.0] - 2025-11-14

*See repository history for previous changes.*

# Changelog

All notable changes to the WitRPC project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **Note**: Since 2.3.1, package versions diverge per package family. Each section below lists the package versions it produced (verified against csproj `<Version>` values).

## [2.3.3] - 2026-04-23

Package versions after this release: `OutWit.Communication` + client packages 2.3.3, server packages 2.3.4, `Client.HealthChecks` 2.3.4, `Client.DependencyInjection` 2.3.7, `Server.DependencyInjection` 2.3.9, `Client.Blazor` 1.0.6. (`OutWit.InterProcess.*` remain 2.3.1.)

### Fixed

- **Empty response crash** (commit `cb4a68d`): `WitClient` no longer crashes on empty or malformed incoming payloads; WebSocket client and server transports skip empty frames.
- New regression test suite `WitClientIncomingPayloadTests` covering empty/garbage incoming payload handling.

## [2.3.2] - 2026-04-22

> **Highlighted: transport restart lifecycle fix.** Server transports previously could not be stopped and restarted on the same endpoint — `WebSocketServerTransportFactory` and `TcpServerTransportFactoryBase` created their listener in the constructor and never released it in `StopWaitingForConnection()`, and `WitServer.Dispose()` never disposed the transport factory. This broke dynamic per-client server recreation after a host restart (observed downstream in OutWit.Cloud).

Package versions after this release: core/client packages 2.3.2, server packages 2.3.3, `Client.DependencyInjection` 2.3.6, `Server.DependencyInjection` 2.3.8, `Client.Blazor` 1.0.5.

### Changed

- **Breaking (interface)**: `ITransportServerFactory` now extends `IDisposable` (commit `8b8501c`); custom transport factories must implement `Dispose()`.

### Fixed

- `WitServer.Dispose()` now stops and disposes its transport factory.
- Restart-safe listener lifecycle for WebSocket (`HttpListener`), TCP (`TcpListener`), Named Pipes, and Memory-Mapped Files server transport factories: `start -> stop/dispose -> recreate -> start` on the same endpoint/port/name now works.
- Listener bind now happens synchronously in `StartWaitingForConnection()` — bind failures surface immediately instead of being swallowed in a background accept loop.
- New regression test suite `TransportFactoryLifecycleTests` (restart cycles for all server transports) plus a server-level test verifying `WitServer.Dispose()` disposes the factory.

Known remaining gap: the REST server (`WitServerRest`) lifecycle was not reworked in this pass.

## [Server 2.3.2] - 2026-03-20

Server packages only: 2.3.1 -> 2.3.2 (`Server.DependencyInjection` -> 2.3.7).

### Fixed

- Hardened `WitServer` against stale frames arriving after a client disconnect (commit `173744a`).
- New regression test suite `WitServerTransportEdgeCaseTests`.

## [Client.Blazor 1.0.1-1.0.4] - 2026-02-10 to 2026-02-20

### Added

- **New Package**: `OutWit.Communication.Client.Blazor` (1.0.1, commit `8b8530e`) — Blazor WebAssembly channel factory for WitRPC over WebSocket with RSA/AES encryption via the Web Crypto API.
- 1.0.2 (`654db14`): custom URL option.
- 1.0.4 (`2edbb8b`, 2026-02-20): configurable `BufferSize` option.

### Fixed

- 1.0.3 (`a12e3fb`, 2026-02-14): crypto interop fix.

## [DependencyInjection 2.3.2-2.3.6] - 2026-02-06

`Client.DependencyInjection` 2.3.1 -> 2.3.5, `Server.DependencyInjection` 2.3.1 -> 2.3.6; other packages unchanged.

### Changed

- Dependency Injection refactoring (`f7b8aab`), additional DI extension methods (`fd21284`), redundant overload removed (`4b582a2`), DI usage simplified (`833a08f`).

### Fixed

- Server DI service resolution fix (`a8a4408`).

## [2.3.1] - 2026-01-25

All packages 2.3.0 -> 2.3.1 (including `OutWit.InterProcess.*`, which remain at 2.3.1).

### Changed

- **License**: project relicensed from the Non-Commercial License (NCL) to **Apache License 2.0**; `NOTICE` files added to all packages (commit `0842296`).
- Dependencies updated.

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

- .NET 10 target framework support (commit `68e9601`).

*See repository history for earlier changes.*

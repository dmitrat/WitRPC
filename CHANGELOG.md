# Changelog

All notable changes to the WitRPC project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.0] - 2025-01-XX

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
- Created comprehensive README files for BouncyCastle encryption packages with compatibility tables
- Updated main `README.md` with full package list, badges, and advanced features documentation
- Created `CHANGELOG.md` for tracking version history

### Fixed

- Improved test stability when running multiple tests sequentially

---

## [2.2.0] - Previous Release

*See repository history for previous changes.*

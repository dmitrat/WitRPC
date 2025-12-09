# OutWit.Communication.Server.Encryption.BouncyCastle

Cross-platform BouncyCastle-based encryption for WitRPC server, providing RSA/AES encryption compatible with BouncyCastle clients.

### Overview

**OutWit.Communication.Server.Encryption.BouncyCastle** provides a cross-platform encryption implementation for WitRPC servers using the BouncyCastle cryptography library. This package is **required** when your clients use BouncyCastle encryption (e.g., Blazor WebAssembly clients).

The encryption uses RSA-OAEP (with SHA-256) for secure key exchange and AES-CBC for symmetric message encryption, providing end-to-end encryption for WitRPC communication.

### Installation

```shell
Install-Package OutWit.Communication.Server.Encryption.BouncyCastle
```

### Usage

#### Basic Usage (Recommended)

Use the extension method for the simplest setup:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.Encryption.BouncyCastle;

var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://localhost:5000", maxClients: 100);
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Simple extension method
    options.WithService(myService);
});

server.StartWaitingForConnection();
```

#### With Dependency Injection

```csharp
services.AddSingleton<IEncryptorServerFactory, EncryptorServerBouncyCastleFactory>();

services.AddWitRpcServer("my-server", (options, sp) =>
{
    var encryptorFactory = sp.GetRequiredService<IEncryptorServerFactory>();
    
    options.WithTcp(5000, maxClients: 50);
    options.WithJson();
    options.WithEncryptor(encryptorFactory);
    options.WithService<IMyService>(sp.GetRequiredService<IMyService>());
});
```

### ?? Important: Client Compatibility

**BouncyCastle encryption is NOT compatible with standard .NET encryption.**

When using `WithBouncyCastleEncryption()` on the server, your clients **must** also use `WithBouncyCastleEncryption()`:

```csharp
// Client configuration (required!)
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Must match server!
});
```

| Server Encryption | Client Encryption | Compatible? |
|-------------------|-------------------|-------------|
| `WithBouncyCastleEncryption()` | `WithBouncyCastleEncryption()` | ? Yes |
| `WithBouncyCastleEncryption()` | `WithEncryption()` | ? No |
| `WithEncryption()` | `WithEncryption()` | ? Yes |
| `WithEncryption()` | `WithBouncyCastleEncryption()` | ? No |

### Security Details

- **Key Exchange**: RSA-OAEP with SHA-256
- **Symmetric Encryption**: AES-256-CBC with PKCS7 padding
- **Key Generation**: Secure random using BouncyCastle's SecureRandom

### When to Use BouncyCastle

Use BouncyCastle encryption when:
- Your clients include **Blazor WebAssembly** applications
- You need **cross-platform consistency** across different .NET runtimes
- You want to avoid platform-specific crypto implementations

Use standard encryption (`WithEncryption()`) when:
- All clients run on standard .NET (Windows/Linux/macOS)
- You don't have Blazor WebAssembly clients
- You prefer using .NET's built-in cryptography

### Further Documentation

For more about WitRPC encryption and cross-platform support, see the official documentation on [witrpc.io](https://witrpc.io/).
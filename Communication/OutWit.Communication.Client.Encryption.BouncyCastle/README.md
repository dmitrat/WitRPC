# OutWit.Communication.Client.Encryption.BouncyCastle

Cross-platform BouncyCastle-based encryption for WitRPC client, providing RSA/AES encryption that works in Blazor WebAssembly and all .NET platforms.

### Overview

**OutWit.Communication.Client.Encryption.BouncyCastle** provides a cross-platform encryption implementation using the BouncyCastle cryptography library. Unlike the standard .NET encryption which relies on platform-specific crypto APIs, BouncyCastle is a pure C# implementation that works everywhere, including:

- ? **Blazor WebAssembly** - No JavaScript interop required
- ? **.NET on Windows, Linux, macOS**
- ? **Xamarin/MAUI**
- ? **Unity**

The encryption uses RSA-OAEP (with SHA-256) for secure key exchange and AES-CBC for symmetric message encryption, providing end-to-end encryption for WitRPC communication.

### Installation

```shell
Install-Package OutWit.Communication.Client.Encryption.BouncyCastle
```

### Usage

#### Basic Usage (Recommended)

Use the extension method for the simplest setup:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.Encryption.BouncyCastle;

var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Simple extension method
});

await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
```

#### Blazor WebAssembly Usage

In Blazor WebAssembly, simply use the BouncyCastle encryption - no JavaScript interop needed:

```csharp
// Program.cs
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://your-server:5000");
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Works in WASM!
});
```

### ?? Important: Server Compatibility

**BouncyCastle encryption is NOT compatible with standard .NET encryption.**

When using `WithBouncyCastleEncryption()` on the client, you **must** also use `WithBouncyCastleEncryption()` on the server:

```csharp
// Server configuration (required!)
var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://localhost:5000", maxClients: 100);
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Must match client!
    options.WithService(myService);
});
```

| Client Encryption | Server Encryption | Compatible? |
|-------------------|-------------------|-------------|
| `WithBouncyCastleEncryption()` | `WithBouncyCastleEncryption()` | ? Yes |
| `WithBouncyCastleEncryption()` | `WithEncryption()` | ? No |
| `WithEncryption()` | `WithEncryption()` | ? Yes |
| `WithEncryption()` | `WithBouncyCastleEncryption()` | ? No |

### Security Details

- **Key Exchange**: RSA-OAEP with SHA-256, 2048-bit keys
- **Symmetric Encryption**: AES-256-CBC with PKCS7 padding
- **Key Generation**: Secure random using BouncyCastle's SecureRandom

### When to Use BouncyCastle

Use BouncyCastle encryption when:
- Your client runs in **Blazor WebAssembly**
- You need **cross-platform consistency** across different .NET runtimes
- You want to avoid platform-specific crypto implementations

Use standard encryption (`WithEncryption()`) when:
- Both client and server run on standard .NET (Windows/Linux/macOS)
- You don't need Blazor WebAssembly support
- You prefer using .NET's built-in cryptography

### Further Documentation

For more about WitRPC encryption and cross-platform support, see the official documentation on [witrpc.io](https://witrpc.io/).

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Client.Encryption.BouncyCastle in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
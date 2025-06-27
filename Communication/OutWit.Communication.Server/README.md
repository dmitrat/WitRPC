# OutWit.Communication.Server

## Overview

The `OutWit.Communication.Server` library provides a powerful framework for creating servers in the WitRPC ecosystem. It supports encryption, token-based authorization, and flexible serialization to ensure secure and efficient communication with clients.

## Features

### 1. Authorization
- **AccessTokenValidatorPlain**: No token validation for unauthenticated scenarios.
- **AccessTokenValidatorStatic**: Static token validation for predefined secure access.

#### Example:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithAccessToken("your-access-token");
});
```

### 2. Encryption
- **EncryptorServerPlain**: No encryption for lightweight communication.
- **EncryptorServerGeneral**: AES encryption for secure data exchange.

#### Example:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithEncryption();
});
```

### 3. Serialization
- **JSON**: Default format, ideal for debugging and compatibility.
- **MessagePack**: High-performance, compact binary serialization.

#### Example:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithJson();
});

// Or for MessagePack:
var server = WitServerBuilder.Build(options =>
{
    options.WithMessagePack();
});
```

### 4. Logging and Debugging
Integrate custom logging for debugging and monitoring:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithLogger(new ConsoleLogger());
});
```

### 5. Timeout Configuration
Set request and response timeouts for server operations:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithTimeout(TimeSpan.FromSeconds(30));
});
```

### 6. Service Support
Expose your services seamlessly using the `WithService` configuration.

#### Example:
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
});
```

## Installation

Install the package via NuGet. Note: You also need to install the specific transport package you intend to use (e.g., `OutWit.Communication.Server.Tcp`, `OutWit.Communication.Server.WebSocket`, etc.):
```bash
Install-Package OutWit.Communication.Server
```

## Getting Started

### Basic Setup
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithAccessToken("my-access-token");
    options.WithEncryption();
    options.WithJson();
    options.WithTimeout(TimeSpan.FromSeconds(10));
    options.WithService(new MyService());
});

server.StartWaitingForConnection();
```

### Stopping the Server
```csharp
server.StopWaitingForConnection();
```

### Handling Requests
Define your service to process client requests:
```csharp
public class MyService
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}
```

Clients can invoke this service:
```csharp
var request = new WitRequest
{
    MethodName = "Greet",
    Parameters = new object[] { "John" }
};
```

## API Reference

### `WitServer`
The core class for server-side communication.

#### Key Methods:
- `StartWaitingForConnection`: Begins listening for client connections.
- `StopWaitingForConnection`: Stops the server and releases resources.

### `WitServerBuilder`
A fluent API for configuring and creating `WitServer` instances.

#### Configuration Options:
- `.WithAccessToken`: Sets the access token for authentication.
- `.WithEncryption`: Enables encryption for secure communication.
- `.WithJson`: Configures JSON serialization.
- `.WithMessagePack`: Configures MessagePack serialization.
- `.WithTimeout`: Sets the timeout duration.
- `.WithService`: Exposes a service for client requests.

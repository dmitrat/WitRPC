# OutWit.Communication.Client

## Overview

The `OutWit.Communication.Client` library provides a robust framework for client-side communication in the WitRPC ecosystem. With features like encryption, token-based authorization, and flexible serialization, it enables secure and efficient communication with WitRPC servers.

## Features

### 1. Authorization
- **AccessTokenProviderPlain**: No token, suitable for public or unauthenticated scenarios.
- **AccessTokenProviderStatic**: Static token for secure, predefined access.

#### Example:
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithAccessToken("your-access-token");
});
```

### 2. Encryption
- **EncryptorClientPlain**: No encryption for lightweight communication.
- **EncryptorClientGeneral**: RSA/AES encryption for secure data exchange.

#### Example:
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithEncryption();
});
```

### 3. Serialization
- **JSON**: Default format, ideal for debugging and compatibility.
- **MessagePack**: High-performance, compact binary serialization.

#### Example:
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithJson();
});

// Or for MessagePack:
var client = WitClientBuilder.Build(options =>
{
    options.WithMessagePack();
});
```

### 4. Logging and Debugging
Integrate custom logging for debugging and monitoring:
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithLogger(new ConsoleLogger());
});
```

### 5. Timeout Configuration
Set request and response timeouts for client operations:
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithTimeout(TimeSpan.FromSeconds(30));
});
```

### 6. Proxy for Services
Automatically generates a proxy to interact with server-side services using interfaces:
```csharp
var service = client.GetService<IMyService>();
service.SomeMethod();
```

## Installation

Install the package via NuGet. Note: You also need to install the specific transport package you intend to use (e.g., `OutWit.Communication.Client.Tcp`, `OutWit.Communication.Client.WebSocket`, etc.):
```bash
Install-Package OutWit.Communication.Client
```

## Getting Started

### Basic Setup
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithAccessToken("my-access-token");
    options.WithEncryption();
    options.WithJson();
    options.WithTimeout(TimeSpan.FromSeconds(10));
});

await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
```

### Sending Requests
```csharp
var request = new WitRequest
{
    MethodName = "MyMethod",
    Parameters = new object[] { 1, "parameter" }
};

var response = await client.SendRequest(request);
Console.WriteLine(response.Status);
```

### Handling Events
The client supports events for callbacks and disconnections:
```csharp
client.CallbackReceived += request =>
{
    Console.WriteLine("Callback received!");
};

client.Disconnected += _ =>
{
    Console.WriteLine("Disconnected from server");
};
```

## API Reference

### `WitClient`
The core class for client-side communication.

#### Key Methods:
- `ConnectAsync`: Establishes a connection to the server.
- `SendRequest`: Sends a request and receives a response.
- `GetService`: Generates a client-side proxy for a server-side service.

#### Events:
- `CallbackReceived`: Triggered when a server callback is received.
- `Disconnected`: Triggered when the client disconnects from the server.

### `WitClientBuilder`
A fluent API for configuring and creating `WitClient` instances.

#### Configuration Options:
- `.WithAccessToken`: Sets the access token for authentication.
- `.WithEncryption`: Enables encryption for secure communication.
- `.WithJson`: Configures JSON serialization.
- `.WithMessagePack`: Configures MessagePack serialization.
- `.WithTimeout`: Sets the timeout duration.

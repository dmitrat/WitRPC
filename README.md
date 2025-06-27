# WitRPC

WitRPC is a modern API for client-server communication designed to simplify development and provide a robust, extensible framework. It offers a seamless way to handle real-time and event-driven interactions with minimal setup, acting as a powerful alternative to traditional frameworks like WCF and SignalR.

## Features

- **Dynamic Proxy Mechanism**: Client-side proxies mirror server interfaces, enabling natural interaction with server-side objects via method calls, property access, and event subscriptions.
- **Multiple Transport Options**:
  - Memory-Mapped Files
  - Named Pipes
  - TCP
  - WebSocket
  - REST
- **Security**:
  - End-to-end encryption (AES/RSA)
  - Token-based authorization
- **Serialization**:
  - JSON (default)
  - MessagePack (for high performance and compact payloads)
- **Cross-Platform Support**: Works across all .NET-supported platforms.
- **Extensibility**: Default implementations for serialization, encryption, and authorization can be replaced with custom ones.

## Getting Started

### Installation
Add server transport to server project via NuGet:
```bash
Install-Package OutWit.Communication.Server.MMF
```
or
```bash
Install-Package OutWit.Communication.Server.Pipes
```
or
```bash
Install-Package OutWit.Communication.Server.Tcp
```
or
```bash
Install-Package OutWit.Communication.Server.WebSocket
```
or
```bash
Install-Package OutWit.Communication.Server.Rest
```

Add client transport to client project via NuGet:
```bash
Install-Package OutWit.Communication.Client.MMF
```
or
```bash
Install-Package OutWit.Communication.Client.Pipes
```
or
```bash
Install-Package OutWit.Communication.Client.Tcp
```
or
```bash
Install-Package OutWit.Communication.Client.WebSocket
```

### Defining an Interface
```csharp
public interface IExampleService
{
    event Action ProcessingStarted;
    event Action<double> ProgressChanged;
    event Action<string> ProcessingCompleted;

    bool StartProcessing();
    void StopProcessing();
}
```

### Implementing the Service
```csharp
public class ExampleService : IExampleService
{
    public event Action ProcessingStarted = delegate { };
    public event Action<double> ProgressChanged = delegate { };
    public event Action<string> ProcessingCompleted = delegate { };

    public bool StartProcessing()
    {
        // Implementation logic
        ProcessingStarted();
        return true;
    }

    public void StopProcessing()
    {
        // Stop logic
    }
}
```

### Setting Up the Server
```csharp
var server = WitServerBuilder.Build(options =>
{
    options.WithService(new ExampleService());
    options.WithWebSocket("http://localhost:5000", 10);
    options.WithJson();
    options.WithEncryption();
});

server.StartWaitingForConnection();
```

### Connecting the Client
```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithEncryption();
});

await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
var service = client.GetService<IExampleService>();

service.ProcessingStarted += () => Console.WriteLine("Processing Started");
service.ProgressChanged += progress => Console.WriteLine($"Progress: {progress}%");
service.ProcessingCompleted += result => Console.WriteLine($"Processing Completed: {result}");

service.StartProcessing();
```

## More Info
For more info visit the [ratner.io](https://ratner.io/witcom/).

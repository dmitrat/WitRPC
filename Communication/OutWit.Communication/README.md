
# OutWit.Communication

The core communication library of the WitRPC framework, providing base RPC functionality such as messaging, dynamic proxy support, and extensibility for multiple transports, serialization formats, and encryption.

### Overview

**OutWit.Communication** is the foundation of the **WitRPC** framework. WitRPC is a modern, extensible API for client-server communication that simplifies development and handles real-time, event-driven interactions with minimal setup. This core package defines the fundamental building blocks used by all other WitRPC components, including common interfaces, request/response message models, and the dynamic proxy mechanism that allows you to call service methods or subscribe to events as if the service were local.

**Key Features:**

-   **Dynamic Proxy & Full-Duplex Communication:** Automatically generates client-side proxies that mirror server interfaces, enabling natural method calls and event subscriptions for real-time callbacks. You can invoke methods and handle events from the server just by using a local interface; WitRPC transparently handles the network communication in both directions.
    
-   **Multiple Transport Options:** Transport-agnostic design supporting various communication transports (e.g. Memory-Mapped Files, Named Pipes, TCP, WebSocket, REST). You can choose the best transport for your scenario without changing your service code.
    
-   **Secure & Authorized:** Built-in end-to-end encryption (AES for data encryption with RSA for key exchange) and token-based client authentication to secure communication. These features are easy to enable via configuration options, ensuring only authorized clients connect and that data is protected in transit.
    
-   **Flexible Serialization:** Supports multiple serialization formats for transmitting data. By default, JSON is used for human-readable payloads, but you can switch to high-performance binary formats like MessagePack for efficiency (and other options such as MemoryPack or ProtoBuf in advanced scenarios).
    
-   **Cross-Platform & Extensible:** Works on all .NET-supported platforms (Windows, Linux, macOS, etc.). The framework is highly extensible - you can plug in custom transports, serialization, encryption, or authorization logic by implementing the respective interfaces, if the defaults don't meet your needs.
    
In summary, OutWit.Communication provides the core logic that makes it easy to build robust RPC (remote procedure call) functionality in .NET applications, acting as a powerful alternative to older frameworks like WCF or more limited messaging solutions. It is typically used alongside the client- and server-specific packages that implement actual communication transports.

### Installation

Install the **OutWit.Communication** package via NuGet:

```shell
Install-Package OutWit.Communication
```

Typically, you do not need to install this core package directly if you are using one of the higher-level transport packages (those will bring this in as a dependency). However, it's available if you need direct access to the core functionality or plan to implement a custom transport.

### Basic Usage Example

Using WitRPC involves defining a service interface, implementing it on the server side, and then using a client proxy to call it. Below is a simple example of how to set up a service using WitRPC (with a WebSocket transport as an example):

1.  **Define a Service Interface**. Define a service contract as a C# interface. This interface can include methods and also events for callbacks:
    
    ```csharp
    public interface IExampleService
    {
        // Events for server-to-client notifications
        event Action ProcessingStarted;
        event Action<double> ProgressChanged;
        event Action<string> ProcessingCompleted;
    
        // Methods for client-to-server calls
        bool StartProcessing();
        void StopProcessing();
    }
    ```
    
2.  **Implement the Service**. Implement this interface in your server application. In the implementation, you can raise the events to notify clients, and define the logic for the methods:
    
    ```csharp
    public class ExampleService : IExampleService
    {
        public event Action ProcessingStarted = delegate { };
        public event Action<double> ProgressChanged = delegate { };
        public event Action<string> ProcessingCompleted = delegate { };
    
        public bool StartProcessing()
        {
            // Start some processing logic...
            ProcessingStarted();  // notify clients that processing began
            // (In a real scenario, you'd likely run a background task and periodically report progress via ProgressChanged)
            return true;
        }
    
        public void StopProcessing()
        {
            // Stop the processing logic...
            // (This could signal a cancellation token in a real scenario)
        }
    }
    ```
    
3.  **Start a WitRPC Server**. In your server application (which could be a separate process), use the **WitServerBuilder** to build and start a server with the chosen transport and settings. For example, using a WebSocket transport:
    
    ```csharp
    using OutWit.Communication.Server;
    using OutWit.Communication.Server.WebSocket;
    using OutWit.Communication.Serializers;
    using OutWit.Communication.Server.Encryption;
    
    var server = WitServerBuilder.Build(options =>
    {
        options.WithService(new ExampleService());                      // register the service implementation
        options.WithWebSocket("http://localhost:5000", maxNumberOfClients: 10); // use WebSocket transport on port 5000, allow up to 10 clients
        options.WithJson();                                             // use JSON serialization (default)
        options.WithEncryption();                                       // enable encryption (AES/RSA)
        // You could also set an access token for authorization: options.WithAccessToken("SecureToken");
    });
    
    server.StartWaitingForConnection();
    Console.WriteLine("Server is listening for clients...");
    ```
    
    This starts a server listening on `http://localhost:5000` for WebSocket connections. The server will accept clients that connect to this URL (with the proper protocol and token if set). It uses JSON to serialize messages and secures communication with encryption.
    
4.  **Connect a WitRPC Client**. In the client application, use **WitClientBuilder** to configure a client with a matching transport and settings, then connect to the server:
    
    ```csharp
    using OutWit.Communication.Client;
    using OutWit.Communication.Client.WebSocket;
    using OutWit.Communication.Serializers;
    using OutWit.Communication.Client.Encryption;
    
    var client = WitClientBuilder.Build(options =>
    {
        options.WithWebSocket("ws://localhost:5000"); // connect via WebSocket to the server's URL
        options.WithJson();                           // use JSON serialization (must match server)
        options.WithEncryption();                     // enable encryption (must match server)
        // If using access tokens: options.WithAccessToken("SecureToken");
    });
    
    bool connected = await client.ConnectAsync(TimeSpan.FromSeconds(5));
    if (!connected)
    {
        Console.WriteLine("Failed to connect to the server.");
        return;
    }
    Console.WriteLine("Client connected.");
    
    // Get a proxy to the remote service
    IExampleService serviceProxy = client.GetService<IExampleService>();
    ```
    
    The `GetService<T>()` call returns an implementation of `IExampleService` that acts as a local proxy. Any calls you make on `serviceProxy` will be sent to the server's `ExampleService`. Similarly, any events raised by the server will trigger the corresponding events on this proxy object.
    
5.  **Use the Service Proxy**. Now you can call methods and subscribe to events on `serviceProxy` as if it were a local object:
    
    ```csharp
    // Subscribe to server events (these will be triggered by the server's ExampleService)
    serviceProxy.ProcessingStarted += () => Console.WriteLine("Processing started on server.");
    serviceProxy.ProgressChanged += percent => Console.WriteLine($"Progress: {percent}%");
    serviceProxy.ProcessingCompleted += status => Console.WriteLine($"Processing completed with status: {status}");
    
    // Call a method on the server via the proxy
    bool started = serviceProxy.StartProcessing();
    if (started)
    {
        Console.WriteLine("Processing initiated successfully.");
    }
    ```
    
    When `StartProcessing()` is called on the proxy, the call is sent to the server's `ExampleService.StartProcessing` method. As that method executes and calls `ProcessingStarted()`, the event is relayed back to the client and the corresponding handler runs, printing "Processing started on server." This demonstrates full-duplex communication: the client can call server methods, and the server can asynchronously notify the client via events.
    
This example used WebSockets, but WitRPC lets you choose any supported transport by swapping out the `.WithWebSocket` line with another option (for instance, `.WithNamedPipe("PipeName")` for named pipes, `.WithMemoryMappedFile("MapName")` for memory-mapped files, `.WithTcp(Port)` for TCP, or `.WithRest("http://...")` for REST). The rest of the code remains the same. Likewise, you can choose a different serializer (e.g., `WithMessagePack()` for binary serialization) or turn off encryption if not needed. The flexibility of the core library allows you to tailor communication to your needs while keeping the programming model consistent.

### Further Documentation

For more information on the WitRPC framework, including advanced topics and additional examples, visit the official documentation site at [witrpc.io](https://witrpc.io/).
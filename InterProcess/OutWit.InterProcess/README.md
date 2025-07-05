
# OutWit.InterProcess

**Inter-process communication base logic for the WitRPC framework.** This package provides the foundation for launching and communicating with external processes within the WitRPC ecosystem. It enables .NET applications to call services in separate processes as if they were local, using WitRPC’s dynamic proxies and transports. OutWit.InterProcess is ideal for scenarios where you need to run parts of your application in isolation or in a different environment (e.g. a 32-bit legacy component from a 64-bit host). It works seamlessly with the rest of WitRPC, allowing event-driven, secure communication between processes just like standard in-process or network calls.

## Overview

**OutWit.InterProcess** extends the WitRPC framework’s capabilities to **inter-process communication** on a single machine. WitRPC is a modern .NET client-server communication framework focused on simplicity and robust real-time RPC. With OutWit.InterProcess, you can leverage WitRPC to communicate across process boundaries. The same **dynamic proxy mechanism** of WitRPC is used here – the client in one process gets a proxy object implementing your interface, and the server in another process hosts the actual service object. Method calls, property accesses, and even events all traverse the process boundary seamlessly, so your code looks like regular method calls even though work is happening in another process.

This package serves as the **foundation** for out-of-process calls:

-   **Core Launch/Connect Logic:** It handles the low-level details of starting processes and establishing communication channels between them.
    
-   **Transport Agnostic:** Communication can occur over any supported WitRPC transport (e.g. Named Pipes or Memory-Mapped Files) optimized for inter-process use. By default, fast IPC transports are used for efficiency on the local machine.
    
-   **Transparent Integration:** Once connected, the remote service behaves like a local object. You can subscribe to events or call methods without worrying about serialization or message passing – WitRPC handles it under the hood.
    
-   **Cross-Architecture Support:** Because communication is transport-based (not via in-process calls), you can connect a 64-bit process to a 32-bit process reliably. This is crucial for hosting legacy 32-bit libraries in a separate helper process while the main app remains 64-bit.
    
In summary, OutWit.InterProcess is the building block that makes out-of-process plugins or services possible in the WitRPC ecosystem, providing a robust alternative to technologies like COM out-of-proc servers or custom socket solutions, with far less boilerplate.

## Installation

You can install **OutWit.InterProcess** via NuGet:

```shell
Install-Package OutWit.InterProcess
```

This package targets **.NET 6.0+** and will pull in the required WitRPC core libraries. In many cases, you won’t install this package directly – instead you will use the higher-level **Host** or **Agent** packages (which depend on OutWit.InterProcess) in your host and agent projects respectively. However, if you need to build custom inter-process logic, you can reference this base package.

## Basic Usage

Typically, you will use OutWit.InterProcess through the **Host** and **Agent** components (see their documentation below). For advanced scenarios, if you are using OutWit.InterProcess directly, you would perform these general steps:

1.  **Prepare the Agent Process:** Develop a separate application (or module) that includes your service implementation and uses **OutWit.InterProcess.Agent** to await connections. For example, the agent might call a run method to host a service (see *OutWit.InterProcess.Agent* section for details).
    
2.  **Launch from Host:** In the main application, use **OutWit.InterProcess.Host** to spawn the external process and connect to it. The Host APIs will start the process (e.g. launching the agent executable) and establish a WitRPC connection over an IPC transport.
    
3.  **Obtain Service Proxy:** After launching, the host gets a proxy for the remote service interface. You can then call methods on this proxy just like a normal object. WitRPC handles routing those calls to the external process.
    

**Example:** Suppose you have an interface `IExampleService` that you want to run in another process. You would create an agent program to implement and host this service, and then launch it from your main app:

-   **Agent side (external process)** – register and start the service:
    
    ```csharp
    // Agent process code
    using OutWit.InterProcess.Agent;
    using OutWit.Communication.Server;  // WitRPC server base
    
    class Program
    {
        static void Main(string[] args)
        {
            // Start the agent server with an instance of ExampleService
            WitProcessAgent.Run(() => new ExampleService());
            // The agent will now host IExampleService and wait for connections from the host.
        }
    }
    ```
    
-   **Host side (main process)** – launch agent and get the service proxy:
    
    ```csharp
    // Host process code
    using OutWit.InterProcess.Host;
    using OutWit.Communication.Client;  // WitRPC client base
    
    // Launch the external process and connect to IExampleService
    IExampleService service = WitProcessHost.Launch<IExampleService>("ExampleAgent.exe");
    
    // Now 'service' is a live proxy to the remote ExampleService.
    bool started = service.StartProcessing();
    Console.WriteLine($"Processing started: {started}");
    ```
    
In this pseudo-code, `WitProcessAgent.Run(...)` initializes the agent’s WitRPC server with your service, and `WitProcessHost.Launch<IFace>(...)` on the host launches the agent executable and returns a proxy implementing the interface `IFace` (here `IExampleService`). The actual API calls in the library may differ, but the concept remains: one call to set up the agent, one call to launch from the host, and then use the interface directly.
    
## Use Cases

OutWit.InterProcess is useful whenever you need to isolate or separate parts of your application into different processes while still having them communicate seamlessly:

-   **Running 32-bit legacy components:** If your main app is 64-bit but you depend on a 32-bit library or COM object, you can run that component in a small 32-bit agent process. OutWit.InterProcess will carry out all calls via IPC (e.g. through named pipes), so the architecture mismatch is no longer a problem.
    
-   **Isolating unreliable or resource-intensive modules:** For components that might crash or leak memory, keeping them in a separate process can protect the stability of the main application. For example, a plug-in executing untrusted scripts could be run in an external sandbox process. If it crashes, the host remains unaffected and can optionally restart the agent.
    
-   **Parallel or multi-process workloads:** You can distribute work across multiple processes on the same machine. Each process can host a service (with its own CPU/memory resources) and the main process can coordinate them. WitRPC’s event-driven calls make it straightforward to manage results and progress updates from each process.
    
-   **Security and permissions:** In scenarios where a certain operation needs elevated permissions or a different user context, you could launch an agent process with those credentials. The host communicates through WitRPC, and you avoid running the entire application with higher privileges.
    
In all these cases, OutWit.InterProcess lets you maintain a **clean separation**: the host and agent don’t share memory – they interact only through well-defined service interfaces, making the system more modular and robust.

## Integration with WitRPC Ecosystem

OutWit.InterProcess is a first-class part of the WitRPC framework. It builds on the same **OutWit.Communication** core libraries for serialization, networking, and security:

-   You can use any **transport** supported by WitRPC for the host-agent connection. The library is typically configured to use fast local transports like memory-mapped files or named pipes by default, but the programming model doesn’t change based on transport. Developers simply call `Launch` or `Run` methods and the underlying transport is abstracted away.
    
-   Full support for **encryption and authorization:** If your WitRPC setup uses end-to-end encryption or token-based auth, those can be enabled in inter-process communications as well. For example, you could protect the channel between host and agent with AES/RSA encryption, just as you would for a network connection, to ensure even local IPC traffic is secure.
    
-   **Consistent serialization:** The default JSON serialization (or MessagePack, if configured) is used for data exchange, so complex objects, callbacks, and exceptions all serialize across processes the same way they do across network endpoints. This means your data contracts and DTOs require no special treatment for IPC.
    
-   **Common API patterns:** OutWit.InterProcess reuses the WitRPC builder patterns and interfaces. Developers already familiar with `WitClientBuilder` and `WitServerBuilder` will find the inter-process host/agent setup quite intuitive. The learning curve is minimal – you’re still working with service interfaces and lambda-based configuration of options.
    
By using OutWit.InterProcess alongside the rest of WitRPC, you ensure that *all* parts of your distributed system (whether in other processes or on other machines) follow the same conventions. You can, for instance, have a mix where some services are loaded in-proc, some in local helper processes, and others on remote servers – all invoked in a uniform way.

## Further Documentation

For more details on the WitRPC framework and advanced usage of inter-process features, visit the official WitRPC documentation site at  [witrpc.io](https://witrpc.io/). There you will find guides, additional examples, and reference documentation covering topics such as custom transports, security, and performance tuning within the WitRPC/OutWit ecosystem
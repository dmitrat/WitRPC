
# OutWit.InterProcess.Agent

**Agent-side library for running WitRPC services in an isolated process.** This package is used inside the **external process (agent)** to host a WitRPC server and register services that the host process can call. OutWit.InterProcess.Agent works in tandem with the Host package to set up communication channels automatically, so you can focus on implementing your service. It’s essentially the “server-side” counterpart in the inter-process model: you include this in the child process that performs the work, such as a legacy component or a plugin that runs out-of-process.

## Overview

**OutWit.InterProcess.Agent** is part of the WitRPC inter-process communication suite and is meant to be **included in the application that runs as the agent** (the separate process). Its primary role is to start up a WitRPC service inside the agent process and connect it to the host. The library abstracts away the complexity of listening for a connection from the host and synchronizing the setup.

Key characteristics of the Agent package:

-   **WitRPC Server Initialization:** Under the hood, this package uses the WitRPC server functionality (from OutWit.Communication.Server) to create a server in the agent process that will serve the host’s requests. It takes care of binding the server to the correct transport/endpoint that the host will use to connect, so you typically don’t have to manually specify ports or pipe names.
    
-   **Service Registration:** You provide the service implementation (one or more objects implementing your service interfaces) to the agent’s startup method. The agent announces these services to the host. From that point, the host can obtain proxies and invoke the services remotely. The agent can host multiple services if needed, similar to how a single server can expose many interfaces.
    
-   **Automatic Handshake with Host:** The Agent package and the Host package have a protocol to handshake. For example, when the host launches the agent process, it might pass connection info via command-line arguments or environment variables. OutWit.InterProcess.Agent reads those and immediately starts the server accordingly. This means as a developer you often don’t need to write any networking setup code in the agent – just call the run method and your service is live.
    
-   **Minimal Footprint:** The agent process can be a lightweight console application or Windows service that does one job. With this library, that app only needs a few lines of boilerplate to start the WitRPC service and then it can focus on the actual logic (e.g., calling the legacy API or performing an isolated computation).
    

By using OutWit.InterProcess.Agent, you ensure your out-of-process service adheres to the WitRPC protocol and can be easily consumed by any WitRPC client (in this case, the host). This package **fits into the WitRPC ecosystem** as the counterpart to the usual server library, but specialized for scenarios where the server lives in a separate process launched by a host.

## Installation

Install **OutWit.InterProcess.Agent** in the project that will be compiled as your external agent process:

```shell
Install-Package OutWit.InterProcess.Agent
```

Requirements: .NET 6.0 (Windows) or higher, and typically you’ll also reference any WitRPC transport packages needed for your scenario (though in many cases, the default transport is set up automatically by the Host <-> Agent handshake). The Agent package will bring in **OutWit.InterProcess** (base logic) and **OutWit.Communication.Server** (WitRPC server core) as dependencies.

> **Tip:** Your agent application could be a simple console app that references this package along with your interfaces and implementations. Ensure that the interface definitions used by the agent and host are the same (usually shared via a common library) so that the WitRPC proxy and server have identical contracts.

## Usage: Hosting a Service in an Agent Process

Using OutWit.InterProcess.Agent is straightforward. In your agent program’s entry point, you will initialize the WitRPC server and register your service instance(s). The library likely provides a helper to do this in one call. While the exact API can vary, a typical pattern is:

1.  **Implement the Service:** Define the interface (if not already defined) and implement it in a class. For example, `IExampleService` and `ExampleService` as in the earlier scenario.
    
2.  **Start the Agent:** In the `Main` method of the agent application, call the provided **agent startup** method to register your services and begin listening for the host’s connection. This could be a method like `WitProcessAgent.Run(...)` or similar. You pass in the service instance or a factory lambda.
    
3.  **Keep Running:** Once started, the agent should generally keep running to serve incoming calls from the host. If it’s a console app, you might just wait on the WitRPC server to shut down or block the main thread (the Agent library might handle this for you internally).
    

**Example (Agent Program):**

```csharp
using OutWit.InterProcess.Agent;
using MySharedInterfaces;  // contains IExampleService

class Program
{
    static void Main(string[] args)
    {
        // Launch the WitRPC server in this process and register the ExampleService.
        WitProcessAgent.Run(() => new ExampleService());

        // The agent is now running. It will remain active, listening for the host 
        // to connect and invoke IExampleService methods. Typically no further code 
        // is needed here unless you want to log or handle shutdown signals.
    }
}
```

In this example, `WitProcessAgent.Run` (hypothetically) does the heavy lifting:

-   It reads any connection parameters (possibly passed via `args` or environment variables by the host).
    
-   It builds a WitRPC server with the chosen transport (e.g., a unique named pipe or shared memory file) matching the host.
    
-   It instantiates your `ExampleService` (via the lambda) and makes it available to the host under the `IExampleService` interface.
    
-   It likely blocks the main thread or runs an event loop to keep the process alive while waiting for calls. You don’t need to loop or poll manually – once `Run` is called, your service is serving.
    

At this point, whenever the host process connects (using OutWit.InterProcess.Host), it will handshake with this agent. The host’s calls into `IExampleService` will be routed to this `ExampleService` instance inside the agent process. Likewise, if `ExampleService` raises an event, the Agent library will forward that to any subscribed proxies in the host process.

## Use Cases

OutWit.InterProcess.Agent is useful in any scenario where you want a .NET component to run out-of-process and communicate with a primary application. Some specific cases:

-   **Legacy Component Wrapper:** Suppose you have an old 32-bit COM object or library that cannot be loaded into a 64-bit process. You can write a small agent that calls this component (e.g., via P/Invoke or COM Interop) and expose a modern interface (`ILegacyWrapper`) via WitRPC. The host launches this agent when needed and interacts through the interface, insulating the main app from the legacy code’s quirks.
    
-   **Crash Isolation:** If you expect a certain module to be unstable (perhaps it uses unsafe code or third-party libraries that occasionally crash), running it as an agent means a crash won’t take down your main application. For example, a graphics processing library that isn’t fully reliable could be used in an agent. The host detects if the agent stops responding and can restart it if necessary.
    
-   **Security Sandbox:** Run sensitive operations with lower privileges. For instance, if the agent process is launched with a restricted Windows user account or with a specific AppContainer, it can perform only the allowed tasks. The host (with higher privileges) remains protected. The Agent package facilitates communication back to the host so results can be returned despite the isolation.
    
-   **Multiple Service Instances:** You could have several different agent executables for different tasks (or even spawn multiple instances of the same agent for load balancing). Each agent uses OutWit.InterProcess.Agent to register its service, and the host keeps track of connections to each. This is useful for parallel processing or modular architectures where each feature/plugin runs independently.
    

In all these cases, the Agent library ensures that writing the agent process code is nearly as simple as writing an in-process service. You focus on your service logic, and the networking/IPC is handled by WitRPC.

## Integration Details

When using OutWit.InterProcess.Agent, you’re leveraging the standard WitRPC server capabilities in an out-of-process setting:

-   The agent’s **WitRPC server** supports all the usual features: encryption, compression, custom serializers, etc. If the host expects an encrypted connection (e.g., using a pre-shared token or certificate), the agent can be configured to use the same `WithEncryption()` or `WithAccessToken(...)` options so that security is enforced on both ends.
    
-   **Supported Transports:** The choice of transport for host-agent communication is typically made by the host (which might default to a local IPC transport). The agent will use whatever transport info is provided. For example, if using Named Pipes, the host might generate a unique pipe name and the agent will open a server on that name. If using a Memory-Mapped File, the host provides the map name and size, and the agent attaches to it. All this is negotiated through the Agent/Host libraries automatically.
    
-   **WitRPC Protocol Compliance:** Because the agent uses the official WitRPC server internally, it speaks the same protocol that any WitRPC client understands. In theory, this means you could even connect to an OutWit.InterProcess.Agent process using a regular OutWit.Communication.Client (if you knew the connection details), not just the special Host launcher. This underscores that the agent is a normal WitRPC server just started in a different manner.
    
-   **Lifecycle Management:** The Agent library might provide hooks or events for things like when a host connects or disconnects. This can be useful for logging or for the agent to decide when to shut down (for instance, the agent might exit if the host disconnects and no other work is to be done). You can integrate this with your application’s logging or monitoring. Similarly, the agent could be configured to accept only a single host connection for security (since in typical scenarios, the host launched it so no other client should attach).
    

**Note:** OutWit.InterProcess.Agent is generally not used standalone; it expects a coordinating host. If you run the agent application by itself, it will likely start a server and wait indefinitely (with no effect until a host connects). Ensure that you launch the agent via the intended Host APIs or at least provide the necessary startup info so it knows how to function.

## Further Documentation

To learn more about WitRPC and advanced agent configurations, visit [witrpc.io](https://witrpc.io/). The documentation contains guides on topics like custom security setups, troubleshooting connections, and optimizing performance for inter-process calls. Understanding the basics of WitRPC (events, serialization, etc.) will also help in making the most of the Host/Agent architecture.
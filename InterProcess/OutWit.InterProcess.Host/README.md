
# OutWit.InterProcess.Host

**Host-side library for launching and communicating with out-of-process WitRPC services.** This package is used in the **main (host) application** to spawn external processes and connect to the services they host. OutWit.InterProcess.Host provides the client-side coordination for inter-process calls: you launch an agent process (which uses the Agent library) and immediately get a proxy to use its services. It simplifies running components in separate processes while treating them almost like local objects in your code.

## Overview

**OutWit.InterProcess.Host** is the counterpart to the Agent package and is typically referenced by the primary application that needs to utilize out-of-process services. Its responsibilities and features include:

-   **Process Launching:** It can start the external process for you, given an executable path or configuration. This might involve creating a new Process with the correct parameters (for example, pointing it to use a particular communication channel). The Host library abstracts the details of using `System.Diagnostics.Process` or other OS-specific calls – you just tell it what to run.
    
-   **Handshake & Connection:** Once the agent process starts, the Host library establishes a WitRPC connection to it. This includes negotiating a transport (often a local IPC mechanism like a named pipe) and making sure the agent’s WitRPC server is ready. The Host will wait for the agent to signal that it’s up and running, thereby handling timing issues of startup.
    
-   **Dynamic Proxy Retrieval:** After a successful connection, OutWit.InterProcess.Host gives you a **proxy instance** implementing the service interface that the agent provides. This uses WitRPC’s dynamic proxy ability – the same feature used for remote network calls. From the developer’s perspective, calling `host.Launch<IService>` (or similar) returns an `IService` object that you can immediately use to call methods. Under the hood, those calls are being transmitted to the other process.
    
-   **Process Management:** The Host library may also manage the lifetime of the agent process. For instance, it could offer methods to gracefully shut down the agent or restart it. It might also detect if the agent process exits unexpectedly (so your host app can react). In some cases, the host can be configured to automatically kill the agent process when the host itself exits, to avoid orphan processes.
    
By using OutWit.InterProcess.Host, you can integrate external processes into your application architecture without having to write manual IPC code or deal with raw process control beyond the basics. This library ensures the **out-of-process services are as easy to consume as in-process ones**, fulfilling WitRPC’s goal of natural, object-oriented communication across boundaries.

## Installation

Install **OutWit.InterProcess.Host** in your main application project (the one that will launch and talk to the external service processes):

```shell
Install-Package OutWit.InterProcess.Host
```

This will also install **OutWit.InterProcess** (base) and **OutWit.Communication.Client** (the WitRPC client core) as dependencies. The Host package targets .NET 6.0 on Windows. Ensure that your host and agent are built against compatible versions of WitRPC so their communications align.

Typically, you'll also have a reference to the interface definitions that the host and agent share (so the host knows about `IExampleService`, for example). These interfaces can be in a common assembly referenced by both projects.

## Usage: Launching an Agent and Getting a Service Proxy

Using OutWit.InterProcess.Host usually boils down to one main action: **launching an agent process and retrieving the service proxy**. The library likely provides a simple API to do this. The general pattern is:

1.  **Prepare the agent executable:** Decide which process you are going to launch. This could be an external `.exe` file (perhaps the output of your agent project). Make sure the agent is accessible (e.g., include it in your deployment or have a known path).
    
2.  **Call the Host launch method:** Use the Host library’s API to start the agent. You will specify the service interface you expect the agent to provide, and the path to the agent program. There may be additional options (for example, working directory, command-line args, etc., if needed).
    
3.  **Receive the service interface proxy:** The launch method will return an implementation of the interface that actually forwards calls to the agent. You can immediately call methods on it or subscribe to its events. From here on, usage of the service is identical to a local object – the Host library and WitRPC handle the communication.
    

**Example (Host side):**

```csharp
using OutWit.InterProcess.Host;
using MySharedInterfaces;  // e.g., contains IExampleService

// Launch the agent process (ExampleAgent.exe) and get a proxy to IExampleService
IExampleService service = WitProcessHost.Launch<IExampleService>("ExampleAgent.exe");

// Use the service proxy as if it's local
string result = service.ProcessData("input-data");
Console.WriteLine($"Agent returned: {result}");

// You can also subscribe to events published by the remote service
service.ProgressChanged += percent => Console.WriteLine($"Progress: {percent}%");
```

In this hypothetical code, `WitProcessHost.Launch<T>` starts the process `ExampleAgent.exe`, negotiates a connection, and returns an object that implements `IExampleService`. When you call `service.ProcessData`, the call is sent over to the agent process, executed by the real `ExampleService`, and the result is sent back. If `IExampleService` has events (like `ProgressChanged` in the snippet above), the agent can raise those and the Host library will forward them to your event handler in the host process.

A few notes on this process:

-   The first call to `Launch` might take a short moment, as it’s starting a process and setting up IPC. Once connected, calls are typically fast (especially using in-memory or pipe transport).
    
-   If the agent fails to start or connect (for example, if the file path is wrong or the agent crashes on startup), the Host library should throw an exception or return an error. Be prepared to handle such cases (e.g., log the error or notify the user).
    
-   The Host can launch multiple agents by calling `Launch` multiple times (perhaps with different executables or the same executable with different arguments). Each call would yield a different service proxy connected to a different process.
    

## Scenarios and Use Cases

OutWit.InterProcess.Host empowers several architectural choices in your application:

-   **Modular Plugins:** Suppose your app supports plugins that might be unreliable or need to be sandboxed. You can deploy each plugin as a separate executable (implementing a known plugin interface). The host uses the Host library to load each plugin process on demand and get a proxy to call its functions. If a plugin misbehaves, you can terminate that process without affecting the rest of the app.
    
-   **Large Computations in Isolation:** If you have a long-running computation or background task that is CPU-intensive, you might run it in another process to keep the UI responsive. The host launches a “worker” process (agent) to do the computation. Progress and results come back via WitRPC events and calls, keeping the main UI thread free. This also takes advantage of multiple CPU cores (each process can run on a different core).
    
-   **Inter-process GUI/Service separation:** In some designs, you have a Windows GUI app and a Windows service or console doing backend work. Using OutWit.InterProcess.Host, the GUI (host) could start the backend (agent) if it’s not already running and then communicate via WitRPC. This is an alternative to using something like WCF with an external service – instead, you directly spawn and connect, which can simplify deployment (no separate service install required, if you go with on-demand launching).
    
-   **Switching Execution Context:** If your application sometimes needs to perform an operation in a different bitness or with certain libraries loaded, you can swap it to an external process. For example, an application might by default do everything in-proc, but if a certain feature is activated that requires a 32-bit environment, the Host can launch a 32-bit agent for that feature alone. The rest of the app remains 64-bit. This targeted approach can reduce complexity compared to running the entire app in 32-bit just for one component.
    

In all these scenarios, OutWit.InterProcess.Host’s value is in making the *communication and management of those processes trivial*. You don’t have to manually set up named pipe servers or socket listeners, or worry about serialization – you call `Launch` and then use a strongly-typed interface.

## Integration and Advanced Features

OutWit.InterProcess.Host is built on the WitRPC client framework, meaning it inherits all client-side capabilities:

-   It utilizes the **OutWit.Communication.Client** under the hood. This means you can configure things like serialization format or timeouts if needed. For example, if you want to use MessagePack instead of JSON for transferring large data to the agent (to improve speed), you could configure that in the launch options if such an API exists. Similarly, you might set a timeout for certain calls – if the agent doesn’t respond in, say, 10 seconds, the proxy call could throw a timeout exception.
    
-   **Security considerations:** Because the host and agent run on the same machine, one might assume security is less of a concern. However, if your agent deals with sensitive operations, you can still enforce encryption or require an authentication token for the connection. WitRPC supports token-based auth and encryption as options. The Host library can pass a token to the agent on startup, and the agent will validate it. This ensures that a malicious process can’t impersonate your agent and hijack the connection.
    
-   **Resource cleanup:** The Host library likely provides a way to dispose of or stop the connection and the process. For instance, when your application is closing, you might want to shut down all agent processes. This could be as simple as disposing the proxy or calling a special method to signal the agent to exit. WitRPC proxies are disposable objects, so disposing one might also terminate the underlying connection. Check the documentation for the recommended cleanup pattern (e.g., the host might call something like `host.Close()` or the agent might exit on its own when the host disconnects).
    
-   **Cross-platform note:** Currently, OutWit.InterProcess.Host is targeted at Windows. It uses transports like named pipes or memory files which are also available on Linux/macOS, but the process launching and some integration aspects might be Windows-specific. Future versions may expand cross-platform support. If you are targeting other OSes, ensure to test the behavior or consult the docs for any limitations.
    
## Further Resources

For more information on using the Host and Agent pattern with WitRPC, refer to the official documentation on [witrpc.io](https://witrpc.io/). You'll find examples, troubleshooting tips (for example, if an agent fails to launch or connect), and guidance on performance tuning (such as when to use memory-mapped files vs. pipes for IPC). The WitRPC community and documentation can also provide patterns for scenarios like gracefully updating an agent or sharing data between host and agent beyond RPC calls.

By understanding both OutWit.InterProcess.Host and Agent, you can architect flexible applications that take full advantage of process isolation while still communicating effectively and safely.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.InterProcess.Host in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
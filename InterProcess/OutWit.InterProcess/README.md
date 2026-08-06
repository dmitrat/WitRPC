# OutWit.InterProcess

Base package of the WitRPC inter-process suite. It defines the model shared by the host and agent sides of the Host/Agent pattern: a main application (the host) launches child processes (agents), each agent hosts a WitRPC service, and the host talks to it through an ordinary typed proxy.

You will normally install one of the two role packages, which bring this one in as a dependency:

- **[OutWit.InterProcess.Host](https://www.nuget.org/packages/OutWit.InterProcess.Host/)** — for the main application: launches agents and returns service proxies.
- **[OutWit.InterProcess.Agent](https://www.nuget.org/packages/OutWit.InterProcess.Agent/)** — for the agent executable: entry-point base with parent monitoring and startup timeout.

Reference this package directly only when building custom inter-process logic on top of the shared model.

## Install

```bash
dotnet add package OutWit.InterProcess
```

## What it contains

**`AgentStartupParameters`** — the launch contract between host and agent, passed to the agent process as serialized command-line arguments:

| Property | Meaning |
|---|---|
| `Address` | Transport address the agent's WitRPC server must listen on |
| `ParentProcessId` | Host process id, monitored by the agent |
| `Timeout` | How long the agent waits for a connection before shutting itself down |
| `ShutdownOnParentProcessExited` | Whether the agent terminates when the host process disappears |

An agent reads the parameters with `args.DeserializeCommandLine<AgentStartupParameters>()` (from `OutWit.Common.CommandLine`).

**`IAgent<TService>`** — the host-side handle for one running agent:

```csharp
public interface IAgent<out TService> : IDisposable where TService : class
{
    event AgentEventHandler Initialized;
    event AgentEventHandler Disposed;

    Task<bool> Initialize(TimeSpan timeout);
    Task Stop();          // graceful disconnect
    void Shutdown();      // terminate the agent process

    TService? Service { get; }   // the typed WitRPC proxy
    bool IsInitialized { get; }
}
```

**`IAgentManager<TService>`** — the contract for managing a set of agents (`CreateClient`, `GetAgent`, `ShutdownAgent`), implemented by `HostManager<TService>` in the Host package.

## How the pieces fit

1. The host prepares `AgentStartupParameters` and starts the agent executable with them.
2. The agent parses the parameters, starts a WitRPC server on `Address`, and begins monitoring `ParentProcessId`.
3. The host connects a WitRPC client to the same address and obtains the typed proxy.
4. From there the connection is ordinary WitRPC: methods, return values, and events across the process boundary.

If the host exits, the agent notices and terminates itself, so no orphaned processes remain; an agent that receives no connection within `Timeout` also shuts down on its own.

A complete working host/agent pair lives in the repository: [Examples/InterProcess](https://github.com/dmitrat/WitRPC/tree/main/Examples/InterProcess).

## Links

- Documentation: https://witrpc.io/
- Repository: https://github.com/dmitrat/WitRPC
- License: Apache 2.0

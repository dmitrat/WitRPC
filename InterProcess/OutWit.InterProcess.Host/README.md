# OutWit.InterProcess.Host

Host-side package of the WitRPC inter-process suite. Install it in the main application to launch agent processes and communicate with the services they host through typed WitRPC proxies. The package covers the full cycle: starting the agent executable, connecting to it, handing you the proxy, and shutting the agent down.

## Install

```bash
dotnet add package OutWit.InterProcess.Host
```

Brings in `OutWit.InterProcess` (shared model) and the WitRPC client core. The host and agent projects should share a contracts assembly with the service interfaces, as in any WitRPC setup.

## Getting started

`HostManager<TService>` manages agents for one service type. Construct it with client options (transport, serializer, security), the path to the agent executable, and a process startup timeout; each `CreateClient` call launches one agent process and returns it connected and initialized:

```csharp
using OutWit.Communication.Client;
using OutWit.InterProcess.Host;

var options = new WitClientBuilderOptions();
options.WithNamedPipe($"MyApp_{Guid.NewGuid():N}");   // unique per-agent address
options.WithJson();

var manager = new HostManager<IProcessingService>(
    options,
    "ProcessingAgent.exe",
    processTimeout: TimeSpan.FromSeconds(30));

IAgent<IProcessingService> agent = await manager.CreateClient(TimeSpan.FromSeconds(10));

// The typed proxy: calls run in the agent process, events come back
IProcessingService service = agent.Service!;
service.ProgressChanged += p => Console.WriteLine($"Progress: {p}%");
var result = await service.ProcessAsync(inputPath);

// Done with this worker
agent.Shutdown();
```

The transport address configured in the options is handed to the agent at launch, so both sides meet on the same channel without any configuration inside the agent.

## The agent handle

`CreateClient` returns `IAgent<TService>`:

- `Service` — the WitRPC proxy for the contract; methods, return values, and events all work across the process boundary.
- `IsInitialized` — whether the agent is connected and ready.
- `Initialized` / `Disposed` — lifecycle events. Subscribe to `Disposed` to react to agent crashes: relaunch, degrade, or report, while the host keeps running.
- `Stop()` — graceful disconnect; `Shutdown()` — terminate the agent process.

The manager tracks its agents (`GetAgent(id)`, `ShutdownAgent(id)`) and disposes them with itself.

## Lower-level launching

`HostUtils.RunAgent` starts an agent process directly, without the manager, when you need custom control over the lifecycle:

```csharp
Process? process = HostUtils.RunAgent(
    pathToAgent: "ProcessingAgent.exe",
    address: pipeName,
    timeout: TimeSpan.FromSeconds(30),
    shutdownOnParentProcessExited: true);
```

An overload accepts a custom serializable parameters object for agents with their own startup contract.

## Choosing a transport

Named pipes are the default choice for command-and-control traffic; memory-mapped files win for large data volumes between the host and a single agent. Both stay on the machine. Any WitRPC client transport package works: install the one matching the agent's server side.

## Safety nets

Agents launched with `ShutdownOnParentProcessExited` terminate themselves when the host process disappears, including a host crash, so no orphaned processes accumulate. An agent that receives no connection within its startup timeout also exits on its own.

A complete working example (a WPF host managing a pool of agents) lives in the repository: [Examples/InterProcess](https://github.com/dmitrat/WitRPC/tree/main/Examples/InterProcess).

## Links

- Documentation: https://witrpc.io/
- Repository: https://github.com/dmitrat/WitRPC
- Agent-side package: [OutWit.InterProcess.Agent](https://www.nuget.org/packages/OutWit.InterProcess.Agent/)
- License: Apache 2.0

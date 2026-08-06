# OutWit.InterProcess.Agent

Agent-side package of the WitRPC inter-process suite. Install it in the executable that runs as a child process (the agent) of a host application. The agent hosts a WitRPC service; this package supplies the application base that handles the lifecycle around it: parsing startup parameters, monitoring the parent process, and shutting down when abandoned.

## Install

```bash
dotnet add package OutWit.InterProcess.Agent
```

Brings in `OutWit.InterProcess` (shared model). Add the WitRPC server transport package matching the host's client side, and reference the shared contracts assembly.

## The agent pattern

An agent does three things: reads its startup parameters from the command line, starts a WitRPC server on the address the host assigned, and lets the lifecycle machinery watch the parent.

```csharp
using OutWit.Common.CommandLine;
using OutWit.Communication.Server;
using OutWit.InterProcess.Model;

[STAThread]
static void Main(string[] args)
{
    var parameters = args.DeserializeCommandLine<AgentStartupParameters>();

    var server = WitServerBuilder.Build(options =>
    {
        options.WithService(new ProcessingService());
        options.WithNamedPipe(parameters.Address);   // address supplied by the host
        options.WithJson();
    });
    server.StartWaitingForConnection();

    var app = new AgentApplication(parameters);
    app.Run();
}
```

The host passes `AgentStartupParameters` (address, parent process id, timeout, shutdown policy) as serialized command-line arguments; `DeserializeCommandLine` reconstructs them. The service implementation and server setup are ordinary WitRPC, with the transport address taken from the parameters instead of hard-coded.

## What AgentApplication handles

- **Parent monitoring.** When `ShutdownOnParentProcessExited` is set, the agent watches the host's process id and terminates itself when the host disappears, including a host crash. No orphaned processes.
- **Startup timeout.** An agent that receives no connection within `Timeout` shuts down on its own instead of lingering.
- **Timeout extension.** Call `ResetTimeout()` during legitimate long-running work so the watchdog does not mistake activity for abandonment.

## Windows and WPF

`AgentApplication` derives from `System.Windows.Application`, so an agent project using it must set `<UseWPF>true</UseWPF>` and targets Windows (`net6.0-windows` through `net10.0-windows`). This suits the common case of agents living alongside desktop hosts.

For a cross-platform or plain console agent, keep the same structure and supply your own entry point: parse `AgentStartupParameters` from the command line, start the server, monitor `ParentProcessId` yourself, and drive the startup timeout with `System.Threading.Timer`. The host side works identically with either variant.

## What the host sees

The host launches this executable through `HostManager<TService>` (from [OutWit.InterProcess.Host](https://www.nuget.org/packages/OutWit.InterProcess.Host/)), connects to the address it assigned, and receives a typed proxy for the service. Method calls execute in the agent; events the service raises arrive at the host's subscribers.

A complete working host/agent pair lives in the repository: [Examples/InterProcess](https://github.com/dmitrat/WitRPC/tree/main/Examples/InterProcess).

## Links

- Documentation: https://witrpc.io/
- Repository: https://github.com/dmitrat/WitRPC
- Host-side package: [OutWit.InterProcess.Host](https://www.nuget.org/packages/OutWit.InterProcess.Host/)
- License: Apache 2.0

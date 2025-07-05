
# OutWit.Communication.Client.Pipes

Named Pipes transport client for WitRPC, allowing efficient inter-process communication via named pipes on the local machine (with support for multiple clients).

### Overview

**OutWit.Communication.Client.Pipes** enables the use of **Named Pipes** as the transport for a WitRPC client. Named pipes are a reliable, high-throughput IPC mechanism provided by the operating system, suitable for communication between processes on the same machine (and even across machines in a Windows domain). This transport allows multiple clients to connect to a server using the same pipe name, with the OS handling the connections behind the scenes. It's useful for local communications where you might have several client processes interacting with a single server process (for example, multiple applications or services talking to a local service host).

Using named pipes, the server creates a pipe with a specific name, and clients connect to that name. The WitRPC framework handles framing and message transfer over the pipe, so you use it just like any other transport.

**Note:** Use this client together with **OutWit.Communication.Server.Pipes** on the server side. The pipe name must match exactly on both ends.

### Installation

```shell
Install-Package OutWit.Communication.Client.Pipes
```

### Usage

To use the named pipe transport, specify it when building your WitRPC client. For example:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.Pipes;
using OutWit.Communication.Serializers;

var client = WitClientBuilder.Build(options =>
{
    options.WithNamedPipe("MyAppPipe");  // connect to a named pipe "MyAppPipe"
    options.WithJson();
    options.WithEncryption();           // encryption is optional for local pipes
});
await client.ConnectAsync(TimeSpan.FromSeconds(5));

IMyService service = client.GetService<IMyService>();
```

On the server side, you would use the corresponding server package to listen on the same pipe name:

```csharp
// Server-side (OutWit.Communication.Server.Pipes) configuration example:
options.WithNamedPipe("MyAppPipe", maxNumberOfClients: 10);
```

The `maxNumberOfClients` parameter on the server defines how many simultaneous client connections are allowed.

After the client is connected, using `service` is the same as with any other transport. You can call methods and subscribe to events normally. The named pipe transport ensures that these calls and events are transmitted through the pipe efficiently.

**Remote and Cross-Platform Usage:** By default, calling `.WithNamedPipe("MyAppPipe")` on the client will try to connect to a pipe on the local machine (the server field defaults to `"."` for local). If you need to connect to a named pipe on a remote Windows machine, you can use the overload `options.WithNamedPipe("ServerMachineName", "MyAppPipe")` which will attempt to open `\\ServerMachineName\pipe\MyAppPipe`. Keep in mind that remote named pipe access may require proper network permissions and is generally limited to Windows environments. On Linux/macOS, .NET's named pipe implementation uses Unix domain sockets, which are local-only.

**Security:** Named pipes on Windows can have ACLs restricting which users or processes can connect. When you create a named pipe without specifying security, it usually is accessible to the same user account or local system. For added security, you can incorporate WitRPC's token authentication (so that even if a process connects to the pipe, it must present the correct token to use the service). Also, enabling `WithEncryption()` will encrypt messages over the pipe, which might be useful if you have concerns even on a local machine (though typically pipe communication is not easily intercepted without access to the machine).

### Further Documentation

For more about using named pipes in WitRPC and advanced pipe usage (including setting custom security or handling remote connections), see the official documentation on [witrpc.io](https://witrpc.io/).
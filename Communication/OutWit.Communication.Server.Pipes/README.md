
# OutWit.Communication.Server.Pipes

Named Pipes transport server for WitRPC, enabling a server to handle client connections over named pipes (efficient local IPC with support for multiple clients).

### Overview

**OutWit.Communication.Server.Pipes** allows a WitRPC server to listen for incoming connections via a **Named Pipe**. Named pipes are a powerful IPC mechanism on Windows (and supported on other OSes via .NET's implementation), suitable for communication on the same machine and even between machines in a Windows network. This transport supports multiple clients: the server can accept several pipe connections concurrently (up to a configured limit). Named pipes are useful for local services that need to talk to multiple client applications without using network sockets, as well as for secure intra-machine communication.

When you configure a named pipe server, it creates a pipe name. Clients connect to that name (locally as `\\.\pipe\Name` on Windows, or a similar path on Unix). Under the hood, the OS handles each client as a separate connection instance of the same named pipe. WitRPC will manage these connections and route RPC calls from each client to your service.

**Usage note:** Use this server in conjunction with **OutWit.Communication.Client.Pipes** on the client side. The pipe name must match.

### Installation

```shell
Install-Package OutWit.Communication.Server.Pipes
```

### Usage

To host a WitRPC service over named pipes, specify a pipe name and an optional client limit:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes;
using OutWit.Communication.Serializers;

var server = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
    options.WithNamedPipe("MyAppPipe", maxNumberOfClients: 5);
    options.WithJson();
    options.WithoutEncryption(); // local pipes may not need encryption, but you can enable it
});
server.StartWaitingForConnection();
Console.WriteLine("Named pipe server listening on MyAppPipe");
```

This configures the server to create a named pipe called `"MyAppPipe"` and allow up to 5 simultaneous client connections. The server will wait for clients to connect to `MyAppPipe`. On Windows, the full pipe path is `\\.\pipe\MyAppPipe`; on Linux/macOS, .NET will create a domain socket (usually in a temp path) to represent the pipe.

Clients (using OutWit.Communication.Client.Pipes) should connect with the same name:

```csharp
options.WithNamedPipe("MyAppPipe");
```

Optionally, clients can specify a machine name to connect to a remote machine's pipe, e.g., `.WithNamedPipe("ServerMachine", "MyAppPipe")`, if the server's machine is accessible and the pipe is permitted for remote access.

When multiple clients connect, the WitRPC server will handle each in parallel (each client gets its own thread or async task context). Your service calls are invoked concurrently as needed, so ensure your service implementation is thread-safe if multiple clients might call at the same time.

**Security:** By default, named pipes on Windows will be accessible only to the same user that created them (and administrators). You can further restrict or open access using pipe security (though that is outside the scope of WitRPC and done at OS level). For most cases, using the default security and running both client and server under the same account is sufficient. Additionally, you can require an access token for your WitRPC server (`options.WithAccessToken("...")`), so even if another process connects to the pipe, it cannot successfully invoke service calls without the token. Also consider using encryption (`WithEncryption()`) if you want to ensure that even on-machine data cannot be sniffed (though named pipe traffic stays within the OS and is not exposed on the network).

**Cross-machine usage:** If you attempt to use named pipes across machines (Windows only), make sure the server's pipe is created with the proper prefix and security for remote access, and that the client has the necessary permissions. This is an advanced scenario; in many cases if cross-machine communication is needed, using TCP or WebSocket is simpler.

### Further Documentation

Refer to the [WitRPC documentation](https://witrpc.io/) for more about using named pipes, including troubleshooting connection issues and performance considerations for IPC.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server.Pipes in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.

# OutWit.Communication.Client.MMF

Memory-mapped file transport client for WitRPC, enabling high-speed inter-process communication via a shared memory region (for on-machine RPC calls).

### Overview

**OutWit.Communication.Client.MMF** adds support for using **Memory-Mapped Files** as the communication transport on the client side of the WitRPC framework. This is ideal for scenarios where the client and server run on the same machine and need extremely fast communication (for example, a host process communicating with a sandboxed worker process). Memory-mapped file (MMF) transport uses a shared memory region to exchange data, which bypasses the network stack entirely and provides ultra-low latency IPC on a single host.

Using this transport, the client and server coordinate via a common memory-mapped file name. The server creates the memory-mapped file and listens, and the client opens the same file to read/write requests and responses. This approach can achieve very high throughput since reading and writing to memory is much faster than socket communication.

**Note:** This MMF client must be used in tandem with **OutWit.Communication.Server.MMF** on the server side. Both sides should use the same memory-mapped file name to communicate. The MMF transport is **one-to-one by design**: one client and one server process per channel name. Both sides must run WitRPC 3.0 — the 3.0 channel layout (lossless, two directional regions) is not compatible with 2.x.

### Installation

```shell
Install-Package OutWit.Communication.Client.MMF
```

This will also install the core OutWit.Communication.Client library if it's not already referenced. Make sure the server application references **OutWit.Communication.Server.MMF** to complete the setup.

### Usage

To use the memory-mapped file transport on a client, specify it when building the WitRPC client. For example:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.MMF;
using OutWit.Communication.Serializers;

var client = WitClientBuilder.Build(options =>
{
    options.WithMemoryMappedFile("MyApp_MMF");  // use a memory-mapped file named "MyApp_MMF"
    options.WithJson();                         // use JSON serialization (default)
    options.WithoutEncryption();                // encryption can be optional for same-machine comm
});
await client.ConnectAsync(TimeSpan.FromSeconds(5));

// Get the service proxy as usual
IMyService service = client.GetService<IMyService>();
```

> **Since 2.4.0:** the parameterless `GetService<T>()` generates its proxy at runtime (Castle.Core) and lives in the opt-in [OutWit.Communication.Client.DynamicProxy](https://www.nuget.org/packages/OutWit.Communication.Client.DynamicProxy/) package — install it alongside this transport. The NativeAOT/trimming-friendly alternative needs no extra package: annotate the interface with `[ProxyTarget("MyServiceProxy")]` (source generator `OutWit.Common.Proxy.Generator`) and call `client.GetService<IMyService>(interceptor => new MyServiceProxy(interceptor))`.

In this snippet, we call `options.WithMemoryMappedFile("MyApp_MMF")` to tell the WitRPC client to use a memory-mapped file transport with the given name. The server must be configured with the **same name** (and optionally a size, if using a non-default size). Once the client calls `ConnectAsync`, it will attempt to open the memory-mapped file that the server created and establish communication through it.

Using the `service` proxy obtained from the client is the same as with any other transport: you call methods and subscribe to events normally. The difference is entirely under the hood: requests and responses are being written to and read from shared memory.

**Performance Consideration:** Memory-mapped file transport can achieve extremely high throughput for large volumes of data because it avoids the network stack entirely. However, it's limited to local scenarios, and it is one-to-one: one server and one client per channel. If you require multiple concurrent clients, use multiple MMF channels (a different name for each client) or a transport like Named Pipes or TCP which natively supports multi-client.

**Security:** The channel's kernel objects live in the session-local `Local\` namespace, so they are not visible across sessions; within the session, any process with the same rights could attempt to attach by name. Choose a unique, hard-to-guess name, and if needed enable WitRPC's encryption (`WithEncryption()`) — authenticated AES-256-GCM in 3.0 — so another local process reading the memory sees only ciphertext and tampered frames are rejected.

### Further Documentation

For more details on using the memory-mapped file transport and tips on optimizing its performance, see the WitRPC documentation on [witrpc.io](https://witrpc.io/).

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Client.MMF in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
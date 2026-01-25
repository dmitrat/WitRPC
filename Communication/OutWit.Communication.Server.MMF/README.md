

# OutWit.Communication.Server.MMF

Memory-mapped file transport server for WitRPC, allowing a server to listen for client connections via a shared memory segment (for high-performance on-machine communication).

### Overview

**OutWit.Communication.Server.MMF** enables a WitRPC server to communicate with clients through a **Memory-Mapped File**. This transport is intended for on-machine scenarios where the server and client processes share the same physical machine and memory. The server creates or opens a memory-mapped file with a given name (and size) and waits for a client to connect to that shared memory region. The MMF transport offers extremely fast communication (memory-speed reads/writes) and minimal latency since it bypasses networking entirely.

Typical use cases include launching a background "worker" process for intensive computations and communicating with it via shared memory, or any scenario requiring very high throughput between two processes on one machine. Essentially, it's like creating a private high-speed bus between your processes.

**Important:** This server transport must be paired with **OutWit.Communication.Client.MMF** on the client side. Both sides need to use the same memory-mapped file name to establish the connection. Typically, an MMF transport is used for **one server and one client** (one-to-one communication). If you need to handle multiple separate clients, you might either use multiple distinct MMF channels or consider using the named pipes transport which natively supports multiple clients.

### Installation

```shell
Install-Package OutWit.Communication.Server.MMF
```

### Usage

To use the memory-mapped file transport on the server, configure it with a file name (and optionally a size in bytes for the memory region):

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.MMF;
using OutWit.Communication.Serializers;

var server = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
    // Create a memory-mapped file transport named "MySharedMap" with 1,000,000 bytes of memory:
    options.WithMemoryMappedFile("MySharedMap", size: 1000000);
    options.WithJson();
    options.WithEncryption();  // optional: enable encryption even for local memory communication
});
server.StartWaitingForConnection();
Console.WriteLine("Memory-mapped file server ready (MySharedMap).");
```

On the client side (OutWit.Communication.Client.MMF), you would call `options.WithMemoryMappedFile("MySharedMap")` with the same name to connect.

When the server starts, it allocates a memory-mapped file of the specified size (or a default size if not specified). It will then wait for a client process to open that file. When a client connects, the server and client will exchange data through this memory segment. The WitRPC framework takes care of managing the shared memory content: segmenting messages, synchronizing access, etc.

**Behavior:** Typically, the server will wait for a single client connection in MMF mode. After the client connects, they effectively have a communication channel. If that client disconnects, the server can potentially accept another connection (reusing the memory-mapped file or creating a new one). However, simultaneous multiple clients on one memory-mapped file aren't a common use-case due to complexity of coordination.

**Security:** The memory-mapped file with name `"MySharedMap"` will be created in the system's global namespace (unless you include session-specific prefixes). Any process with the name could attempt to connect. To prevent unauthorized access:

-   Use a unique, hard-to-guess name for the MMF.
    
-   Rely on operating system user account isolation (e.g., run both processes under the same user and no other user).
    
-   You can also leverage WitRPC's token auth (`WithAccessToken`) even in this scenario; the client would need the correct token to make valid requests (an unauthorized process that just opens the MMF wouldn't have the token).
    
-   If extremely sensitive, you might still enable `WithEncryption()` such that even if another process read the memory, the content is encrypted.
    

**Performance:** Memory-mapped file transport can deliver very high throughput for large data exchange (since it's essentially memory copy operations). But it's also constrained by the size of the memory region and the need to avoid contention. For best performance, choose an appropriate `size` that can accommodate your largest expected message (plus some overhead). Avoid making the size excessively large as it will reserve that amount of memory.

### Further Documentation

See the [WitRPC documentation](https://witrpc.io/) for more on the memory-mapped file transport and guidance on scenarios and performance tuning for MMF communications.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server.MMF in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
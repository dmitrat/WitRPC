

# OutWit.Communication.Server.MMF

Memory-mapped file transport server for WitRPC, allowing a server to listen for client connections via a shared memory segment (for high-performance on-machine communication).

### Overview

**OutWit.Communication.Server.MMF** enables a WitRPC server to communicate with clients through a **Memory-Mapped File**. This transport is intended for on-machine scenarios where the server and client processes share the same physical machine and memory. The server creates or opens a memory-mapped file with a given name (and size) and waits for a client to connect to that shared memory region. The MMF transport offers extremely fast communication (memory-speed reads/writes) and minimal latency since it bypasses networking entirely.

Typical use cases include launching a background "worker" process for intensive computations and communicating with it via shared memory, or any scenario requiring very high throughput between two processes on one machine. Essentially, it's like creating a private high-speed bus between your processes.

**Important:** This server transport must be paired with **OutWit.Communication.Client.MMF** on the client side. Both sides need to use the same memory-mapped file name to establish the connection. The MMF transport is **one-to-one by design**: one server and one client per channel name, which is exactly the host ↔ worker shape it exists for. When a client disconnects, the next client gets a fresh channel — but there is never more than one client at a time on one name. If you need multiple concurrent clients, use multiple distinct MMF channels (one name per client) or the named pipes transport, which natively supports multiple clients.

**Version note:** the channel layout changed in WitRPC 3.0 (lossless two-region design, see below). Both processes must run 3.0 — a 2.x peer cannot attach to a 3.0 channel. For a local link where you control both ends this is a one-time coordinated update.

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

When the server starts, it allocates a memory-mapped file of the specified size (or a default size if not specified) and waits for a client process to attach. The region is split into two equal halves, one per direction, each with its own pair of synchronization events: the writer waits for the reader to free the slot before writing the next chunk, so **no frame can ever be overwritten and no signal can be lost**. Messages larger than a directional region are chunked transparently.

**Behavior:** The server serves exactly one client at a time (see the one-to-one note above). When that client disconnects — gracefully or by dying — the server detects it (peer presence is tracked through a named mutex, which the OS abandons if the owning process exits, so no heartbeats are needed) and hands the next client a fresh channel on the same name. A reconnecting client always lands on a fresh transport instance.

**Security:** All kernel objects backing the channel (the mapped file, its events and mutexes) are created in the session-local `Local\` namespace — processes in other sessions cannot see them. Any process in the *same* session with the same rights could still attempt to attach by name, so:

-   Use a unique, hard-to-guess name for the MMF.
    
-   Rely on operating system user account isolation (e.g., run both processes under the same user and no other user).
    
-   You can also leverage WitRPC's token auth (`WithAccessToken`) even in this scenario; the client would need the correct token to make valid requests (an unauthorized process that just opens the MMF wouldn't have the token).
    
-   If extremely sensitive, enable `WithEncryption()` — in 3.0 this is authenticated AES-256-GCM, so another process reading the memory sees only ciphertext, and any tampering with frames is detected and rejected.
    

**Performance:** Memory-mapped file transport can deliver very high throughput for large data exchange (since it's essentially memory copy operations). The `size` is split into the two directional regions, and a message larger than a region is chunked — so `size` is a throughput knob (fewer, larger chunks for big payloads), not a hard cap on message size. Avoid making the size excessively large as it will reserve that amount of memory.

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
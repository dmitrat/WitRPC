
# OutWit.Communication.Serializers.MessagePack

MessagePack serialization for WitRPC method parameters, return values and event arguments — on the client and the server — as an opt-in plugin, so nobody who does not use MessagePack pays for its dependencies.

### Overview

Since 3.1.0 the WitRPC core packages carry only what every setup needs: MemoryPack for the message envelope and JSON as the default payload serializer. MessagePack lives here. Reference this package on **both** ends and call `WithMessagePack()`:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Serializers.MessagePack;

var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithMessagePack();          // parameters, results, event args
});
```

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Communication.Serializers.MessagePack;

var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://localhost:5000", maxClients: 10);
    options.WithMessagePack();          // must match the client
    options.WithService(new MyService());
});
```

`WithMessagePack(Action<MessagePackOptions>)` applies a resolver / compression configuration process-wide (for example a contractless resolver for unannotated models).

### Why this exists: bring your own models

The serializer is **MessagePack-CSharp** — the same library behind SignalR's MessagePack hub protocol. Models already annotated with `[MessagePackObject]` / `[Key(n)]` (or served by a contractless resolver) move over WitRPC **unchanged**: same attributes, same formatters, same wire bytes for the payload. You swap the transport, not the models.

Only the *payloads* are yours to choose. WitRPC's own message envelope always travels as MemoryPack, independently of this package — it is not affected by, and does not affect, your MessagePack configuration.

### Migration from 3.0

`WithMessagePack()` used to live in the core client/server packages. Add this package and the `using OutWit.Communication.Serializers.MessagePack;` line; the call sites stay as they were. The `MessageSerializerMessagePack` class moved to the same namespace.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Serializers.MessagePack in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.

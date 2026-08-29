
# OutWit.Communication.Serializers.ProtoBuf

protobuf-net serialization for WitRPC method parameters, return values and event arguments — on the client and the server — as an opt-in plugin, so nobody who does not use protobuf pays for its dependencies.

### Overview

Since 3.1.0 the WitRPC core packages carry only what every setup needs: MemoryPack for the message envelope and JSON as the default payload serializer. protobuf-net lives here. Reference this package on **both** ends and call `WithProtoBuf()`:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Communication.Serializers.ProtoBuf;

var client = WitClientBuilder.Build(options =>
{
    options.WithTcp("localhost", 5000);
    options.WithProtoBuf();             // parameters, results, event args
});
```

The server side is symmetrical: `options.WithProtoBuf()` on `WitServerBuilder` with the same `using`. `WithProtoBuf(Action<ProtoBufOptions>)` applies a protobuf-net configuration process-wide.

### Why this exists: bring your own models

The serializer is **protobuf-net**. Code-first protobuf models — `[ProtoContract]` / `[ProtoMember(n)]`, or `[DataContract]` / `[DataMember(Order = n)]` as used with **protobuf-net.Grpc** and WCF-era contracts — move over WitRPC **unchanged**. You swap the transport, not the models.

> **Proto-first gRPC (protoc / Grpc.Tools) generates `Google.Protobuf` messages, which protobuf-net does not read.** For those, use [OutWit.Communication.Serializers.GoogleProtobuf](https://www.nuget.org/packages/OutWit.Communication.Serializers.GoogleProtobuf/) instead.

Only the *payloads* are yours to choose. WitRPC's own message envelope always travels as MemoryPack, independently of this package.

### Migration from 3.0

`WithProtoBuf()` used to live in the core client/server packages. Add this package and the `using OutWit.Communication.Serializers.ProtoBuf;` line; the call sites stay as they were. The `MessageSerializerProtoBuf` class moved to the same namespace.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Serializers.ProtoBuf in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.

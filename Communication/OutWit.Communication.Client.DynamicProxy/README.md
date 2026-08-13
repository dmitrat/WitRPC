# OutWit.Communication.Client.DynamicProxy

Runtime dynamic proxy support (Castle.Core) for WitRPC clients, enabling `client.GetService<TService>()` without a source-generated proxy.

### Overview

**OutWit.Communication.Client.DynamicProxy** provides the runtime proxy generation path for the WitRPC client. When you call `GetService<TService>()` on a `WitClient`, this package uses **Castle DynamicProxy** to emit a proxy class for your service interface at runtime, so you can start calling remote methods without any code generation step.

This capability used to live inside the core packages. It was split out so that **OutWit.Communication** and **OutWit.Communication.Client** stay free of Castle.Core and publish cleanly under **NativeAOT/trimming**. The split changes nothing about the wire protocol or server behavior:

-   **If your client uses `client.GetService<TService>()`** (no arguments, or the `strongAssemblyMatch` flag) — add a reference to this package. Your code recompiles unchanged: the extension method keeps the `OutWit.Communication.Client` namespace.

-   **If your client uses the source-generated (static) proxy path** — `client.GetService<TService>(interceptor => new MyServiceProxy(interceptor))` with a `[ProxyTarget]` interface and the `OutWit.Common.Proxy.Generator` package — you do not need this package. That path stays in the core client and is AOT-compatible.

**Note:** runtime proxy emission is not available under NativeAOT. For AOT-published clients, use the static proxy path instead.

### Installation

```shell
Install-Package OutWit.Communication.Client.DynamicProxy
```

### Usage

```csharp
using OutWit.Communication.Client;

var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000/api");
    options.WithEncryption();
    options.WithTimeout(TimeSpan.FromSeconds(5));
});

await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

// Runtime-generated proxy, courtesy of this package:
IMyService service = client.GetService<IMyService>();

var result = await service.DoWorkAsync("hello");
```

Set `strongAssemblyMatch: false` when the client and server load the contract types from differently named assemblies:

```csharp
IMyService service = client.GetService<IMyService>(strongAssemblyMatch: false);
```

### Learn More

-   [WitRPC Documentation](https://witrpc.io/)
-   Main packages: **OutWit.Communication.Client** (client core), **OutWit.Communication.Server** (server side)

# OutWit.Communication.Server.Rest

The REST host for WitRPC: exposes a WitRPC service as plain HTTP + JSON endpoints, so the other side of the wire can be anything that speaks HTTP.

### Overview

**OutWit.Communication.Server.Rest** is WitRPC's **compatibility layer outward**. The persistent transports (named pipes, TCP, WebSocket, memory-mapped files) carry WitRPC's own protocol -- a handshake, message-layer encryption, a session with server-to-client events, a binary envelope -- and need WitRPC on both ends. REST deliberately does not: every call is one stateless HTTP request whose body is the arguments as plain JSON, and whose reply is the return value as plain JSON. Nothing WitRPC-specific travels on the wire, so the caller can be curl, a browser, a Python script, a JavaScript front-end, or the .NET `OutWit.Communication.Client.Rest` package -- interchangeably.

That is why the REST host is **its own server** (`WitServerRest`, built by `WitServerRestBuilder`) rather than a transport plugged into `WitServer`: a transport would wrap each request in the envelope and the handshake, and the readable contract -- the whole point -- would be gone. What is shared is everything above the wire: the same service interface, the same implementation, the same request processor and the same `IAccessTokenValidator` as the persistent server. One `ExampleService` can be hosted over WebSocket for WitRPC clients and over REST for everyone else, side by side.

Use it for:

-   **External integration** -- clients in other languages, or environments where only HTTP is feasible, call your methods by URL.
-   **Web front-ends** -- a JavaScript app calls the endpoints directly with `fetch`.
-   **A REST API without a web framework** -- no controllers, no routing tables: the service interface is the API, and the request and response shapes are documented below.

What REST gives up, by design: it is stateless, so **server-to-client events are not delivered** (poll, or use a persistent transport where callbacks matter); and there is no WitRPC message-layer encryption -- TLS (`https://`) is the transport protection, and token authorization still applies.

### Installation

```shell
Install-Package OutWit.Communication.Server.Rest
```

### Usage

The REST server is its own host (stateless HTTP, not a `WitServer` transport), built with `WitServerRestBuilder`:

```csharp
using OutWit.Communication.Server.Rest;

var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/api/example/");
    options.WithService<IExampleService>(new ExampleService());
    options.WithAccessToken("MySecretToken");      // optional: require a Bearer token
    options.WithTimeout(TimeSpan.FromSeconds(30));  // optional: per-call processing limit
    options.WithMaxBodyBytes(16 * 1024 * 1024);     // optional: 64 MB by default
    options.WithMaxConcurrentRequests(64);          // optional: unbounded by default
});
server.StartWaitingForConnection();
```

Several contracts can share one host -- the persistent server's `WithServices()` has its REST twin (since 3.2.2):

```csharp
var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/api/");
    options.WithServices()
        .AddService<IOrderService>(new OrderService())
        .AddService<IPricingService, PricingService>(new PricingService())
        .Build();
});
```

Every option has an open form that takes your own implementation: `WithRequestProcessor(processor, contracts)`, `WithAccessTokenValidator(validator)`, `WithLogger(logger)`, `WithOptions(transportOptions)`.

Or through dependency injection (`OutWit.Communication.Server.DependencyInjection`, since 3.2.0) -- registered by name, built on first use, started with the host when `autoStart` is set:

```csharp
services.AddWitRpcRestServer<IExampleService, ExampleService>("api", ctx =>
{
    ctx.WithUrl("http://localhost:5000/api/example/");
    ctx.WithAccessTokenValidator<MyTokenValidator>();   // resolved from the container
    ctx.WithLogger("WitRPC.Rest");
}, autoStart: true);

// or expose a service already registered in the container:
services.AddWitRpcRestServer("api", ctx => { ctx.WithUrl("..."); ctx.WithService<IExampleService>(); });

// or several contracts, each resolved from the container:
services.AddWitRpcRestServerWithServices("api", ctx => ctx.WithUrl("..."), composite =>
{
    composite.AddService<IOrderService, OrderService>();
    composite.AddService<IPricingService>();          // already registered
});
```

The server listens at the base URL; every method of the contract is one endpoint under it, and the caller does not need to be WitRPC at all. The contract (since 3.2.0) is plain JSON both ways:

### The wire contract

**`POST {base}/{MethodName}`** with a JSON body of the arguments -- either an **object of named arguments** or an **array of positional ones**:

```http
POST /api/example/StartProcessing HTTP/1.1
Authorization: Bearer MySecretToken
Content-Type: application/json

{"number": {"A": 2, "B": 3}, "iterations": 4}
```

```http
POST /api/example/RequestData HTTP/1.1
Content-Type: application/json

["hello"]
```

-   Names are matched to the method's parameter names case-insensitively; `param1`, `param2`, ... are accepted as positional aliases. A JSON object whose keys match no parameter is taken positionally, in document order.
-   Every argument is bound against the method's **declared parameter type** -- a nested object becomes your DTO, `null` becomes a null argument, a missing optional argument is null. No type names, no encoding, no envelope.
-   Overloads resolve by name and argument count.

**`GET {base}/{MethodName}?name=value&...`** for simple arguments (query values by parameter name or `param1=...`). Strings, enums, GUIDs and dates are taken verbatim; numbers and booleans must parse; anything else must be JSON (`?number={"A":2,"B":3}`, URL-encoded).

**The reply is the return value as plain JSON** -- `"hello"`, `42`, `{"A":6,"B":9}` -- with `200`; a `void` method answers `204 No Content`. Errors carry an HTTP status and a small JSON object:

```json
{"status":"BadRequest","error":"Method 'RequestData' does not take 2 argument(s)"}
```

**HTTP status mapping:** 200 result; 204 nothing to return; 400 unbindable arguments or invalid JSON; 401 invalid token; 404 no such method; 405 unsupported HTTP verb; 408 processing timeout; 413 body over the size cap; 500 service fault (with `details`).

`Authorization: Bearer <token>` is required when the server is configured with an access token. Generic methods are not callable over REST; property getters are (`GET .../StringProperty` calls `get_StringProperty`).

**Limits and behavior:** requests are handled concurrently and independently -- a slow or throwing call neither blocks the next request nor takes the listener down. The body size is capped (`maxBodyBytes`, 64 MB default -> 413), concurrency is bounded (`maxConcurrentRequests`), and processing is time-bounded by the configured timeout. Server-to-client events are **not supported** over REST (stateless request/reply) -- use WebSocket or another persistent transport for callbacks.

On the client side, use **OutWit.Communication.Client.Rest** from .NET -- it sends exactly the JSON above (a positional array, or a query string when its mode allows) -- or call the endpoints from any HTTP-capable stack.

**Security and HTTPS:** In production, run the REST endpoint over HTTPS (TLS) — that is the transport protection for REST. WitRPC's message-layer encryption does not apply to the REST transport (each call is a bare HTTP request); use an `https://` URL in `WithUrl(...)`, bind a certificate for the host/port (HttpListener on Windows typically needs a `netsh http add sslcert` binding), and keep token auth on so only authorized clients call your endpoints.

### Further Documentation

Visit the [witrpc.io](https://witrpc.io/) documentation for more on the REST transport. The request and response shapes above are the whole contract; complex arguments are the JSON your DTOs serialize to.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server.Rest in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
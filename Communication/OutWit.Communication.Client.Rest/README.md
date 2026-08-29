
# OutWit.Communication.Client.Rest

REST transport client for WitRPC, allowing communication with a WitRPC server over HTTP (RESTful) calls – ideal for integrating with web services or non-.NET clients.

### Overview

**OutWit.Communication.Client.Rest** enables a WitRPC client to use **HTTP/REST** as the underlying transport. Instead of a persistent socket or pipe, the client makes HTTP requests to call service methods on the server. The REST transport is particularly useful for integrating with environments where a full-time connection may not be possible or when you want to expose/consume the service via standard web protocols. For example, if a WitRPC server is configured with REST, you could call it from a JavaScript frontend or a scripting language by making HTTP requests, or conversely use this client to call an existing HTTP-based service.

**Key scenarios:**

-   **Web Integration:** Easily call services from web or mobile apps by exposing the WitRPC service over HTTP. A .NET client can also use the REST transport to consume a service in a firewall-friendly way (HTTP port).
    
-   **Interoperability:** Allow non-.NET or non-WitRPC clients to interact with your service. Since the communication is via standard HTTP with JSON payloads, any technology stack can consume it (they just need to follow the expected request format).
    
-   **Stateless Calls:** Each call is a separate HTTP request/response, which is suitable for request-reply patterns that don’t require a persistent connection.
    

Keep in mind that the REST transport operates in a stateless, request-response manner. It does not maintain a continuous connection, so **server-to-client callbacks (events)** will not be delivered in real-time through the same channel. (A client could poll for events or updates, but that logic is up to the client implementation.) If your application needs real-time notifications from the server, consider WebSocket or another persistent transport.

This client works with **OutWit.Communication.Server.Rest** on the server. The server will host HTTP endpoints that the client calls.

### Installation

```shell
Install-Package OutWit.Communication.Client.Rest
```

### Usage

The REST client is stateless (every call is an independent HTTP request, nothing to `ConnectAsync`) and is built with `WitClientRestBuilder` (since 3.2.1):

```csharp
using OutWit.Communication.Client.Rest;

var client = WitClientRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/api/example/");   // base URL of the REST server
    options.WithAccessToken("YourBearerToken");              // or WithoutAuthorization()
    options.WithTimeout(TimeSpan.FromSeconds(30));           // per-call limit
    options.WithMode(RestClientRequestModes.AllowGetForMethodsWithoutParameters);
});

// Source-generated proxy: [ProxyTarget("ExampleServiceProxy")] on the interface
// plus the OutWit.Common.Proxy.Generator package — no extra runtime dependency:
IExampleService service = client.GetService<IExampleService>(interceptor => new ExampleServiceProxy(interceptor));
```

Runtime-generated proxies work as well — add the opt-in [OutWit.Communication.Client.DynamicProxy](https://www.nuget.org/packages/OutWit.Communication.Client.DynamicProxy/) package (since 2.4.0; its `GetService<T>()` accepts a REST client since 3.1.1):

```csharp
IExampleService service = client.GetService<IExampleService>();
```

Every option has an open form that takes your own implementation, next to the convenience ones (since 3.2.2):

```csharp
var client = WitClientRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/api/example/");
    options.WithAccessToken(() => tokenCache.Current);               // fetched on every call; an async form exists too
    options.WithAccessTokenProvider(new MyTokenProvider());           // any IAccessTokenProvider
    options.WithHttpMessageHandler(new SocketsHttpHandler { /* proxy, certificates, ... */ });
    options.WithHttpClient(existingHttpClient);                       // or a client you already own
    options.WithLogger(logger);                                       // token, timeout and transport failures
});
```

In this example, the client sends HTTP requests to `http://localhost:5000/api/example/`. Each method call on `service` becomes a `POST {base}/{MethodName}` whose body is a plain **JSON array of the arguments** (`["hello", 42]`) -- the same readable shape any curl or JavaScript caller would send, so a WitRPC client and a hand-written HTTP client are interchangeable on the wire. When `RestClientTransportOptions.Mode` allows it (`PostOnly` is the default), a call goes out as `GET {base}/{MethodName}?param1=...` instead. The full contract, with request and response examples, is documented in the **OutWit.Communication.Server.Rest** package README.

The reply is the return value as plain JSON (`204 No Content` for `void`). On an HTTP error status the client reads the server's JSON error object (`{"status":"BadRequest","error":"..."}`) and raises it as a typed fault from the proxy rather than a raw `HttpRequestException`. Client-local failures map to honest statuses: an HTTP timeout becomes `Timeout`, a send/receive failure becomes `TransportError` -- neither is confused with a service fault. Generic methods cannot be called over REST.

The access token supplied through the `IAccessTokenProvider` (e.g. `AccessTokenProviderStatic("YourBearerToken")`) is sent as an `Authorization: Bearer` header on every request; the server validates it and answers 401 when it does not match.

Through dependency injection (`OutWit.Communication.Client.DependencyInjection`, since 3.2.0) the service interface is injected directly as a proxy over a named REST client:

```csharp
services.AddWitRpcRestClient<IExampleService>("api", ctx =>
{
    ctx.WithUrl("http://localhost:5000/api/example/");
    ctx.WithAccessTokenProvider<MyTokenProvider>();   // resolved from the container
    ctx.WithHttpClient("example-api");                 // services.AddHttpClient("example-api"): handlers, resilience, headers
    ctx.WithLogger("WitRPC.Rest");
});

// later: IExampleService is an injectable proxy; IWitClientRestFactory gives the raw WitClientRest
```

**Server Setup:** On the server side, using OutWit.Communication.Server.Rest:

```csharp
var server = WitServerRestBuilder.Build(options =>
{
    options.WithUrl("http://localhost:5000/api/example/");
    options.WithService<IExampleService>(new ExampleService());
});
server.StartWaitingForConnection();
```

Ensure the base URL matches between client and server.

**Security:** Use HTTPS for the REST transport in production — an `https://` URL with a TLS certificate configured on the server. TLS is the transport protection here: WitRPC's message-layer encryption (`WithEncryption()`) does not apply to REST, since each call is an independent bare HTTP request with no encryption handshake. Token auth still applies and is recommended.

**Use Cases:** The REST client is perfect if you want to call a WitRPC service from an environment where you can’t run the full WitRPC client (for example, from a web page using AJAX, or a Python script). It trades some performance for wide accessibility. Each RPC call incurs HTTP overhead, so for high-frequency call patterns a persistent transport might be better.

### Further Documentation

Learn more about the REST transport and how to format calls in the [WitRPC documentation](https://witrpc.io/). The documentation includes details on request/response formats and examples of integrating with non-.NET clients.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Client.Rest in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
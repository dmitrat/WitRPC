
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

The REST client is stateless and is constructed directly (it does not go through `WitClientBuilder` and needs no `ConnectAsync` — every call is an independent HTTP request):

```csharp
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Model;

var client = new WitClientRest(
    new RestClientTransportOptions
    {
        Host = (HostInfo)"http://localhost:5000/api/example/" // base URL for the RESTful service
    },
    new AccessTokenProviderStatic("YourBearerToken")); // or AccessTokenProviderPlain() when no auth is required

// Source-generated proxy: [ProxyTarget("ExampleServiceProxy")] on the interface
// plus the OutWit.Common.Proxy.Generator package — no extra runtime dependency:
IExampleService service = new ExampleServiceProxy(new RequestInterceptor(client, true));
```

Runtime-generated proxies work as well — add the opt-in [OutWit.Communication.Client.DynamicProxy](https://www.nuget.org/packages/OutWit.Communication.Client.DynamicProxy/) package (since 2.4.0) and create the proxy through Castle:

```csharp
using Castle.DynamicProxy;

IExampleService service = new ProxyGenerator()
    .CreateInterfaceProxyWithoutTarget<IExampleService>(new RequestInterceptorDynamic(client, true));
```

In this example, the client sends HTTP requests to `http://localhost:5000/api/example/`. Each method call on `service` becomes a `POST {base}/{MethodName}` whose JSON body is the WitRPC request envelope (`WitRequest`): the method name, a stable `InvocationId`, the arguments (each one serialized to JSON, UTF-8 encoded and base64-wrapped), and the contract/method ids the server dispatches by. A parameterless method can go out as a plain `GET {base}/{MethodName}` when `RestClientTransportOptions.Mode` allows it (`PostOnly` is the default). The full wire contract, with examples, is documented in the **OutWit.Communication.Server.Rest** package README.

The response body is always a JSON `WitResponse` — the client reads it **even on a non-2xx HTTP status**, so a server fault surfaces as a typed fault from the proxy rather than a raw `HttpRequestException`. Client-local failures map to honest statuses: an HTTP timeout becomes `Timeout`, a send/receive/parse failure becomes `TransportError` — neither is confused with a service fault.

The access token supplied through the `IAccessTokenProvider` (e.g. `AccessTokenProviderStatic("YourBearerToken")`) is sent as an `Authorization: Bearer` header on every request; the server validates it and answers 401 when it does not match.

**Server Setup:** On the server side, using OutWit.Communication.Server.Rest, you would do something like:

```csharp
options.WithRest("http://localhost:5000/api/example/");
```

The server will then listen on that URL prefix for incoming requests. Ensure the paths and port match between client and server.

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
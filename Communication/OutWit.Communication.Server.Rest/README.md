# OutWit.Communication.Server.Rest

REST transport server for WitRPC, exposing your services as HTTP REST endpoints to allow calls from web or external clients via standard HTTP.

### Overview

**OutWit.Communication.Server.Rest** enables a WitRPC server to serve incoming requests over **HTTP** as a RESTful API. With this transport, the server sets up an HTTP listener on a specified URL (and port) and translates HTTP requests into calls to your service methods. This allows clients that cannot maintain persistent connections (or non-.NET clients) to interact with your service using standard HTTP calls. In effect, it makes your service accessible in a similar way to a typical Web API.

This is particularly useful for:

-   **External Integration:** Exposing services to clients written in other languages or running in environments where only HTTP is feasible. Any HTTP-capable client (cURL, browser, etc.) could call your service's methods by making requests to the correct URLs.
    
-   **Web Clients:** Enabling simple web front-ends to trigger server-side operations. For example, you might have a JavaScript app making AJAX calls to these REST endpoints.
    
-   **Quick API Deployment:** You can stand up a basic REST API for your service without writing boilerplate controllers or using a full web framework: WitRPC will handle the routing of HTTP requests to the service methods.
    

Keep in mind that the REST transport is stateless. The server handles each HTTP request independently. It doesn't maintain session state or persistent connections with clients (unlike WebSocket or TCP). This means server-to-client events are not pushed to REST clients in real-time; those clients would need to poll or use some long-polling mechanism to receive event-like updates. If real-time feedback is crucial, consider using WebSockets or another push-capable transport.

### Installation

```shell
Install-Package OutWit.Communication.Server.Rest
```

### Usage

To expose a service over REST, specify an HTTP URL prefix when configuring the server:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.Rest;

var server = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
    options.WithRest("http://localhost:5000/api/example/");
    options.WithAccessToken("MySecretToken"); // optional: require a token for requests
    // Note: The REST transport uses JSON serialization by default for requests/responses
});
server.StartWaitingForConnection();
Console.WriteLine("RESTful RPC server running at http://localhost:5000/api/example/");
```

In this configuration, the server listens for HTTP requests at the base URL `http://localhost:5000/api/example/`. The contract (since 3.0) is written down and stable:

### The wire contract

**`POST {base}/{MethodName}`** — the JSON body is a WitRPC request envelope, the same `WitRequest` every other transport carries:

```http
POST /api/example/Concat HTTP/1.1
Authorization: Bearer MySecretToken
Content-Type: application/json

{
  "MethodName": "Concat",
  "InvocationId": "11111111-2222-3333-4444-555555555555",
  "Parameters": ["ImhlbGxvIg==", "NDI="],
  "ContractId": 0,
  "MethodId": 0,
  "ParameterTypesByName": [ ... ]
}
```

-   `Parameters` — one element per argument: the argument's **JSON value, UTF-8 encoded, then base64-wrapped** (`ImhlbGxvIg==` is `"hello"`, `NDI=` is `42`). An empty element is a `null` argument.
-   Method resolution: when `MethodId` is non-zero (the .NET `WitClientRest` proxy always sends it), the server dispatches by id and deserializes arguments against the method's declared parameter types — no type information travels on the wire. With `MethodId` = 0 the server resolves by `MethodName`, and a method **with** parameters then needs `ParameterTypes`/`ParameterTypesByName` filled in; a parameterless method resolves by name alone.
-   `Authorization: Bearer <token>` is required when the server is configured with an access token; requests without a valid token get **401**.

**`GET {base}/{MethodName}`** — allowed for **parameterless** methods only; equivalent to a POST with an empty parameter list.

**The response body is always a JSON `WitResponse`** — on success *and* on failure — so a caller can always read the outcome from the body:

```json
{"Status":200,"Data":"ImhlbGxvNDIi","ErrorMessage":null,"ErrorDetails":null}
{"Status":500,"Data":null,"ErrorMessage":"Something failed","ErrorDetails":null}
```

`Data` is the return value in the same base64-wrapped-JSON form; `null` for `void` methods.

**HTTP status mapping:** 200 Ok · 400 bad/unparsable request · 401 invalid token · 405 unsupported HTTP verb · 408 processing timeout · 413 body over the size cap · 500 service fault.

**Limits and behavior:** requests are handled concurrently and independently — a slow or throwing call neither blocks the next request nor takes the listener down. The body size is capped (`maxBodyBytes`, 64 MB default → 413), concurrency is bounded (`maxConcurrentRequests`), and processing is time-bounded by the configured timeout. Server-to-client events are **not supported** over REST (stateless request/reply) — use WebSocket or another persistent transport for callbacks.

On the client side, use **OutWit.Communication.Client.Rest** from .NET — it produces exactly the envelope above — or reproduce the envelope from any HTTP-capable stack.

**Security and HTTPS:** In production, run the REST endpoint over HTTPS (TLS) — that is the transport protection for REST. WitRPC's message-layer encryption does not apply to the REST transport (each call is a bare HTTP request); use `https://` in `WithRest(...)`, bind a certificate for the host/port (HttpListener on Windows typically needs a `netsh http add sslcert` binding), and keep token auth on so only authorized clients call your endpoints.

### Further Documentation

Visit the [witrpc.io](https://witrpc.io/) documentation for detailed information on the REST transport, including how method parameters and return values are serialized in the HTTP requests/responses and how to handle things like binary data or complex types in a REST scenario.

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
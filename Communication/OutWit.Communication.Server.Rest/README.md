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

In this configuration, the server listens for HTTP requests at the base URL `http://localhost:5000/api/example/`. This transport is WitRPC's **compatibility layer**: the caller on the other side does not need to be WitRPC at all -- curl, a browser, a Python script. The contract (since 3.2.0) is plain JSON both ways:

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
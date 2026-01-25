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

In this configuration, the server will listen for HTTP requests at the base URL `http://localhost:5000/api/example/`. Each RPC method call is mapped to an HTTP endpoint:

-   A client performing an HTTP POST to `http://localhost:5000/api/example/SomeMethod` (with JSON payload) will invoke `SomeMethod` on `MyService`.
    
-   If the method returns a result, the server will return it as a JSON response body. If the method is void (or one-way), the server might return a 204 No Content or similar.
    
-   If a token is set (as in the example), the server expects an `Authorization: Bearer MySecretToken` header on incoming requests. Unauthorized requests will receive an HTTP 401 response.
    

On the client side, you could use OutWit.Communication.Client.Rest (if it's a .NET client) or simply make HTTP requests using any HTTP client. The WitRPC client will handle constructing the proper URLs and payloads if you use the Client.Rest package.

**Security and HTTPS:** In production, you should run the REST endpoint over HTTPS (TLS) to ensure encryption of data in transit. To do this, you would:

-   Configure an HTTPS URL in `WithRest`, e.g., `options.WithRest("https://myhost.example.com/api/example/")`.
    
-   Ensure that the specified host and port have a certificate bound (HttpListener on Windows requires you to configure this, often via `netsh` or using a certificate that matches the domain).
    
-   Clients would then use `https://` for their requests. WitRPC's REST client will happily work with an HTTPS address as well.  
    If HTTPS is used, you might not need WitRPC's message-level encryption (`WithEncryption()`), since TLS already provides transport security. However, using token auth is still recommended to ensure only authorized clients call your endpoints.
    

**Response Codes and Errors:** The REST server will map WitRPC's responses to HTTP status codes. For example, if a method throws an exception or returns an error status, the server might respond with a 400 or 500 series status code and possibly a JSON error message. This design allows non-.NET clients to handle errors in a straightforward way via HTTP responses.

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
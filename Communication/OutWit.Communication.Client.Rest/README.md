
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

To use the REST transport, configure the client with the base URL of the REST endpoint:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Serializers;

var client = WitClientBuilder.Build(options =>
{
    options.WithRest("http://localhost:5000/api/example/"); // base URL for the RESTful service
    options.WithJson();       // REST uses JSON format by default
    options.WithoutEncryption(); // rely on HTTPS for encryption at transport level
    // If the server requires an access token for auth:
    // options.WithAccessToken("YourBearerToken");
});
await client.ConnectAsync(TimeSpan.FromSeconds(5));

IExampleService service = client.GetService<IExampleService>();
```

In this example, the client will send HTTP requests to `http://localhost:5000/api/example/`. Each method call on `service` will be translated into an HTTP request (usually a POST) to an endpoint like `http://localhost:5000/api/example/MethodName` with a JSON payload containing the method parameters. The server will process the request and send back a JSON response, which the client library will convert into the method’s return value (or throw an exception if the server returned an error).

If an access token is provided via `.WithAccessToken("YourBearerToken")`, the client will include it as a Bearer token in the `Authorization` header of each HTTP request. The server can then validate this token to authorize the call.

**Server Setup:** On the server side, using OutWit.Communication.Server.Rest, you would do something like:

```csharp
options.WithRest("http://localhost:5000/api/example/");
```

The server will then listen on that URL prefix for incoming requests. Ensure the paths and port match between client and server.

**Security:** It’s recommended to use HTTPS when using the REST transport in production. This means using an `https://` URL (and corresponding `wss://` for WebSocket if mixed) and having a TLS certificate configured. The WitRPC REST server (based on HttpListener) will encrypt traffic at the transport layer in that case. The `.WithEncryption()` option in WitRPC is generally not used for REST, because you typically rely on HTTPS for security. (If you did call `.WithEncryption()` as well, the payloads would be double-encrypted, which usually isn’t necessary.)

**Use Cases:** The REST client is perfect if you want to call a WitRPC service from an environment where you can’t run the full WitRPC client (for example, from a web page using AJAX, or a Python script). It trades some performance for wide accessibility. Each RPC call incurs HTTP overhead, so for high-frequency call patterns a persistent transport might be better.

### Further Documentation

Learn more about the REST transport and how to format calls in the [WitRPC documentation](https://witrpc.io/). The documentation includes details on request/response formats and examples of integrating with non-.NET clients.
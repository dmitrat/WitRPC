# OutWit.Communication.Client.Tcp

TCP transport client for WitRPC, enabling network communication over TCP sockets (with optional TLS support) to connect to WitRPC servers.

### Overview

**OutWit.Communication.Client.Tcp** enables a WitRPC client to communicate with a server over **TCP/IP**. This transport is ideal for distributed systems that require communication across network boundaries or over the internet. TCP is a low-level, robust transport that provides reliable, ordered delivery of data, making it suitable for large-scale applications and services. With this client, you can connect to a WitRPC server listening on a specific host and port.

Key points:

-   **Network Communication:** Unlike named pipes or memory-mapped files (which are for local IPC), TCP works across machines. You provide the server's hostname or IP address and port, and the client will connect to that endpoint over the network.
    
-   **Optional TLS Security:** This client supports secure connections via TLS. WitRPC provides `WithTcpSecure` methods to handle the SSL/TLS handshake. When using TLS, the server must have an X.509 certificate and the client will validate it. This allows you to securely communicate with a server over the internet. (You can use WitRPC's own encryption on top of TLS as well, but typically if using TLS, you might disable the additional layer of encryption to avoid double-encrypting.)
    
-   **High Performance:** TCP has minimal overhead and is suitable for high-throughput and low-latency communication. The WitRPC protocol runs efficiently on top of TCP, allowing multiple requests and responses to be handled concurrently over the single connection.
    

As with all transport-specific packages, OutWit.Communication.Client.Tcp should be used with the matching server package **OutWit.Communication.Server.Tcp** on the server side (both client and server must use TCP to communicate).

### Installation

```shell
Install-Package OutWit.Communication.Client.Tcp
```

### Usage

Configure the TCP endpoint when building the client. For example:

```csharp
using OutWit.Communication.Client;
using OutWit.Communication.Client.Tcp;
using OutWit.Communication.Serializers;
using OutWit.Communication.Client.Encryption;

var client = WitClientBuilder.Build(options =>
{
    // Connect to server at example.com on port 5001 using TCP:
    options.WithTcp("example.com", 5001);
    options.WithJson();
    options.WithEncryption(); // enable message-level encryption (optional, see below)
    // If server requires a token:
    // options.WithAccessToken("SecureToken");
});
await client.ConnectAsync(TimeSpan.FromSeconds(5));

IMyService service = client.GetService<IMyService>();
```

In this example, the client opens a TCP connection to `example.com:5001`. If your server is on the same machine, you could use `"localhost"` or the machine's local IP address instead of `"example.com"`. The `ConnectAsync` call attempts to establish the socket connection within 5 seconds.

By default, we called `WithEncryption()`, which means the WitRPC protocol will encrypt the request/response payloads at the application layer (using AES/RSA). This is useful if you're running over plaintext TCP. If you plan to use TLS at the transport layer, you might omit this (or use `WithoutEncryption()`).

**Using TLS (Secure TCP):** If the server is configured with TLS (via `WithTcpSecure`), the client should also use `WithTcpSecure` to connect. For example:

```csharp
options.WithTcpSecure("example.com", 5002, "example.com");
// Where "example.com" is both the host address and the expected certificate name.
```

This will initiate an SSL/TLS handshake with the server on port 5002. The third parameter is the server's host name for certificate validation (often the same DNS name you used to connect). Optionally, you can provide a custom certificate validation callback if you need to accept self-signed certs or implement custom validation logic. When using `WithTcpSecure`, consider not using WitRPC's own encryption (`WithEncryption()`), because the TCP connection itself will be encrypted by TLS.

**Authorization:** Just like other transports, you can use `WithAccessToken` on the TCP client to include an authorization token. The server (with OutWit.Communication.Server.Tcp) can then validate this token for each connection or request.

Once connected, you use the `service` proxy exactly as you would in a local scenario: call methods, await results, handle events. The TCP client will keep the connection open until you dispose the client or an error/timeout occurs. This allows multiple calls to reuse the same connection efficiently.

**Firewall Note:** Ensure that the port you are connecting to is open on the server's firewall. For instance, if connecting to port 5001, that port must be allowed through any firewall on the server machine and network.

### Further Documentation

See the [WitRPC documentation](https://witrpc.io/) for more about TCP transport usage, including details on setting up TLS certificates and best practices for deployment in production environments.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Client.Tcp in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.

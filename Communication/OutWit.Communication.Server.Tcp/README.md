
# OutWit.Communication.Server.Tcp

**NuGet Description:** TCP transport server for WitRPC, enabling a server to host services over a TCP port (supports plaintext or TLS-secured connections).

### Overview

**OutWit.Communication.Server.Tcp** allows a WitRPC server to accept client connections over **TCP/IP**. This is the transport to use for cross-machine or internet communication when you want high performance and full-duplex capability. The server listens on a specified TCP port for incoming connections from WitRPC clients. TCP is a reliable, streaming protocol, making it suitable for continuous, bidirectional communication in distributed systems.

This server supports:

-   **Multiple Clients:** You can specify `maxNumberOfClients` when configuring TCP, which limits how many clients can connect at once. The server will accept connections up to that number concurrently.
    
-   **Secure Sockets (TLS):** OutWit.Communication.Server.Tcp supports SSL/TLS encryption. If you provide an X.509 certificate via `WithTcpSecure`, the server will use it to perform TLS handshakes, allowing clients to connect securely (similar to HTTPS but for the custom protocol). Clients must use `WithTcpSecure` as well and will validate the server's certificate.
    
-   **Hybrid Encryption:** You have the flexibility to use WitRPC's own encryption (`WithEncryption()`) on top of TCP, or rely solely on TLS, or even use both. Typically, if using TLS, you might disable the additional encryption to avoid redundancy, but both options are available. Authorization via token is also fully supported on TCP connections.
    

### Installation

```shell
Install-Package OutWit.Communication.Server.Tcp
```

### Usage

Below are examples for configuring a TCP server, both without and with TLS:

```csharp
using OutWit.Communication.Server;
using OutWit.Communication.Server.Tcp;
using System.Security.Cryptography.X509Certificates;

// Example 1: Plain TCP server
var serverPlain = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
    options.WithTcp(port: 5001, maxNumberOfClients: 50);
    options.WithJson();
    options.WithEncryption();    // encrypt messages at the protocol level
    options.WithAccessToken("SecretToken"); // require clients to provide this token
});
serverPlain.StartWaitingForConnection();
Console.WriteLine("TCP server listening on port 5001 (unencrypted).");

// Example 2: TLS-secured TCP server
var certificate = new X509Certificate2("serverCert.pfx", "pfx-password"); // your SSL certificate
var serverSecure = WitServerBuilder.Build(options =>
{
    options.WithService(new MyService());
    options.WithTcpSecure(port: 5002, maxNumberOfClients: 50, certificate: certificate);
    options.WithJson();
    options.WithoutEncryption(); // rely on TLS for encryption
    options.WithAccessToken("SecretToken");
});
serverSecure.StartWaitingForConnection();
Console.WriteLine("Secure TCP server listening on port 5002 (TLS enabled).");
```

In the first example, the server listens on TCP port 5001 and allows up to 50 clients concurrently. We use JSON for serialization, enable WitRPC's built-in encryption (so even though the socket is plain, messages are encrypted with AES/RSA), and require a token `"SecretToken"` for clients to connect.

In the second example, the server listens on port 5002 with a TLS certificate. We call `WithTcpSecure`, passing in the `certificate` object. This means the server will perform an SSL/TLS handshake for each connection. We disabled the additional WitRPC encryption (using `WithoutEncryption()`) because TLS already encrypts the data. We still require the same token for authentication. Clients connecting to this server need to use `WithTcpSecure("host", 5002, "hostName")` and trust or validate the certificate. Once connected, all RPC traffic is protected by TLS.

**Certificate Management:** In a production environment, you'd obtain a certificate (e.g., from a CA) for your server's domain. In development or testing, you might use a self-signed certificate. Clients may need to supply a `RemoteCertificateValidationCallback` in `WithTcpSecure` to accept self-signed certs (or you can install the cert in the client's trust store). In the above code, we assumed the certificate is valid and trusted by clients.

**Firewall Considerations:** Ensure that the port you choose (5001, 5002 in the examples) is open and forwarded as necessary. The server will bind to that port on all network interfaces by default. You can restrict it by specifying an IP address if needed (e.g., `options.WithTcp("127.0.0.1", port)` in an overload if supported, or by using a HostInfo object).

**Performance and Threads:** The TCP server will handle each client connection on a separate thread or async task. Make sure your service implementation can handle multiple threads if `maxNumberOfClients` is more than 1. WitRPC's architecture is asynchronous and can scale to many connections, but the actual throughput also depends on how efficiently your service methods execute and respond.

### Further Documentation

For more information about the TCP transport, including details on certificate setup and best practices for secure deployment, refer to the [WitRPC documentation](https://witrpc.io/). The documentation also covers error handling (e.g., how connection drops or timeouts are signaled) and tuning options for high-load scenarios.

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Communication.Server.Tcp in a product, a mention is appreciated (but not required), for example:
"Powered by WitRPC (https://witrpc.io/)".

## Trademark / Project name

"WitRPC" and the WitRPC logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitRPC");
- use the name to indicate compatibility (e.g., "WitRPC-compatible").

You may not:
- use "WitRPC" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitRPC logo to promote forks or derived products without permission.
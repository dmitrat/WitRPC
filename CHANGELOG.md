# Changelog

All notable changes to the WitRPC project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **Note**: Since 2.3.1, package versions diverge per package family. Each section below lists the package versions it produced (verified against csproj `<Version>` values).

## [Open extension points: Server / Client / Client.DynamicProxy 3.1.1, Server.Rest / Client.Rest 3.2.2, Server.DependencyInjection / Client.DependencyInjection 3.2.1] - 2026-08-29

### Added

- Every builder option now has an open form that takes an implementation, next to the convenience presets: `WithTransport(ITransportServerFactory)` / `WithTransport(ITransportClient)` and `WithDiscovery(IDiscoveryServer)` on the persistent builders; `WithLogger(ILogger)`, `WithHttpClient(HttpClient)`, `WithHttpMessageHandler(HttpMessageHandler)` and the token callbacks `WithAccessToken(Func<string>)` / `WithAccessToken(Func<Task<string>>)` on the REST client.
- `WitServerRestBuilder.WithServices()` -- several contracts in one REST host (`CompositeServiceBuilderRest`) -- and `AddWitRpcRestServerWithServices(...)` for the same through the container.
- Proxies straight off the REST client: `client.GetService<T>(interceptor => new TProxy(interceptor))` (source-generated) and, with Client.DynamicProxy, `client.GetService<T>()` -- the runtime overload now accepts any `IClient`.
- The DI contexts resolve every interface-typed option from the container: `WithRequestProcessor<TProcessor>()` and `WithTransport<TFactory>()` (server), `WithTransport<TTransport>()` (client), `WithRequestProcessor<TProcessor>(contracts)` (REST server), `WithLogger<TLogger>()` / `WithLogger(category)` / `WithHttpClient(name)` through `IHttpClientFactory` (REST client; Client.DependencyInjection now references Microsoft.Extensions.Http).

### Changed

- `WitClient.Dispose()` is idempotent: a client the DI factory owns and the caller has already disposed no longer throws `ObjectDisposedException` when the host shuts down.
- The REST client logs token, timeout and transport failures when a logger is set, and reports an HTTP-level cancellation (a supplied `HttpClient`'s own timeout) as `Timeout` rather than `TransportError`.

## [REST via DI: Client.DependencyInjection / Server.DependencyInjection 3.2.0, Client.Rest / Server.Rest 3.2.1] - 2026-08-29

### Added

- **REST servers through dependency injection**: `AddWitRpcRestServer(name, ctx => ...)`, `AddWitRpcRestServer<TService, TImplementation>(...)`, both with an `autoStart` overload that starts the server with the host (`WitServerRestHostedService`); `IWitServerRestFactory.GetServer(name)`; a `WitServerRestBuilderContext` with `WithService<T>()`, `WithAccessTokenValidator<T>()` and `WithLogger` resolving from the container. Server.DependencyInjection now references Server.Rest (no third-party dependencies come with it).
- **REST clients through dependency injection**: `AddWitRpcRestClient(name, ctx => ...)` and `AddWitRpcRestClient<TService>(...)`, which injects the service interface as a proxy over the named client; `IWitClientRestFactory.GetClient(name)` / `GetService<T>(name)`; `WithAccessTokenProvider<T>()` on the context.
- `WitClientRestBuilder` -- the REST client gets the same fluent builder as every other client: `WithUrl`, `WithHost`, `WithOptions`, `WithMode`, `WithTimeout`, `WithAccessToken`, `WithAccessTokenProvider`, `WithoutAuthorization`, and `Build(options)` for the DI path.
- `WitServerRestBuilder.Build(options)` overload, `WithMaxBodyBytes` and `WithMaxConcurrentRequests` extensions.

### Fixed

- The core, `Client.Rest` and `Server.Rest` READMEs showed an `options.WithRest(...)` call on `WitServerBuilder` that has never existed; they now show `WitServerRestBuilder` / `WitClientRestBuilder` and the DI registrations.

## [Client.Rest / Server.Rest 3.2.0] - 2026-08-29

REST packages only: 3.1.0 -> 3.2.0. Every other package stays 3.1.0 (Tcp 3.1.1).

### Changed

- **REST is a readable compatibility layer again.** The 3.0 rebuild put the whole `WitRequest` envelope in the HTTP body -- arguments as base64-wrapped JSON plus type names or method ids -- which made the transport unusable from anything that is not WitRPC, defeating the reason it exists. 3.2.0 restores the original idea on top of the 3.x internals: `POST {base}/{Method}` with a plain JSON body (an object of named arguments or an array of positional ones), `GET {base}/{Method}?name=value` for simple arguments, the return value back as plain JSON (`204` for void), and an HTTP error status with a small JSON error object (`{"status":"BadRequest","error":"..."}`) on failure. Arguments are bound against the contract's declared parameter types by a new `RestMethodCatalog`, so a caller needs the method name and the values -- nothing else. Names match case-insensitively; `param1...paramN` positional aliases and JSON-object-in-document-order are accepted for 2.x-style callers; unknown method is 404, wrong arity or invalid JSON is 400. Property getters are callable; generic methods are not.
- `WitClientRest` sends the same readable shape (a positional JSON array, or a query string in the GET modes) and reads plain JSON back; `WitServerRestBuilderOptions` gains `Contracts`, filled by `WithService<T>`; `WithRequestProcessor(processor, params Type[] contracts)` for custom processors.
- New `CommunicationTestsRestInterop` fixture drives the server with a bare `HttpClient` and plain JSON -- the contract is tested from the outside, not through the proxy.

## [Client.Tcp / Server.Tcp 3.1.1] - 2026-08-29

TCP packages only: 3.1.0 -> 3.1.1. Every other package stays 3.1.0.

### Fixed

- **TCP latency: Nagle + delayed ACK were stalling every message.** Neither end set `NoDelay`, and a frame went out as two writes (the four-byte length prefix, then the payload) -- the write-write-read pattern that Nagle's algorithm holds back until the peer's delayed ACK arrives, roughly 200 ms per message on Windows. Both ends now set `TcpClient.NoDelay = true` and write each frame with a single call. Found through the release gate: every CI flake during the 3.x publish was a TCP test missing a one-second budget while pipes and WebSocket were fine. TCP+TLS shares the base and gets the same fix.

## [3.1.0] - 2026-08-29

> **Highlighted: serializers become plugins.** The core packages now carry only what every setup needs -- MemoryPack for the message envelope and JSON as the default payload serializer. MessagePack and protobuf-net move to opt-in packages, and a third plugin brings Google.Protobuf for proto-first gRPC models. A WitRPC client no longer drags MessagePack and protobuf-net (and their transitive graphs) into every application -- notably every Blazor WebAssembly bundle.

Package versions after this release: every package 3.1.0 (one number for the family), plus three new ones.

### Added

- **New package**: `OutWit.Communication.Serializers.MessagePack` -- `MessageSerializerMessagePack` and the `WithMessagePack()` extensions (client and server, one generic method via the new `ISerializationOptions`). Models annotated for MessagePack-CSharp (SignalR's MessagePack protocol) move over WitRPC unchanged.
- **New package**: `OutWit.Communication.Serializers.ProtoBuf` -- `MessageSerializerProtoBuf` and `WithProtoBuf()`. Code-first protobuf-net models (`[ProtoContract]`, `[DataContract]` as used with protobuf-net.Grpc) move over unchanged.
- **New package**: `OutWit.Communication.Serializers.GoogleProtobuf` -- `MessageSerializerGoogleProtobuf` and `WithGoogleProtobuf()`. protoc-generated `IMessage` parameters and results travel as protobuf wire bytes, exactly as gRPC would send them; everything else in a signature (primitives, `Guid`, enums, plain DTOs) goes through a fallback serializer, JSON by default (`WithGoogleProtobuf(IMessageSerializer fallback)` to choose another). This closes the gap for proto-first gRPC migrations, which protobuf-net could never read.
- `ISerializationOptions` in the core: the one property (`ParametersSerializer`) a serializer plugin needs, implemented by both builder options, so a plugin ships a single `WithX<TOptions>()` extension and references neither the client nor the server package.

### Changed

- **Breaking (package split)**: `WithMessagePack()` / `WithProtoBuf()` and the two serializer classes left `OutWit.Communication` / `.Client` / `.Server`. Migration: add the matching `OutWit.Communication.Serializers.*` package on both ends and a `using OutWit.Communication.Serializers.MessagePack;` (or `.ProtoBuf`) line; call sites stay as they were. The 2.4.0 DynamicProxy split followed the same shape.
- **The message envelope is MemoryPack-only.** WitRPC's own wire models (`WitMessage`, `WitRequest`/`WitResponse`, the handshake pairs, `ParameterType`, `DiscoveryMessage`, `HostInfo`) no longer carry `[MessagePackObject]`/`[ProtoContract]` annotations; `[DataContract]` stays. Consequently the `DiscoveryClientOptions.WithMessagePack()` / `WithProtoBuf()` overloads are gone (discovery datagrams are WitRPC's own messages, not yours) and `WithMessageSerializer(...)` can no longer be pointed at MessagePack or protobuf-net. Payload serialization -- the thing the plugins are for -- is unaffected: your models are serialized by your serializer and travel inside the envelope as bytes.
- `OutWit.Communication` no longer depends on `OutWit.Common.MessagePack` and `OutWit.Common.ProtoBuf`.

## [3.0.0] - 2026-08-29

> **Highlighted: protocol 3 — a coordinated hardening major.** Every package ships as **3.0.0** (including `Client.Blazor`, previously 1.0.x, and `InterProcess.*`, previously 2.3.x). The wire format changed, so 3.0 clients require 3.0 servers; from 3.0 onward the server refuses a mismatched client with a readable version message, and payload models are version-tolerant so 3.x can evolve without another major. The full stage-by-stage record lives in [ROADMAP-v3.md](ROADMAP-v3.md).

### Breaking

- **Wire format** (`WitRequest`/`WitResponse` and the handshake): new fields `InvocationId`, `ContractId`, `MethodId`, `ProtocolVersion`, `ErrorMessage`; all payload models are now `[MemoryPackable(GenerateType.VersionTolerant)]`. A 2.x peer cannot talk to a 3.0 peer — update both ends in one wave.
- **Encryption**: AES-CBC replaced by authenticated **AES-256-GCM** (separate keys per direction via HKDF-SHA256, strictly ordered frame counters, AAD = protocol version + direction). Tampering, replay, reordering, or a dropped frame now throw `WitExceptionEncryption` instead of silently producing garbage. Standard, BouncyCastle, and Blazor Web Crypto encryptors moved together and interoperate within their pairs. Benchmarked 4.5–6× faster than the CBC path it replaces.
- **Retry semantics** (`RetryOptions`, Blazor `ChannelRetryOptions`): `InternalServerError` (a service fault) is **no longer retried by default**; retries cover the new client-local statuses `Timeout` (408) and `TransportError` (503) and apply **only to methods declared idempotent** (`MarkIdempotent(...)` / `IdempotentMethods`, or the explicit `RetryAllMethods` escape hatch). With no declarations the policy is inert.
- **Concurrency contract**: service methods are invoked concurrently across connections (since the Stage 2 rework) and must be thread-safe; set `MaxConcurrentRequests = 1` on the server to restore global serialization.
- **`HostManager` constructor** (`OutWit.InterProcess.Host`): now takes a `Func<WitClientBuilderOptions>` factory instead of a single options instance — the old shape handed every agent the *same* transport and address, so a second agent landed on the first one's endpoint.
- **MMF channel layout**: one file split into two directional regions with an atomic handoff; both ends must run 3.0. Public API unchanged (`WithMemoryMappedFile(name[, size])`).
- **REST**: rebuilt on a written contract. `POST {base}/{Method}` with the whole `WitRequest` as the JSON body (`GET` for parameterless methods), `Authorization: Bearer`, the body always a `WitResponse` (also on non-2xx), HTTP status mapped from it. The old REST-only request/processor/exception types were deleted; the `OutWit.Common.Rest` dependency dropped from the client. REST with parameters works now (it never did end-to-end before 3.0).

### Added

- **Protocol version handshake**: `WitProtocol.VERSION = 3`; the server refuses older/unreadable initializations with a logged, readable reason (encrypted for the client when it offered a key) instead of a decode failure.
- **Contract-scoped dispatch**: deterministic FNV-1a contract/method ids computed from namespace-qualified names (no assembly identity — linked-source contracts across different assemblies dispatch correctly). One dictionary lookup per call; parameters deserialize against the method's **declared** types, eliminating per-call reflection scans and `Type.GetType` on wire-supplied names from the fast path. Two services with identical method signatures on one channel no longer collide, and callbacks are filtered by contract on the client.
- **Invocation de-duplication**: `WitRequest.InvocationId` stays stable across retries; the server answers duplicates from a bounded per-connection cache (64 entries, ≤256 KB each) without re-executing the method.
- **Honest statuses**: `Timeout = 408` and `TransportError = 503` (client-local) split from `InternalServerError` (service fault); REST maps them to HTTP statuses.
- **Framing and lifecycle hardening**: length prefixes read fully and validated before allocation; `MaxMessageSize` per transport (256 MB default); handshake timeout (30 s default); idempotent `Dispose` with `Disconnected` raised exactly once; callbacks delivered only to authorized connections; failed auth closes the transport.
- **MMF rework**: lossless one-to-one channel (two directional regions, chunking, ready/free event pairs, peer death detected through an abandoned mutex — no heartbeats), kernel objects in the session-local `Local\` namespace.
- **InterProcess hardening**: graceful `HostAgent.Stop()` (disconnect → bounded wait → `Kill(entireProcessTree: true)`), one cleanup path for every exit route, `Disposed` raised exactly once, a synchronized `HostManager` registry with no finalizer, and real-process integration tests replacing the stub suite.
- **In-flight requests fault on disconnect**: a dropped connection promptly fails pending calls with `TransportError` instead of hanging them forever.
- **CI gate** (`.github/workflows/ci.yml`): own-code warnings as errors, tests on every TFM, the NativeAOT smoke does a real round-trip against a live server, and publishing is blocked unless CI is green.
- **NativeAOT-ready wire path**: the round-trip smoke immediately caught three AOT breaks and 3.0 fixes them all — the encryption handshake serialized RSA keys through reflection-based `System.Text.Json` (now hand-written JSON, wire-identical, JWK aliases accepted); MemoryPack discovered wire-model formatters via reflection that trimming removed (now registered explicitly at assembly load, trim-safe); and proxy type-name literals carried reference-facade assembly names NativeAOT cannot resolve (fixed by `OutWit.Common.Proxy` 1.2.11 + `OutWit.Common.Proxy.Generator` 2.2.2, now the pinned minimums). An AOT-published client completes an encrypted round-trip against a live server — proven in CI on every run.

### Fixed

- A throwing service method no longer tears down the connection (it becomes an `InternalServerError` response; all transports).
- A latent frame-ordering bug: every client transport (and the MMF server transport) raised inbound frames via fire-and-forget `Task.Run`, so an event could overtake the response it preceded. Frames now arrive synchronously in read order; the client processes them through a single inbound channel and dispatches user event handlers off the loop.
- Client authorization now honours the connect timeout.
- The Blazor Web Crypto encryptor advances its send counter only after successful JS encryption.
- REST listener survives unexpected exceptions, handles requests concurrently under a limit, and enforces `MaxBodyBytes` (64 MB default → 413).

### Known issues

- WebSocket server restart still hangs an already-connected client (pre-existing on 2.x: `HttpListener.Close()` aborts live connections). Documented in ROADMAP-v3.md "Known defects"; fix planned for a 3.x lifecycle pass.

## [2.4.1] - 2026-08-13

Documentation-only wave: no code changes. The package READMEs (shown on nuget.org) now document the 2.4.0 dynamic proxy split — the parameterless `GetService<T>()` requires the opt-in `OutWit.Communication.Client.DynamicProxy` package, and the source-generated (`[ProxyTarget]`) path needs no extra package.

Package versions after this release: `OutWit.Communication`, `Client.Pipes`, `Client.Tcp`, `Client.WebSocket`, `Client.MMF`, `Client.Rest`, `Server` 2.4.1; `Server.DependencyInjection` 2.3.11. (`Client` and `Client.DynamicProxy` shipped correct READMEs in 2.4.0 and are not republished; server transports have no client-side samples and stay 2.4.0.)

### Fixed

- Proxy-usage note added to the READMEs of the core package, all five client transports, the server package, and the server DI extension.
- `Client.Rest` README rewritten: it documented a non-existent API (`options.WithRest(...)` + `GetService<T>()` on a builder-produced client). The real surface is `new WitClientRest(RestClientTransportOptions, IAccessTokenProvider)` with a proxy built over `RequestInterceptor` (static) or `RequestInterceptorDynamic` (dynamic, opt-in package). This predates 2.4.0.

## [2.4.0] - 2026-08-13

> **Highlighted: dynamic proxy package split.** Runtime dynamic proxy support (Castle.Core) moved out of the core packages into the new opt-in `OutWit.Communication.Client.DynamicProxy` package. `OutWit.Communication`, `OutWit.Communication.Client`, and `OutWit.Communication.Server` no longer depend on Castle.Core, and the client core publishes cleanly under NativeAOT with source-generated proxies.

Package versions after this release: `OutWit.Communication` + client/server packages and non-MMF transports 2.4.0, `Client.DependencyInjection` 2.4.0, `Client.DynamicProxy` 2.4.0 (new), `Client.Blazor` 1.0.9, `OutWit.InterProcess.Host` 2.3.3. Unchanged: MMF transports 2.4.0 (published earlier), `Client.HealthChecks` 2.3.5, `Server.DependencyInjection` 2.3.10, `Encryption.BouncyCastle` 2.3.4/2.3.5, `OutWit.InterProcess`/`.Agent` 2.3.2. `Client.Blazor` 1.0.9, `Client.DependencyInjection` 2.4.0, and `InterProcess.Host` 2.3.3 are mandatory updates when moving to client 2.4.0 — their published predecessors bind to the relocated dynamic `GetService` and would fail at runtime against the new client.

### Added

- **New package**: `OutWit.Communication.Client.DynamicProxy` (2.4.0) — carries `RequestInterceptorDynamic`, the Castle `IInvocation` adapter (`ProxyUtils`), and the relocated dynamic `GetService<TService>()` extension. Namespaces are unchanged (`OutWit.Communication.Interceptors`, `OutWit.Communication.Utils`, `OutWit.Communication.Client`), so existing call sites recompile after adding a package reference.
- **New project**: `OutWit.Communication.Client.AotSmoke` — non-packable NativeAOT publish gate exercising the static-proxy path (`[ProxyTarget]` + `OutWit.Common.Proxy.Generator`); an MSBuild guard fails the build if any `Castle.*` assembly (or the DynamicProxy package itself) appears in its reference closure.
- New test suite `DynamicProxySplitTests` pinning the split at assembly-metadata level: core assemblies reference no Castle, the moved types live in the new package, the relocated extension keeps the `OutWit.Communication.Client` namespace and `WitClient` receiver.

### Changed

- **Breaking (binary, dynamic path only)**: `GetService<TService>(this WitClient, bool strongAssemblyMatch = true)` moved from the `OutWit.Communication.Client` assembly to `OutWit.Communication.Client.DynamicProxy`. Migration: add a reference to the new package; no code changes. The static-proxy overload is untouched and stays in the core client. No type forwarding is provided by design — forwarding would keep Castle.Core in the core.
- `Castle.Core` PackageReference removed from `OutWit.Communication`; dead `Castle.Components.DictionaryAdapter.Xml` usings removed from `WitServer`, `EncryptorServerGeneral`, and the BasicHost example.
- `OutWit.Communication.Client.Blazor`, `OutWit.Communication.Client.DependencyInjection`, and `OutWit.InterProcess.Host` now reference `OutWit.Communication.Client.DynamicProxy` (they sit on the dynamic path; behavior unchanged). Examples using the dynamic overload reference it as well.
- `ParameterType`'s name-based `Type` resolution now carries an `UnconditionalSuppressMessage(IL2057)` with a documented justification — it was the only WitRPC-owned AOT analysis warning on the client path.

### Known AOT gaps (upstream)

The AOT smoke publish succeeds and the WitRPC assemblies are AOT-warning-free, but the dependency graph is not yet: MemoryPack's non-source-gen fallback paths (~19 diagnostics) and the `OutWit.Common`/`OutWit.Common.Json`/`OutWit.Common.MemoryPack` reflection helpers (~16) still emit IL2xxx/IL3050 analysis warnings. `TreatWarningsAsErrors` in the smoke project stays off until those are annotated upstream; the closure guard and the run-the-binary check are the hard gates meanwhile.

## [2.3.3] - 2026-04-23

Package versions after this release: `OutWit.Communication` + client packages 2.3.3, server packages 2.3.4, `Client.HealthChecks` 2.3.4, `Client.DependencyInjection` 2.3.7, `Server.DependencyInjection` 2.3.9, `Client.Blazor` 1.0.6. (`OutWit.InterProcess.*` remain 2.3.1.)

### Fixed

- **Empty response crash** (commit `cb4a68d`): `WitClient` no longer crashes on empty or malformed incoming payloads; WebSocket client and server transports skip empty frames.
- New regression test suite `WitClientIncomingPayloadTests` covering empty/garbage incoming payload handling.

## [2.3.2] - 2026-04-22

> **Highlighted: transport restart lifecycle fix.** Server transports previously could not be stopped and restarted on the same endpoint — `WebSocketServerTransportFactory` and `TcpServerTransportFactoryBase` created their listener in the constructor and never released it in `StopWaitingForConnection()`, and `WitServer.Dispose()` never disposed the transport factory. This broke dynamic per-client server recreation after a host restart (observed downstream in OutWit.Cloud).

Package versions after this release: core/client packages 2.3.2, server packages 2.3.3, `Client.DependencyInjection` 2.3.6, `Server.DependencyInjection` 2.3.8, `Client.Blazor` 1.0.5.

### Changed

- **Breaking (interface)**: `ITransportServerFactory` now extends `IDisposable` (commit `8b8501c`); custom transport factories must implement `Dispose()`.

### Fixed

- `WitServer.Dispose()` now stops and disposes its transport factory.
- Restart-safe listener lifecycle for WebSocket (`HttpListener`), TCP (`TcpListener`), Named Pipes, and Memory-Mapped Files server transport factories: `start -> stop/dispose -> recreate -> start` on the same endpoint/port/name now works.
- Listener bind now happens synchronously in `StartWaitingForConnection()` — bind failures surface immediately instead of being swallowed in a background accept loop.
- New regression test suite `TransportFactoryLifecycleTests` (restart cycles for all server transports) plus a server-level test verifying `WitServer.Dispose()` disposes the factory.

Known remaining gap: the REST server (`WitServerRest`) lifecycle was not reworked in this pass.

## [Server 2.3.2] - 2026-03-20

Server packages only: 2.3.1 -> 2.3.2 (`Server.DependencyInjection` -> 2.3.7).

### Fixed

- Hardened `WitServer` against stale frames arriving after a client disconnect (commit `173744a`).
- New regression test suite `WitServerTransportEdgeCaseTests`.

## [Client.Blazor 1.0.1-1.0.4] - 2026-02-10 to 2026-02-20

### Added

- **New Package**: `OutWit.Communication.Client.Blazor` (1.0.1, commit `8b8530e`) — Blazor WebAssembly channel factory for WitRPC over WebSocket with RSA/AES encryption via the Web Crypto API.
- 1.0.2 (`654db14`): custom URL option.
- 1.0.4 (`2edbb8b`, 2026-02-20): configurable `BufferSize` option.

### Fixed

- 1.0.3 (`a12e3fb`, 2026-02-14): crypto interop fix.

## [DependencyInjection 2.3.2-2.3.6] - 2026-02-06

`Client.DependencyInjection` 2.3.1 -> 2.3.5, `Server.DependencyInjection` 2.3.1 -> 2.3.6; other packages unchanged.

### Changed

- Dependency Injection refactoring (`f7b8aab`), additional DI extension methods (`fd21284`), redundant overload removed (`4b582a2`), DI usage simplified (`833a08f`).

### Fixed

- Server DI service resolution fix (`a8a4408`).

## [2.3.1] - 2026-01-25

All packages 2.3.0 -> 2.3.1 (including `OutWit.InterProcess.*`, which remain at 2.3.1).

### Changed

- **License**: project relicensed from the Non-Commercial License (NCL) to **Apache License 2.0**; `NOTICE` files added to all packages (commit `0842296`).
- Dependencies updated.

## [2.3.0] - 2025-12-09

### Added

#### BouncyCastle Cross-Platform Encryption
- **New Package**: `OutWit.Communication.Client.Encryption.BouncyCastle` - Cross-platform encryption client using BouncyCastle cryptography library
- **New Package**: `OutWit.Communication.Server.Encryption.BouncyCastle` - Cross-platform encryption server using BouncyCastle cryptography library
- Pure C# implementation that works everywhere, including **Blazor WebAssembly** without JavaScript interop
- RSA-OAEP (SHA-256) for key exchange, AES-256-CBC for symmetric encryption
- Extension methods `WithBouncyCastleEncryption()` for easy configuration

```csharp
// Client (works in Blazor WebAssembly!)
var client = WitClientBuilder.Build(options =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Cross-platform encryption
});

// Server
var server = WitServerBuilder.Build(options =>
{
    options.WithWebSocket("http://localhost:5000", maxClients: 100);
    options.WithJson();
    options.WithBouncyCastleEncryption();  // Must match client
    options.WithService(myService);
});
```

> **Important**: BouncyCastle encryption is NOT compatible with standard .NET encryption. Both client and server must use `WithBouncyCastleEncryption()`.

#### Composite Services (Multiple Interfaces per Server)
- **New Class**: `CompositeRequestProcessor` - Request processor that handles multiple service interfaces
- **New Builder**: `CompositeServiceBuilder` - Fluent builder for registering multiple services
- Allows clients to request proxies for different interfaces from a single server connection
- Eliminates the need to create "super-interfaces" that inherit from multiple service interfaces

```csharp
// Server: Register multiple services
var server = WitServerBuilder.Build(options =>
{
    options.WithServices()
        .AddService<IUserService>(new UserService())
        .AddService<IOrderService>(new OrderService())
        .AddService<INotificationService>(new NotificationService())
        .Build();
    
    options.WithTcp(5000, maxClients: 100);
    options.WithJson();
});

// Client: Access any registered service
var userService = client.GetService<IUserService>();
var orderService = client.GetService<IOrderService>();
var notificationService = client.GetService<INotificationService>();
```

#### Composite Services with Dependency Injection
- **New Method**: `AddWitRpcServerWithServices()` - Register composite services with automatic DI resolution
- **New Class**: `CompositeServiceRegistration` - Helper for configuring composite services in DI context
- Services can be resolved from DI container or registered inline
- Three registration patterns: pre-registered services, inline registration, and factory functions

```csharp
// Option 1: Services already registered in DI
services.AddSingleton<IUserService, UserServiceImpl>();
services.AddSingleton<IOrderService, OrderServiceImpl>();

services.AddWitRpcServerWithServices("api-server",
    options =>
    {
        options.WithTcp(5000, maxClients: 100);
        options.WithJson();
    },
    svcs =>
    {
        svcs.AddService<IUserService>();    // Resolved from DI
        svcs.AddService<IOrderService>();   // Resolved from DI
    });

// Option 2: Register and add in one step
services.AddWitRpcServerWithServices("api-server",
    options => { /* ... */ },
    svcs =>
    {
        svcs.AddService<IUserService, UserServiceImpl>();
        svcs.AddService<IOrderService, OrderServiceImpl>();
    });

// Option 3: Factory functions
services.AddWitRpcServerWithServices("api-server",
    options => { /* ... */ },
    svcs =>
    {
        svcs.AddService<IUserService>(sp => new UserServiceImpl(
            sp.GetRequiredService<ILogger<UserServiceImpl>>()));
    });
```

#### Dependency Injection Integration
- **New Package**: `OutWit.Communication.Client.DependencyInjection` - Microsoft.Extensions.DependencyInjection support for WitRPC clients
- **New Package**: `OutWit.Communication.Server.DependencyInjection` - Microsoft.Extensions.DependencyInjection support for WitRPC servers
- Seamless integration with ASP.NET Core and other DI-based applications
- Extension methods for `IServiceCollection`

```csharp
// Register WitRPC client with DI
services.AddWitRpcClient("my-client", (options, sp) =>
{
    options.WithWebSocket("ws://localhost:5000");
    options.WithJson();
    options.WithEncryption();
});

// Inject and use
public class MyController
{
    private readonly IWitClient _client;
    
    public MyController(IWitClientFactory factory)
    {
        _client = factory.GetClient("my-client");
    }
}
```

#### Auto-Reconnection
- **New Class**: `ReconnectionOptions` - Configurable reconnection settings
- Automatic reconnection with exponential backoff
- Configurable max attempts, initial delay, max delay, and jitter
- Extension method `WithAutoReconnect()` for easy configuration

```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithTcp("localhost", 5000);
    options.WithJson();
    options.WithAutoReconnect(reconnect =>
    {
        reconnect.MaxAttempts = 5;
        reconnect.InitialDelay = TimeSpan.FromSeconds(1);
        reconnect.MaxDelay = TimeSpan.FromSeconds(30);
        reconnect.UseJitter = true;
    });
});
```

#### Retry Policy / Resilience
- **New Class**: `RetryPolicy` - Configurable retry settings for failed RPC calls
- Support for fixed and exponential backoff strategies
- Configurable retry conditions (which exceptions to retry)
- Extension method `WithRetryPolicy()` for easy configuration

```csharp
var client = WitClientBuilder.Build(options =>
{
    options.WithTcp("localhost", 5000);
    options.WithJson();
    options.WithRetryPolicy(retry =>
    {
        retry.MaxRetries = 3;
        retry.InitialDelay = TimeSpan.FromMilliseconds(100);
        retry.BackoffMultiplier = 2.0;
        retry.MaxDelay = TimeSpan.FromSeconds(5);
    });
});
```

#### Health Checks
- **New Package**: `OutWit.Communication.Client.HealthChecks` - ASP.NET Core Health Checks support
- Monitor WitRPC client connection status
- Integration with standard ASP.NET Core health check infrastructure

```csharp
// Register health check
services.AddHealthChecks()
    .AddWitRpcClientCheck("my-client", tags: new[] { "rpc", "ready" });

// Use in ASP.NET Core
app.MapHealthChecks("/health");
```

#### Server Encryption Interface Enhancement
- Added `EncryptForClient(byte[] data, byte[] clientPublicKey)` method to `IEncryptorServer` interface
- Enables server-side encryption implementations to encrypt data using client's public key
- Required for BouncyCastle encryption support

#### CI/CD
- **New Workflow**: `publish-package.yml` - GitHub Action for publishing individual NuGet packages
- **New Workflow**: `publish-all-packages.yml` - GitHub Action for publishing all packages in parallel
- Supports publishing to both nuget.org and GitHub Packages
- Includes all Communication and InterProcess packages

#### Test Infrastructure
- Added `[assembly: Parallelizable(ParallelScope.None)]` to disable parallel test execution
- Prevents port/resource conflicts when running tests in bulk
- New test interfaces and implementations for composite service testing
- Integration tests for BouncyCastle encryption and composite services

### Changed

#### Documentation
- Updated `README.md` for `OutWit.Communication.Server` with composite services documentation
- Updated `README.md` for `OutWit.Communication.Client` with multiple service access documentation
- Updated `README.md` for `OutWit.Communication.Server.DependencyInjection` with composite services DI documentation
- Created comprehensive README files for BouncyCastle encryption packages with compatibility tables
- Updated main `README.md` with full package list, badges, and advanced features documentation
- Created `CHANGELOG.md` for tracking version history

### Fixed

- Improved test stability when running multiple tests sequentially

---

## [2.2.0] - 2025-11-14

- .NET 10 target framework support (commit `68e9601`).

*See repository history for earlier changes.*

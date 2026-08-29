# WitRPC 3.0 — hardening roadmap

Status: **in progress** (branch `v3`). Started 2026-08-28.

Source: an independent audit of the 2.4.x line (local working document,
`@Roadmap/README.md`, not tracked), verified claim by claim against the code and
against every consumer in the OutWit workspace. Every P0/P1 finding was
confirmed. What changed after verification is the *ranking* and the *shape* of
the fixes, which is what this document records.

## Decisions that shape the plan

1. **One major, not two minors.** Several fixes need new wire fields
   (`ContractId`, `InvocationId`, AEAD framing). `WitMessage`/`WitRequest` are
   plain `[MemoryPackable]` with no version tolerance and MemoryPack is the
   default message serializer, so *any* new field breaks every older client.
   Rather than a no-wire minor followed by a wire major, everything ships as
   3.0. Stages are still ordered so that no-wire work lands first and the wire
   change is a single, late, deliberate step.
2. **MMF is one-to-one by design.** It is the primary transport for local
   inter-process links (host ↔ agent). Multi-client MMF is out of scope; the
   one-to-one contract becomes explicit in the options and the docs.
3. **MMF goes first.** It is needed now, its defects are transport-local, and it
   can be verified at the `ITransport` level without waiting for the core rework.
4. **Every stage closes with a test that gates it.** The existing suite cannot
   yet serve as a gate (intermittent hangs on Pipes and MMF). Stage 0 adds the
   harness the later stages are measured with; the hangs themselves are product
   bugs fixed in stages 1–3.
5. **No silent caps.** Where a limit is introduced (frame size, queue depth,
   handshake time) it is an option with a documented default, and exceeding it
   closes the connection with a logged reason — never a quiet drop.

## Where the real risk is (ranked, after verification)

| Rank | Finding | Why it ranks here |
|---|---|---|
| 1 | Global `SemaphoreSlim(1,1)` serialises every request and callback across all connections; the callback path has no `try/finally` and a timed `Wait` releases the lock mid-send | One slow call stalls every client; one serializer throw freezes the server for good |
| 2 | MMF: one slot at offset 0 for both directions, `AutoResetEvent` with no read-ack | Frames overwrite each other; the audit's "0 instead of 1" callback failures are exactly this |
| 3 | Callbacks are broadcast to unauthorised connections; failed auth does not close the transport | Pre-auth exposure of events on public endpoints |
| 4 | No frame-size limit, no handshake timeout; TCP/Pipes read the 4-byte prefix with one `ReadAsync` and never validate it | Pre-auth DoS: unbounded `MemoryStream` on WebSocket, slot exhaustion by idle sockets |
| 5 | Client registers its `TaskCompletionSource` *after* `SendBytesAsync`; a late response poisons the next request | Rare but cascading |
| 6 | `InternalServerError` is returned for both client timeouts and server application exceptions, and Blazor `ChannelFactoryOptions.Retry` is on by default | Business exceptions are retried 3× with backoff; timed-out commands re-execute |
| 7 | `CompositeRequestProcessor` routes by name + parameter types only; first registration wins | Confirmed collision in a consumer (`CancelJobAsync(Guid)` on two channels) |
| 8 | REST works end-to-end only for parameterless methods | GET drops `byte[]` params, POST base64-encodes them, the builder wires the wrong processor, the client throws on any non-2xx, the listener loop is sequential and dies on unexpected exceptions |
| 9 | AES-CBC with a static per-connection IV and no MAC | Real, but every deployment runs it under TLS; the README's "end-to-end encryption" claim is the immediate problem |
| 10 | InterProcess: unsynchronised registry, finalizer calling `Dispose`, `Kill()` with no graceful wait, stub tests | Moderate; used for local IPC |

Not in the audit, found during verification: `Type` names from the wire are
resolved (`Type.GetType`) *before* the token check; `ErrorDetails` carries
`innerException.Message` to clients; the static token compare is `==`.

## Stages

### Stage 0 — Baseline and transport conformance harness

- Record a baseline run of the Communication suite (`net10.0-windows`), with
  `--blame-hang-timeout`, so "before" is a number and not a memory.
- Add `Transports/TransportConformanceTests`: the same contract for every
  transport pair at the `ITransportClient`/`ITransportServerFactory` level, no
  `WitServer` involved — echo, ordering, concurrent both-direction traffic,
  frames larger than any internal buffer, client disconnect seen by the server,
  server stop seen by the client, stop → start on the same name/port, and every
  wait bounded.

Done when: the harness runs green for Pipes, TCP, TCP/TLS, WebSocket, and red
for MMF in exactly the ways stage 1 is about to fix.

### Stage 1 — MMF rework (one-to-one, lossless)

Layout: one `MemoryMappedFile` of `Size` bytes split into two equal regions,
client→server and server→client. Each region carries a fixed header
(`length`, `total`, `offset`, `flags`) and a payload; messages larger than a
region are chunked. Per direction two auto-reset events, `ready` and `free`:
the writer waits `free`, writes, sets `ready`; the reader waits `ready`, copies,
sets `free`. Nothing can be overwritten and no signal can be lost.

Presence: each side holds a named mutex on a dedicated thread for the life of
the transport. The peer includes it in its wait set; when the owner exits (or
the process dies) the mutex is abandoned and the peer sees the disconnect
without heartbeats. A `hello` frame arms it after connect; a `bye` frame is the
graceful marker.

Also: consistent `Local\` namespace for every kernel object (today the events
are `Global\` and the file is session-local, which cannot work cross-session
anyway); `MemoryMappedViewAccessor` with explicit offsets instead of a shared
stream position; `Size` validated; `NewClientConnected` fires when a client is
actually attached, not when a slot is published; `CanReinitialize` becomes
`false` like every other transport because a departed client always gets a
fresh transport.

Public API unchanged: `WithMemoryMappedFile(name)`, `WithMemoryMappedFile(name,
size)`, `MemoryMappedFileServerTransportOptions { Name, Size }`,
`MemoryMappedFileClientTransportOptions { Name }`. The file layout changes, so
both ends must run 3.0 — acceptable for a local link.

Done when: conformance + a stress test (requests and callbacks concurrently,
repeated connect/disconnect, peer death simulated by an abandoned mutex) pass
in a loop; the existing MMF fixtures pass; nothing hangs.

### Stage 2 — Core concurrency and the authorisation boundary

`WitClient`: `ConcurrentDictionary<Guid, PendingRequest>` registered *before*
send, `RunContinuationsAsynchronously`, removal in `finally`; the single-flight
semaphore goes, several RPCs may be in flight; late responses are dropped and
logged, never matched to the wrong waiter.

`WitServer`: per-connection inbound queue processed in order, a configurable
server-wide concurrency limit, one async send lock per connection so responses
and callbacks never interleave on the transport; the callback path becomes
async with `try/finally`; token validation moves *before* request parsing;
callbacks go only to `IsAuthorized` connections; a failed authorisation closes
the transport; connection state is an explicit `Connected → Initialized →
Authorized` machine and out-of-order handshake messages are rejected.

Done when: 100 parallel clients are not blocked by one slow method; a throwing
callback serializer does not freeze the server; negative tests for wrong,
missing and expired tokens pass on every transport.

### Stage 3 — Framing, limits and lifecycle (all transports)

`MaxMessageSize` option on every transport with a documented default;
`ReadExactly` for length prefixes; length validated before allocation;
handshake timeout (a connection that has not authorised within it is closed);
bounded per-connection inbound queue; `AuthenticateAsServerAsync` off the
accept loop; the Pipes factory's client set synchronised; `Dispose` idempotent
and `Disconnected` raised exactly once; `IAsyncDisposable` where shutdown is
asynchronous.

Done when: an oversized or truncated frame closes the connection without a
large allocation; fuzzed prefixes and fragmented WebSocket messages never OOM;
the lifecycle contract has one test per transport.

### Stage 4 — Protocol 3 (the wire change)

- `WitRequestInitialization.ProtocolVersion`; the server refuses older clients
  with a clear error rather than a decode failure.
- `ContractId`, `MethodId`, `EventId` generated by the source generator and
  checked at `Register` — colliding contracts fail at start-up.
- `InvocationId` stable across retry attempts; bounded server-side
  de-duplication; retry restricted to methods marked idempotent, off by default
  for commands.
- Status split: `Timeout` / `TransportError` (client-local, retryable) vs
  `InternalServerError` (service fault, not retryable by default).
- AEAD instead of CBC: AES-GCM (or ChaCha20-Poly1305 under BouncyCastle),
  separate keys per direction, nonce = session prefix + sequence counter,
  associated data = protocol version + direction + message kind + message id;
  replay window enforced. General, BouncyCastle and Web Crypto encryptors move
  together.
- `[MemoryPackable(GenerateType.VersionTolerant)]` on wire models so 3.x can
  evolve without another major.

Done when: tampering, replay and reordering are rejected by tests; a 2.x client
against a 3.0 server fails with the version message; every consumer in the
workspace (WitCloud SDK, Blazor channel factory) is updated in the same wave.

### Stage 5 — REST rebuilt on a written contract

`POST {base}/{Method}` with a JSON body of named parameters (positional array
accepted); `GET` only for simple types; `Authorization: Bearer`; the body is
always `WitResponse`, HTTP status mapped (200/400/401/408/413/500), and the
client reads the body on non-2xx instead of throwing. Parameters travel as raw
JSON (the client's `ParametersSerializer` already produces JSON — it must not be
base64-wrapped). `RequestProcessorRest` becomes the only REST processor, with
the `Task<T>` path fixed and resolution by name + parameter count; the listener
handles requests concurrently under a limit, survives unexpected exceptions,
enforces `MaxBodyBytes` and applies the configured timeout. Events over REST
are documented as unsupported.

Done when: a `CommunicationTestsRest` fixture mirrors the transport fixtures
(sync/async/void/`Task<T>`/null parameters/errors/auth/limits) and passes.

### Stage 6 — InterProcess hardening

Synchronised agent registry; graceful shutdown (disconnect → bounded wait →
`Kill(entireProcessTree: true)`); `WaitForExit`, `Process.Dispose`,
`WitClient.Dispose`; event handlers removed; no finalizer; integration tests
that spawn a real agent process and cover start, crash, reconnect and cleanup.

### Stage 7 — Release 3.0.0

All packages to 3.0.0; CHANGELOG; package READMEs corrected (encryption stated
as defence in depth under TLS, MMF stated as one-to-one, REST contract
documented); CI gate: own-code warnings as errors, tests on every TFM, transport
integration tests serialised with unique names/ports, NativeAOT smoke doing a
real round-trip, publish blocked on any red job.

## Out of scope for 3.0

Multi-client MMF; typed REST endpoints; server streaming
(`IAsyncEnumerable<T>`); `ValueTask` surface; a sequential dispatcher for user
event handlers (today two callbacks may invoke handlers out of order relative
to each other — same as 2.x; decryption and response correlation are ordered); capability negotiation beyond the
protocol version check. These are features, not hardening.

## Progress

- [x] Stage 0 — `Transports/TransportConformanceTests` in place: one
  `ITransport`-level contract for every transport pair, plus MMF-specific tests.
- [x] Stage 1 — MMF reworked (one-to-one, lossless, atomic handoff). All 11
  conformance tests pass repeatedly; the full end-to-end WitServer/WitClient
  MMF suite is 84/84 across serializers, static/dynamic proxy, reconnect,
  one-to-one and the callback tests the audit reported as hanging. Commits on
  `v3`: `e2b60a3` (rework + harness), `fc78f64` (ack-after-registration fix).
- [x] Stage 2 — core concurrency and auth boundary. Per-connection inbound queue
  + processing loop, per-connection send lock, server-wide concurrency cap
  (`MaxConcurrentRequests`, unbounded default), explicit handshake state machine,
  callbacks only to authorized connections, failed auth closes the connection,
  async callback path with `try/finally`. Client gained a pending-request map
  (register-before-send, multiplexing). Pulled forward from Stage 3: the
  transport init-race fix (`TransportInboundBuffer` on Pipes/TCP/WebSocket
  server transports) — without it, a fast client's first frame was dropped
  before the server subscribed, which is what made multi-client flaky. Commit on
  `v3`: `1f952b0`. Deferred to Stage 4 (needs the wire): cancellation/deadline
  propagation. **Behaviour change for consumers:** service methods are now
  invoked concurrently across connections and must be thread-safe; set
  `MaxConcurrentRequests = 1` to restore global serialization.
- [x] Stage 3 — framing, limits, lifecycle. Commit on `v3`: `63ab272`.
  - Framing safety (P0-3): `StreamFrameReader` (read the length in full, reject a
    non-positive or over-`MaxMessageSize` length before allocating, read the
    payload to completion) on TCP and Pipes; WebSocket caps its fragment
    accumulation the same way. `MaxMessageSize` per transport, 256 MB default.
  - Handshake timeout (30 s default, configurable): a client that connects and
    never authorizes is closed rather than holding a slot.
  - Idempotent `Dispose` on every stream transport (`Disconnected` raised once).
  - Pipes factory client-set synchronized; a slot released only for a tracked
    client (closed a slot leak).
  - Deferred with reason: per-connection outbound send lock inside the stream
    transports (the WitServer/WitClient layer already serializes sends, so the
    conformance ConcurrentSends test stays MMF-only); async TLS handshake (TCP/TLS
    unused by the consumers); bounded inbound queue; `IAsyncDisposable`.
- [x] Stage 4 — protocol 3. First half (`b5e0b00`), contract ids (`6d5286f`),
  AEAD (`8499544`).
  - **Version tolerance.** Every payload wire model (`WitRequest`, `WitResponse`,
    the initialization/authorization pairs, `ParameterType`, `DiscoveryMessage`)
    is `[MemoryPackable(GenerateType.VersionTolerant)]` with explicit
    `[MemoryPackOrder]` mirroring the MessagePack `[Key]` numbering — 3.x can now
    add fields without another major. The `WitMessage` envelope (id, kind,
    payload) is deliberately **frozen** non-tolerant and documented as such: any
    build can read any other build's envelope and answer with a readable refusal;
    evolution happens inside payloads.
  - **Version handshake.** `WitProtocol.VERSION = 3`;
    `WitRequestInitialization.ProtocolVersion` (a pre-3.0 client reads as 0),
    `WitResponseInitialization.{ProtocolVersion, ErrorMessage}`. The server
    refuses a mismatched or unreadable initialization with a readable reason
    (encrypted for the client when it offered a key), logs it, and closes the
    connection; the client surfaces `ErrorMessage` instead of guessing from a
    null key. A 2.x client cannot read 3.0 bytes at all — it fails fast with the
    reason in the *server* log; the in-band message works from 3.0 onward.
  - **Honest statuses.** `Timeout = 408` and `TransportError = 503` — client-local
    outcomes — split from `InternalServerError` (a service fault). The client maps
    its own timeout → `Timeout`, send/receive/parse failures → `TransportError`
    (REST client likewise); `RetryOptions` retries `Timeout`/`TransportError` by
    default and **no longer retries `InternalServerError`** — re-running failed
    business logic is an explicit opt-in.
  - **In-flight requests fault on disconnect** (was a known defect): both
    `OnServerDisconnected` and `Dispose` complete pending requests with a
    transport exception, so a dropped connection fails calls promptly instead of
    hanging them forever.
  - **InvocationId + de-duplication + idempotent-only retry.**
    `WitRequest.InvocationId` is stamped once per logical call (in
    `CreateRequest` and defensively in `WitClient.SendRequest`) and stays stable
    across retry attempts. The server keeps a bounded per-connection cache
    (64 entries, ≤256 KB each) and answers a duplicate invocation from it without
    re-executing the method. Retry is restricted to methods the consumer declared
    via `RetryOptions.MarkIdempotent(...)` (or the explicit `RetryAllMethods`
    escape hatch) — a command never silently runs twice. Blazor's
    `ChannelRetryOptions` exposes the same knobs; its default retry is now inert
    until methods are declared.
  - **Contract ids (`6d5286f`).** `ContractIds`: deterministic FNV-1a ids from
    namespace-qualified names with no assembly identity. `WitRequest` gains
    `ContractId` + `MethodId`; the interceptor (now handed the contract type by
    both `GetService<T>` entry points) stamps them, and both processors dispatch
    by one dictionary lookup, deserializing parameters against the method's
    **declared** types — the per-call reflection scan and `Type.GetType` from
    wire strings are gone from the fast path. `CompositeRequestProcessor` routes
    contract-scoped: two services with the same method signature on one channel
    each get their own calls (audit finding 7 — regression-tested with the
    `CancelJob(Guid)` scenario). Callbacks are stamped with the raising
    contract's id and filtered on the client, so colliding event names across
    services no longer cross-deliver. Deviations, deliberate: **EventId** folded
    into ContractId + event name (names are unique within a contract);
    **generic methods** keep the name-based path (`MethodId = 0`) — their closed
    signatures differ per call (REST POST carries the ids in its JSON body, so
    the fast path applies there too; only parameterless GET is name-based); ids are computed at runtime, not by the source
    generator (`IProxyInvocation` exposes no `MethodInfo`; a generator version
    can come with an `OutWit.Common.Proxy` bump without wire changes).
  - **AEAD (`8499544`).** AES-256-GCM per direction: the handshake's
    `SymmetricKey`/`Vector` become a master key + HKDF salt (wire shape
    unchanged, interfaces unchanged); both ends derive one key per direction.
    Frame: `[counter:8][tag:16][ciphertext]`; nonce = the counter (keys are
    fresh per connection, which is what makes it unique); AAD = protocol version
    + direction; the receiver requires counters strictly in order, so replayed,
    reordered, dropped or tampered frames throw `WitExceptionEncryption` instead
    of the old CBC path's silent empty array. `EncryptorGeneral` (client+server)
    and BouncyCastle move together — BC keeps its web-format RSA handshake and
    shares the same `AeadCipher`, so the pairs interoperate; the Blazor Web
    encryptor does GCM through SubtleCrypto with counters and HKDF kept in
    managed code (the JS stays stateless). Benchmarked 4.5-6× faster than the
    CBC path it replaces. `AeadCipherTests`: round-trip, tamper, replay,
    reorder, cross-direction, and General client↔server interop.
    The strict counter also exposed (and forced the fix of) a latent bug: every
    client transport and the MMF server transport raised inbound frames via
    fire-and-forget `Task.Run` — unordered. Before, that occasionally delivered
    an event after the response it preceded (silent); under AEAD it dropped a
    frame and hung the caller (diagnosed from the hang dump: server counters
    3/2, client 2/2 — the response overtook the callback and failed the counter
    check). Frames now arrive synchronously in read order; `WitClient` mirrors
    the server with one inbound channel, decrypts in order, and dispatches user
    event handlers off the loop (a handler calling back into its own client
    must not deadlock the inbound processing). Sealing and sending share one
    lock; authorization now honours the connect timeout.
  - Done-when: tampering/replay/reordering rejected by tests ✓; a 2.x client
    against 3.0 fails with the version story ✓ (readable in-band from 3.0 on,
    server-log + fast failure for pre-3.0); consumers update in one wave —
    that's Stage 7's release work.
  - Consciously moved out of 3.0: cancellation/deadline propagation (deferred
    here from Stage 2). It is a feature, not hardening — and now that the wire
    is version-tolerant it can ship in any 3.x minor without a break, which
    removes the reason to rush it into the major.
- [x] Stage 5 — REST rebuilt. Commit on `v3`: `75bf391`.
  - **Client** (`WitClientRest`): one stateless HTTP call per request. A single
    process-wide `HttpClient` (its own timeout disabled; each call bounded by the
    options timeout through a linked token). The whole `WitRequest` is the JSON
    body of `POST {base}/{Method}` (or a `GET` for a parameterless method when
    the mode allows one); `Authorization: Bearer` from the token provider; the
    reply is always read back as a `WitResponse` from the body — including on a
    non-2xx status — so a server fault comes back as a response the proxy turns
    into a fault, not a thrown `HttpRequestException`.
  - **Server** (`WitServerRest`): `HttpListener` with an accept loop that never
    dies (each request handled off-loop so one slow or failing request neither
    blocks the next nor takes the listener down), a concurrency limit
    (`MaxConcurrentRequests`), a body-size cap (`MaxBodyBytes`, 64 MB default →
    413), the configured processing timeout, Bearer-token validation → 401, and
    an HTTP status mapped from the `WitResponse` (200/400/401/413/500).
  - **Design deviation from the plan, deliberate:** rather than a bespoke
    "named parameters" REST contract with its own `RequestProcessorRest`, the
    whole `WitRequest` is the JSON body run through the *same*
    `RequestProcessor<T>` every other transport uses (serializer reset to JSON).
    REST now behaves identically to the persistent transports — same resolution,
    same `Task<T>`/void/nullable handling — instead of carrying a parallel code
    path. The old REST-only request/processor/exception/util types were deleted,
    and the `OutWit.Common.Rest` dependency dropped from the client.
  - Events over REST remain unsupported (stateless request/reply, no callbacks) —
    documented in the type summaries.
  - Done-when met: `CommunicationTestsRest` mirrors the transport fixtures
    (sync / async `Task<T>` / void / async void / null parameters / null result /
    multiple nullable params / no-auth / wrong-token fault / GET-for-parameterless)
    — 10 tests, green on net8.0 and net10.0.
- [x] Stage 6 — InterProcess hardened. Commit on `v3`: `3a3a33f`.
  - **`HostAgent`**: `Stop()` is the graceful path the plan asked for —
    disconnect, a bounded wait (5 s) for the agent to leave on its own, and only
    then `Kill(entireProcessTree: true)`; `Shutdown()` stays the kill switch but
    now also waits for the exit and releases everything. One `CleanUp` path used
    by every exit route (stop, kill, crash, dispose): the client is **disposed**,
    not just disconnected; `Process.Exited` is unsubscribed; the process handle
    is disposed; `Disposed` is raised exactly once, whichever side died first.
    All state transitions under a lock.
  - **`HostManager`**: the finalizer is gone; the registry is synchronized; the
    `Disposed` subscription is removed on removal; `Dispose` is idempotent and
    takes every remaining agent's process down. **Breaking:** the constructor
    now takes a `Func<WitClientBuilderOptions>` factory instead of one options
    instance — the old shape handed every agent the *same* transport and
    address, so a second agent landed on the first one's endpoint. Each agent
    now gets its own options. (No caller in the workspace used the old
    constructor.)
  - `HostUtils.RunAgent` disposes the process object when `Start()` returns
    false. `AgentApplication` (WPF) needed nothing: no finalizer, and its parent
    process handle lives exactly as long as the process does.
  - **Integration tests against a real agent process** (the stub `Assert.Pass`
    suite is gone): a console `OutWit.InterProcess.Tests.Agent` serves the test
    contract over the pipe it is handed, follows its parent down, and writes a
    marker file from `ProcessExit` — which runs on a clean exit but not on a
    kill, so the graceful-stop test can tell the two apart after the fact.
    Eight tests: start/call/stop with no process left behind, graceful stop
    (marker present), shutdown kill (marker absent), crash → `Disposed` raised
    and state cleaned, missing executable fails cleanly, registry
    create/shutdown/dispose, crash → self-removal → replacement agent works,
    concurrent creation registers all three. The contract crosses the process
    boundary as shared source — two assemblies, two different .NET types — and
    dispatches via Stage 4's contract-scoped method ids, proving the
    assembly-independent routing on a live boundary.
- [x] Stage 7 — release prepared (commit on `v3`; publishing itself is a
  workflow run + the consumer wave, below).
  - **Versions**: every packable project is 3.0.0 — including `Client.Blazor`
    (was 1.0.9) and `InterProcess.*` (were 2.3.x); one family, one number.
  - **CHANGELOG**: a full 3.0.0 section — breaking changes, additions, fixes,
    the known WebSocket-restart defect — dated 2026-08-29.
  - **READMEs corrected** (the three the plan named, plus what a read-through
    caught): encryption is now described as authenticated AES-256-GCM used as
    defence in depth under TLS — the "end-to-end encryption" claim is gone
    everywhere (core, server, both BouncyCastle packages, Blazor, InterProcess);
    MMF is stated one-to-one by design with the `Local\` namespace and
    both-ends-3.0 requirements; the REST contract is written down in the
    Server.Rest README (envelope, base64-wrapped-JSON parameters, GET rule,
    status mapping, limits) with the client README pointing at it; retry
    semantics (idempotent-only, no InternalServerError retry) documented in
    client + Blazor READMEs; the root README banner now says 3.0.0; the
    InterProcess.Host README's fictional `WitProcessHost.Launch<T>` example is
    replaced with the real `HostAgent`/`HostManager` API, including the 3.0
    factory constructor.
  - **Warnings as errors**: the whole CI filter builds Release with
    `TreatWarningsAsErrors` — 30 own-code warnings fixed across the libraries
    and tests (unused catch variables, fire-and-forget `Task.Run`, nullability,
    two event declarations on `TcpServerTransport` shadowing the base class's,
    `SYSLIB0057`, NUnit analyzer findings). Generated-proxy warnings
    (CS0067/CS8669) are suppressed in the test project only, named as upstream.
  - **Test-infra hardening** (the bind-flake fix): transport tests now take
    OS-assigned free ports (bind port 0, cache per test name) instead of
    hash-derived ranges — a port can no longer land in a Windows excluded-port
    block (the 10013 story); kernel-object channel names carry the process id
    and multi-TFM test runs are serialized (`TestTfmsInParallel=false`) — the
    net8/net10 test hosts used to race each other for the same MMF/pipe names,
    which is exactly what a both-TFMs run reproduced.
  - **NativeAOT smoke does a real round-trip** (`Scripts/aot-smoke.ps1`, used
    by CI): publish the AOT client (closure guard: no Castle), run it bare
    (publish gate), then run it against a live JIT server — an **encrypted**
    Echo + AddAsync round-trip through the generated proxy, with the contract
    shared as linked source (two assemblies, id-based dispatch). The round-trip
    immediately caught **three** real AOT defects, each invisible to the old
    publish-only smoke: (1) the encryption handshake serialized `RSAParameters`
    through reflection-based System.Text.Json, which NativeAOT refuses —
    `RsaUtils` now writes/reads the same JWK-style JSON by hand (wire-identical,
    JWK aliases accepted); (2) MemoryPack found wire-model formatters through a
    reflection lookup that trimming removes — `MemoryPackWireFormatters` now
    registers every wire model explicitly in a module initializer, statically
    reachable, no reflection; (3) the generated proxy's type-name literals used
    reference-facade assembly names that NativeAOT cannot resolve, fixed
    upstream — pins raised to `OutWit.Common.Proxy` 1.2.11 and
    `OutWit.Common.Proxy.Generator` 2.2.2 (both published). Diagnostics
    improved along the way: the client logs handshake byte counts at Debug and
    passes its logger into handshake deserialization, so a swallowed
    serializer exception is named instead of degrading to a silent `false`.
  - **CI gate** (`.github/workflows/ci.yml`): restore, Release build with
    own-code warnings as errors on every TFM (via `WitRPC.CI.slnf` — libraries
    and tests, no Examples), all three test projects on every TFM they target
    with `--blame-hang`, the AOT smoke — and `publish.yml` now calls this
    workflow and refuses to push unless every job is green. The known
    WebSocket-restart hang test stays excluded, documented here under "Known
    defects".
  - Solution files: the five packages missing from `OutWit.sln` added; the new
    `AotSmoke.Server` and `InterProcess.Tests.Agent` registered in both
    `.sln` and `.slnx`.

### 3.1.0 follow-up — serializers become plugins

Decided during the 3.0.0 publish (too late for the major, and 3.0.0's core was
already on nuget.org): the core dragged MessagePack and protobuf-net into every
consumer — every Blazor WASM bundle included — although only a migrant from
SignalR or gRPC ever needs them. The reason those serializers exist at all is
**bring your own models**: someone with models already annotated for
MessagePack-CSharp or protobuf-net must be able to swap the transport and touch
nothing. The split keeps that promise exactly and makes it cheaper for everyone
else:

- `OutWit.Communication.Serializers.MessagePack` / `.ProtoBuf` carry the two
  serializers and one generic `WithX<TOptions>()` each, via the new
  `ISerializationOptions` in the core (implemented by both builder options), so a
  plugin references neither client nor server.
- `OutWit.Communication.Serializers.GoogleProtobuf` is new: proto-first gRPC
  models are `Google.Protobuf` `IMessage`s that protobuf-net cannot read, so the
  old "gRPC-compatible" promise held only for code-first users. `IMessage`
  payloads travel as protobuf wire bytes, everything else through a fallback
  (JSON by default), decided per declared type on both sides.
- The envelope is MemoryPack-only: the wire models lost their
  MessagePack/ProtoBuf attributes, `DiscoveryClientOptions.WithMessagePack/
  WithProtoBuf` went away, the ten wire-model MessagePack/ProtoBuf round-trip
  tests with them. User payloads are untouched by any of this.
- Consumer wave targets 3.1.0 directly — nobody in the workspace consumed 3.0.0,
  so the extra version costs no one a second bump.

### The consumer wave (after packages are published)

Publishing 3.0.0 is a `publish.yml` run per package (now gated on CI). Then:

- **WitCloud** (the coordinated wave): pins `Client` 2.4.0, `Client.Blazor`
  1.0.9, `Client.DynamicProxy` 2.4.0, `Client.WebSocket` 2.4.0,
  `Server.WebSocket` 2.4.0, `Server.DependencyInjection` 2.3.10 → all to
  3.0.0 in one commit; client and server sides must move together (protocol 3).
  Review retry usage (`MarkIdempotent` for anything that relied on default
  retries) and service-method thread-safety (`MaxConcurrentRequests`).
- **WitAnalytics / WitForms / WitLicense** float on `2.3.*`/`1.0.*` — floating
  ranges do not cross a major, so they stay on 2.x untouched until each repo
  migrates deliberately.
- **Common** pins 2.3.x/1.0.2 exactly — same story, migrates when chosen.

### Robustness fixes (found while validating, folded into the branch)

- **A failing service call no longer tears down the connection.** `ProcessMessage`
  awaited `RequestProcessor.Process` with no guard, so a service method that threw
  unwound to the connection loop's catch, which closed the connection — one bad
  call blocked every later request on it (all transports). Now the throw is caught
  and turned into an `InternalServerError` response: the caller gets an answer and
  the connection stays up. Regression test:
  `WitServerTransportEdgeCaseTests.TransportBoundaryFailureDoesNotBlockNextMessageTest`
  (which was itself wrong — it never drove the connection to `Authorized`, so it
  couldn't reach the processor; fixed to handshake first). Commit on `v3`: `1a209c2`.

### Known defects (found during 3.0 hardening, not yet fixed)

- **WebSocket server restart hangs a connected client — pre-existing, reproduces on
  `main`.** `CommunicationTestsBasic.StartStopWaitingForConnectionTest(WebSocket,*)`
  hangs (Pipes and Tcp pass). Root cause: the WebSocket factory's
  `StopWaitingForConnection` calls `HttpListener.Close()`, which aborts *existing*
  connections — so stopping the acceptor kills an already-connected client, which
  the test's contract (met by Pipes/Tcp) forbids. The naive fix (`Stop()` without
  `Close()`) keeps connections but leaves the prefix registered, so the restart's
  fresh `HttpListener.Start()` conflicts. A real fix reuses one listener across
  stop/restart (or redefines the stop contract). This affects the production
  WebSocket path (a server restart hangs live clients on their next request) and
  belongs in lifecycle hardening. Currently excluded from green runs via
  `--filter FullyQualifiedName!~StartStopWaitingForConnectionTest`.
- ~~A dropped connection does not fault the client's in-flight requests.~~
  **Fixed in the Stage 4 first half**: `OnServerDisconnected` and `Dispose` both
  complete pending requests with a transport exception, surfaced as
  `TransportError`.
- ~~Tcp bind flakiness (`AccessDenied`/10013) — pre-existing, reproduces on `main`.~~
  **Fixed in Stage 7's test-infra hardening**: ports are OS-assigned (bind port
  0) instead of drawn from fixed ranges, so they cannot land in a Windows
  excluded-port block; channel names carry the process id and multi-TFM test
  runs are serialized, so parallel test hosts cannot collide either.

### Stage 1 notes (for whoever picks up Stage 2+)

- MMF wire layout changed, so both ends must run 3.0 (acceptable for a local
  link). Public API is unchanged: `WithMemoryMappedFile(name[, size])`, options
  `{ Name, Size }`. Layout, names and the handshake live in `Communication/_Shared/MMF`.
- The handoff is gated by a factory-owned slot semaphore (`{name}_slot`, max 1):
  a ready channel posts one permit; a client claims it before attaching. This is
  what makes a reconnecting client always land on a fresh instance.
- The server's hello-ack is deliberately sent by the factory *after*
  `NewClientConnected` (via `ConfirmAttachedAsync`), so the connection is
  registered before the client's connect returns. Any future transport that
  adds a connect handshake must preserve this ordering or the layer above drops
  the client's first message.
- Test isolation: conformance channel names carry a per-fixture run id, and echo
  tests wait for `WaitForClientAsync` before the first send. Watch for the same
  reactive-echo-wiring race if you add echo-style harness tests elsewhere.

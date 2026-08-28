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
(`IAsyncEnumerable<T>`); `ValueTask` surface; capability negotiation beyond the
protocol version check. These are features, not hardening.

## Progress

- [x] Stage 0 — `Transports/TransportConformanceTests` in place: one
  `ITransport`-level contract for every transport pair, plus MMF-specific tests.
- [x] Stage 1 — MMF reworked (one-to-one, lossless, atomic handoff). All 11
  conformance tests pass repeatedly; the full end-to-end WitServer/WitClient
  MMF suite is 84/84 across serializers, static/dynamic proxy, reconnect,
  one-to-one and the callback tests the audit reported as hanging. Commits on
  `v3`: `e2b60a3` (rework + harness), `fc78f64` (ack-after-registration fix).
- [ ] Stage 2 — core concurrency and auth boundary
- [ ] Stage 3 — framing, limits, lifecycle (includes the per-connection outbound
  send lock for the stream transports; the conformance ConcurrentSends test is
  MMF-only until then)
- [ ] Stage 4 — protocol 3
- [ ] Stage 5 — REST
- [ ] Stage 6 — InterProcess
- [ ] Stage 7 — release

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

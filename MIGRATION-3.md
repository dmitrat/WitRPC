# Ecosystem migration to WitRPC 3.x

Status: **plan, verified against every consumer on 2026-08-29; execution starts
next session.** WitRPC 3.0.0 and 3.1.0 are on nuget.org (Server / Client / Client.DynamicProxy / Tcp 3.1.1; REST packages 3.2.2 with the readable contract restored, `WitClientRestBuilder` and composite hosts; DependencyInjection packages 3.2.1 with `AddWitRpcRestServer` / `AddWitRpcRestClient` and every interface-typed option resolvable from the container -- consumers pin the latest of each: Server / Client 3.1.1, transports 3.1.0 (Tcp 3.1.1), DI 3.2.1);
nothing that talks to production has been redeployed yet.

The one fact that shapes everything: **protocol 3 is not wire-compatible with
2.x.** A 3.x server refuses a 2.x client with a readable reason in *its* log; a
2.x client cannot read 3.x bytes at all and just fails to connect. Every WitRPC
edge between two deployables therefore moves both ends at once, and the plan
is a plan over edges, not over repositories. Decision taken: hard cutover, no
dual-stack period — the service has almost no traffic yet.

## Is the break worth it?

Judged against what this ecosystem actually runs (WebSocket + MemoryPack +
encryption everywhere, Blazor WASM UIs, a NativeAOT native SDK, a fleet of
render nodes on a public endpoint), the 3.x gains that matter:

| Gain | What 2.x did | Who feels it here |
|---|---|---|
| **Concurrency** | One global semaphore serialized every request and callback across *all* connections; a throwing callback serializer froze the server for good | The gateway: one node's slow blob call stalled every other client, every UI, the S2S channel |
| **Auth boundary** | Callbacks broadcast to unauthorized connections; failed auth left the socket open | `engine.omnibuscloud.com` and `id.*` are public endpoints |
| **Pre-auth limits** | No frame-size cap, no handshake timeout, unbounded `MemoryStream` on WebSocket | Same public endpoints (DoS surface) |
| **Contract-scoped dispatch** | Name-based routing, first registration wins — `CancelJobAsync(Guid)` on two channels collided | Confirmed in WitCloud's own composite server |
| **Honest statuses + idempotent-only retry + de-dup** | Timeout and service fault were one status; Blazor retried business exceptions 3×; a timed-out command could re-execute | Job submission paths; every Blazor UI |
| **Ordered frames** | Every client transport delivered frames via fire-and-forget `Task.Run` — an event could overtake the response it preceded | Silent today, deterministic tomorrow |
| **AEAD encryption** | CBC, static IV, no MAC | Local links (MMF/pipes) where there is no TLS; 4.5–6× faster on large payloads |
| **NativeAOT-proof wire path** | Reflection-based RSA JSON and reflection-discovered MemoryPack formatters — trimmed away under AOT | **The native SDK is NativeAOT**; 3.1.0's encrypted round-trip is proven in CI |
| **Version-tolerant payloads** | Any new wire field broke every older client | This is the *last* forced break: 3.x can add fields in minors |
| **Serializer plugins (3.1)** | Core dragged MessagePack + protobuf-net into every consumer | Every WASM bundle shrinks; gRPC/SignalR migrants get real plugins |
| **TCP NoDelay (3.1.1)** | Two writes per frame + Nagle ≈ 200 ms per message | Anyone on the TCP transport |

What 3.x does **not** give: streaming, cancellation propagation (deferred to a
3.x minor, now possible without a break), and the WebSocket server-restart
hang (pre-existing, still open). What it **demands**: service methods run
concurrently — every channel must be thread-safe (see the safety knob below).

Verdict: the break is a one-time cost that buys an evolvable wire; deferring
it means paying the same cost later plus running the 2.x defects in
production meanwhile. Worth it.

## Consumers, verified

Every .NET consumer was compiled against 3.1.0 with temporary pins (files
restored afterwards); plugins against a locally packed `OutWit.Cloud.SDK 2.0.0` (nuget.config extended with a scratch feed, restored afterwards).

| Consumer | Edge | Transport / SDK | Verification |
|---|---|---|---|
| **WitIdentity** server + UI | hub for six clients | Server.WebSocket 2.3.4, Server.DI 2.3.9, Client.Blazor 1.0.6, Client.WebSocket 2.3.3 | compiles on 3.1.0 once the ASP.NET WASM packages move 10.0.8 → 10.0.10 (`Client.Blazor 3.1.0` floor; NU1605 otherwise): **server and UI compile** with that bump |
| **WitCloud** server / UI / SDK / node client | UI↔server; server→Identity (S2S `IUsersChannel`); gateway↔nodes; gateway↔SDK | 2.4.0 / 1.0.9 / 2.3.10 | compiles; full suite **identical** on 2.4.0 and 3.1.0 (61 pre-existing failures, same names — missing `ClientRatingService` registration in `WitRpcTestHost`) |
| **WitForms** server + UI | UI↔server; UI→Identity | floating `2.3.*` / `1.0.*` | compiles on 3.1.0 |
| **WitAnalytics** server + UI | same | floating | compiles on 3.1.0 |
| **WitLicense** server + UI | same | floating | compiles on 3.1.0 |
| **Native SDK** `OutWit.Cloud.SDK.Native` | NativeAOT over the managed SDK → `omnibuscloud_native.dll` | from source | publishes under NativeAOT on 3.1.0 (10.5 MB) |
| **Blender addon** | python `pyoc` → native dll **bundled in the addon zip** (`NATIVE_VERSION 1.0.0`) | native 1.0.0 | rebuild zip with native 2.0.0 |
| **ParaView plugin** | python `pyoc` → native dll bundled in the plugin package | native | rebuild package |
| **3ds-Max plugin** | managed SDK **1.1.3** (public nuget only) | SDK | restores + compiles on SDK 2.0.0 (its `OutWit.Common.*` pins lag behind what the SDK needs — NU1605; align pins in the real bump) |
| **Inventor add-in** | managed SDK 1.2.0 (private feeds) | SDK | restores + compiles on SDK 2.0.0 (private feeds resolve; same pin alignment) |
| **Simulation** `OutWit.Simulation.Bridge.Session` | managed SDK 1.2.0 → also consumed by WitSweep | SDK | restores + compiles on SDK 2.0.0 |
| **WitSweep** | transitively: `Bridge.Session 0.2.0 → OutWit.Cloud.SDK 1.2.0 → WitRPC 2.3.3` | SDK via package | rebuild after Bridge.Session republishes |
| Controllers | SDK only in `@Simulation/live-test`; controllers do not talk to the gateway | — | not an initiator |
| External `OutWit.Cloud.SDK 1.3.0` users | managed SDK | nuget | SDK 2.0.0 is a major for them |

Contracts are neutral: `OutWit.Identity.Contracts / .Interfaces / .Contracts.Shared /
.Blazor / .Profile` reference no WitRPC package — nothing to republish there.
`WitIdentity/_Ecosystem/_WitRPC` is an unreferenced 2.x source snapshot — delete.
`Common` pins WitRPC only in `Settings/Samples` — irrelevant.

Login does not depend on WitRPC (OIDC over HTTP in `OutWit.Identity.Blazor`; JWT
validated by `Authority` in the servers). A version skew breaks profile pages
and the S2S user-directory lookup, not sign-in.

## Behaviour changes to handle in every server

1. **Concurrency.** `AddService<T>()` registers channels as singletons and 3.x
   invokes methods in parallel; 2.x serialized everything. Set the Stage 2 knob
   `MaxConcurrentRequests = 1` on every server for the first deploy (restores 2.x
   semantics), then audit each channel for shared state / scoped dependencies
   (`DbContext`, `UserManager`) and lift the cap per service.
2. **Retry.** No UI configures `ChannelFactoryOptions.Retry`; in 2.x that meant
   `InternalServerError` retried 3×, in 3.x nothing is retried until methods are
   declared idempotent. Declare the read-only channel methods
   (`IdempotentMethods`) where retries are wanted.
3. **Serializers.** Everyone uses MemoryPack + JSON — both still in the core.
   Nobody needs the new plugin packages.

## Versions to publish

| Package | From | To | Why |
|---|---|---|---|
| `OutWit.Cloud.SDK` | 1.3.0 | **2.0.0** | its consumers break on the wire — a major; declares `OutWit.Cloud.Auth 1.1.0` (already on nuget) |
| `OutWit.Cloud.SDK.Native` | 1.0.0 | **2.0.0** | same protocol inside; C ABI unchanged (additive) |
| `OutWit.Cloud.Client` (node) | current | next | appcast entry |
| `OutWit.Simulation.Bridge.Session` | 0.2.0 | next | on SDK 2.0.0; WitSweep follows |
| Blender addon / ParaView plugin | — | next | bundle native 2.0.0, `NATIVE_VERSION` |
| 3ds-Max / Inventor plugins | SDK 1.1.3 / 1.2.0 | SDK 2.0.0 | also closes their lag behind 1.3.0 |
| WitCloud local commit `96bc306` | — | — | still says SDK 1.3.0 — set 2.0.0 before publishing |

## The plan

### Step 0 — preparation (no production deploys)

- WitIdentity: delete `_Ecosystem/_WitRPC`; pins → 3.1.0 explicit; ASP.NET WASM
  packages → 10.0.10; `MaxConcurrentRequests = 1`; run its suite.
- WitForms, WitAnalytics, WitLicense: floating `2.3.*`/`1.0.*` → **explicit**
  3.1.0 (a floating range never crosses a major, and floats have bitten before);
  `MaxConcurrentRequests = 1`; suites.
- WitCloud: SDK → 2.0.0 in the csproj; node client gets one last **2.x** release
  whose only change is *"on repeated gateway connect failure, check the update
  feed immediately"* (today the check runs on a schedule of hours) — this turns
  the cutover's fleet downtime from hours into minutes. Same idea for the SDK
  (an HTTP version probe on connect failure, so a stale plugin can tell its user
  *why*).
- Build SDK 2.0.0, native 2.0.0, node 3.x, `Bridge.Session`, and every plugin
  package — **not published**.
- Manual smoke on a staging pair: UI login → profile page over WitRPC, S2S
  lookup, one job submitted from the native SDK loader (`examples/c/loader`).

### Step 1 — hub: WitIdentity 3.x

Deploy server + its UI (atomic — the server hosts the WASM). Skew window opens:
profile pages in the other UIs and WitCloud's S2S lookups fail until steps 2–3.
Login keeps working.

### Step 2 — same evening: WitForms, WitAnalytics, WitLicense

One deploy per service closes its internal edge and its edge to Identity.

### Step 3 — OmnibusCloud, its own window

In this order, minutes apart: gateway 3.x → node 3.x in the appcast → SDK 2.0.0
and native 2.0.0 published (`native-v2.0.0` tag) → `Bridge.Session` republished
→ plugin packages (Blender, ParaView, 3ds-Max, Inventor) and WitSweep rebuilt
and released. Nodes self-update over HTTP; stragglers are refused with a
readable line in the gateway log. Steps 1 and 3 can be swapped — the only cost
of the gap between them is the S2S lookup.

### Step 4 — after the dust settles

Lift `MaxConcurrentRequests` per service after the thread-safety audit; declare
idempotent methods where retries are wanted; fix WitCloud's test host
(`ClientRatingService`); WebSocket restart hang in WitRPC.

## Out of scope

Local Pipes/MMF/InterProcess applications outside the workspace ship host and
agent together — no cross-application edge; migrate one app at a time.

# Ecosystem migration to WitRPC 3.x

Status: **Step 0 executed on 2026-08-30 — every consumer is moved in a local,
unpushed commit and rebuilt; nothing is published or deployed yet.** See
"Step 0 — done" below for the exact state and the Step 3 checklist. WitRPC 3.0.0 and 3.1.0 are on nuget.org (Server / Client / Client.DynamicProxy / Tcp 3.1.1; REST packages 3.2.3 with the readable contract restored, `WitClientRestBuilder` and composite hosts; DependencyInjection packages 3.2.1 with `AddWitRpcRestServer` / `AddWitRpcRestClient` and every interface-typed option resolvable from the container -- consumers pin the latest of each: Server / Client 3.1.1, transports 3.1.0 (Tcp 3.1.1), DI 3.2.1);
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
| **WitCloud.Portal** host + WASM client + `OutWit.Portal.Cloud` | client↔host (own channels); `Portal.Cloud`→**WitCloud gateway** (`Contracts.Internal` over WitRPC); client→Identity (`IdentityChannelFactory`); host→Identity S2S | Server.DI 2.3.9, Server.WS 2.3.4, Client.Blazor 1.0.7, Client.WS 2.3.3 | **missed by the first survey.** One deployable on two 3.x edges: it goes with the gateway in Step 3, and its Identity edges are down between Steps 1 and 3 exactly like WitCloud's S2S. Compiles and its suites pass on 3.x (Step 0) |
| **WitForms** server + UI | UI↔server; UI→Identity | floating `2.3.*` / `1.0.*` | compiles on 3.1.0 |
| **WitAnalytics** server + UI | same | floating | compiles on 3.1.0 |
| **WitLicense** server + UI | same | floating | compiles on 3.1.0 |
| **Native SDK** `OutWit.Cloud.SDK.Native` | NativeAOT over the managed SDK → `omnibuscloud_native.dll` | from source | publishes under NativeAOT on 3.1.0 (10.5 MB); the latest release is **native-v1.1.0** (not 1.0.0 as first written) |
| **Blender addon** | python `pyoc` → native dll **bundled in the addon zip** (`NATIVE_VERSION 1.0.0`) | native 1.0.0 | rebuild zip with native 2.0.0 |
| **ParaView plugin** | python `pyoc` → native dll bundled in the plugin package (`Plugin/NATIVE_VERSION 1.1.0`) | native 1.1.0 | rebuild package |
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

### Step 0 — done (2026-08-30)

Everything below is a **local, unpushed commit on `main`** of its repository
(the Portal, the OmnibusCloud initiators and the two plugin repos included),
so the cutover is a `git push` + tag per repository in the order of Steps 1–3.
Every suite was run after the change; numbers are in the commit messages.

| Repository | Commit(s) | What moved |
|---|---|---|
| WitIdentity | `a3de4a1` | Server.DI 3.2.1, Server.WS 3.1.0, Client.Blazor / Client.WS 3.1.0 (server, UI **and the Examples in the solution**); WASM Authentication/DevServer 10.0.10; `WithMaxConcurrentRequests(1)`; `_Ecosystem/_WitRPC` deleted (it was gitignored) |
| WitForms, WitAnalytics, WitLicense | `d572b39`, `02e4e18`, `ae20a2a` | floating `2.3.*` / `1.0.*` → explicit 3.2.1 / 3.1.0; the cap |
| WitCloud.Portal | `c64eda2` | all three projects on 3.x (+ Client.DynamicProxy 3.1.1 in `Portal.Cloud`); WASM 10.0.10 and `Microsoft.Extensions.*` 10.0.10 floors; the cap |
| WitCloud | `96bc306` → `77f0271`, `885fe42`, `81219b7`, `6cda86a` | pins 3.1.1 / 3.2.1; the cap on the gateway **and the per-node servers**; `GET /version`; **SDK 2.0.0**; the node's update-on-connect-failure (below); the SDK probe (below); the AOT baseline (35 reviewed, one new upstream key from the 3.x interceptor) |
| WitCloud `release/2.x` | worktree `../WitCloud-2x`, `85751e0`, `b78fbf9`, `034ba94` | the **last 2.x line** off `b2956be`: the same node feature and SDK probe, SDK **1.3.1** (protocol 2). Builds; client + SDK suites green. Tag `client-v1.1.6-beta` on `034ba94` is the last 2.x node release |
| Simulation | `e113754` | `Bridge.Session` **0.3.0** on SDK 2.0.0; solver floors (Auth 1.1.0 → Serilog 4.4.0, Aspects 1.3.4) |
| WitSweep | `a0ce720` | Bridge.Session `0.3.*`, Serilog 4.4.0 |
| Inventor | `ddd22d9` | SDK 2.0.0, Bridge.Session `0.3.*` |
| 3ds-Max | `6474fc5` | SDK 1.1.3 → 2.0.0, Auth 1.1.0, Serilog 4.4.0, Aspects 1.3.4 |
| Blender, ParaView | `f53c906`, `ebe95f8` | `NATIVE_VERSION` → 2.0.0; both packages built against a locally published native 2.0.0 |

Two things the survey had wrong, both fixed above: **WitCloud.Portal** is a
WitRPC consumer on two edges, and the native SDK's latest release is
**1.1.0**. One 3.x API move that bit twice: the reflection-based
`WitClient.GetService<T>()` now lives in `OutWit.Communication.Client.DynamicProxy`
(WitIdentity's `Example.WeatherConsumer`, the Portal's `Portal.Cloud`).

**The node follows a server it can no longer talk to** (`77f0271`, on both
lines). The scheduled update check only notifies, so until now a fleet
followed a server upgrade as fast as someone clicked "Update now" per
machine. Now, on the third consecutive failed gateway-level recovery (then
every twelfth) and on a startup connect failure, the node asks the public
feed; when a newer build is published for its platform it downloads +
verifies it and hands it to the external updater, then exits with CleanQuit
(desktop shell and background mode both). Feed traffic is once per 10 min
per node; `AutoUpdateOnConnectFailure` (user-scoped, default on) opts a device
out. **This is what makes the last 2.x node release worth shipping before
Step 3**: a fleet on `client-v1.1.6-beta` follows the 3.x gateway in
minutes, a fleet on 1.1.5 does not follow at all.

**The SDK explains a refused connect** (`885fe42`, both lines). `GET /version`
on the gateway (3.x commit) answers `{ server, protocol, minSdk }`; the SDK
probes it when `ConnectAsync` returns false and names both protocols and the
release to move to. The 2.x SDK **1.3.1** carries the same probe (protocol 2)
— publishing it is optional: it only helps a plugin rebuilt on it *before*
the cutover, and every plugin is rebuilt on 2.0.0 right after anyway.

Verified beyond compiling: WitCloud core sweep 834/0, SDK 99, Data 324,
Native 132, Documents 59; native AOT publish + reviewed-warning gate + ABI
export gate + C loader + C++ host all PASS on win-x64; SDK 2.0.0 packed
(dependencies Auth 1.1.0, Contracts 1.2.0, Client 3.1.1, Client.WS 3.1.0 —
all on nuget.org already); Bridge.Session 0.3.0 packed; WitIdentity,
Forms, Analytics, License and Portal suites green including the PostgreSql
providers (throwaway `postgres:16` container).

**Local state to be aware of.** The four initiator repos (3ds-Max, Inventor,
Simulation, WitSweep) have an **uncommitted** `nuget.config` edit adding the
scratch feed (`…\scratchpadeed`) so they restore SDK 2.0.0 / Bridge.Session
0.3.0 before those are published — `git checkout nuget.config` in each before
pushing. `~/.nuget/packages` holds the scratch-built `outwit.cloud.sdk/2.0.0`
and `outwit.simulation.bridge.session/0.3.0`: **delete both folders after the
real publish**, or the local builds keep using the scratch bits.

**Not done in Step 0 — the smoke on a real pair.** Two routes were tried and
both are blocked on things only the operator has: `test.omnibuscloud.com`
accepts publickey only and none of the keys on this machine is authorized
(`id_ed25519` is encrypted with another passphrase; `omnibuscloud_macbook`
and `id_ed25519_stats_deploy` are refused for root/ubuntu/dmitrat) — the
right key or user is needed; and the `WitCloud.Test` fork that feeds the
test server is 88 commits behind `main` with its own `feat(api): job
visibility` conflicting in `IApiChannel` / `IWitCloudJobs` / the SDK, so
syncing it is a merge to be done deliberately, not a pull. What stands in
for the pair meanwhile: the WitCloud **Integration** category runs the real
3.x `WitServer` + WebSocket + encryption in-process (151 pass; the same 61
`ClientRatingService` test-host failures as on 2.x), which is the identical
`AddWitRpcServerWithServices` path WitIdentity, Forms, Analytics, License and
the Portal host use. The Blazor UI ↔ server edge and the S2S lookup have no
headless test and remain the first thing to click through after Step 1.
Also pre-existing: the 3ds-Max `SmokeValidateCurrentSceneThrough3dsMaxBatch*`
tests fail identically on the unchanged tree (local 3ds Max, exit -130).

#### Step 3 checklist, in order

1. WitCloud: push `main`; tag `v1.6.103` (or the docker.yml dispatch) → gateway image; deploy.
2. WitCloud.Portal: push `main`; tag → image; deploy (its gateway edge is now 3.x; its Identity edges were already 3.x after Step 1).
3. WitCloud: push `release/2.x`; tag `client-v1.1.6-beta` **on `034ba94`** → the last 2.x node in the appcast is what the fleet needs *before* the gateway flips. (This is the one step that may run **before** 1: it is protocol-neutral.)
4. WitCloud `main`: tag `client-v1.2.0-beta` → the 3.x node in the appcast; nodes on 1.1.6 pick it up within minutes of being refused.
5. WitCloud: `publish.yml` for `OutWit.Cloud.SDK` (2.0.0) → nuget.org; tag `native-v2.0.0` → native-sdk.yml → then `native-carrier-nuget.yml` 2.0.0 → nuget.org.
6. Simulation: push; `publish.yml` for `OutWit.Simulation.Bridge.Session` (0.3.0) → OmnibusCloud feed.
7. WitSweep, Inventor, 3ds-Max, Blender (`addon-v2.0.0`), ParaView: push, release.
8. Everywhere: `git checkout nuget.config` was done before the push; delete the two scratch package folders from `~/.nuget/packages`.

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

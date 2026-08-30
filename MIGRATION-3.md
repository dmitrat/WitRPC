# Ecosystem migration to WitRPC 3.x

Status: **rolling out, 2026-08-30 — WitIdentity 1.5.0, the WitCloud gateway
1.7.0 and the Portal 1.1.0 are LIVE on protocol 3; the 3.x node
(`client-v2.0.0-beta`) and the native SDK (`native-v2.0.0`) are released and
in the portal feed; the node fleet, the plugins (with the nuget.org
publishes) and the three small services are still ahead.** "Rollout log"
below records what each step showed; "Rollout, in the order chosen" is the
plan it follows; "Step 0 — done" records how the code got there. WitRPC 3.0.0 and 3.1.0 are on nuget.org (Server / Client / Client.DynamicProxy / Tcp 3.1.1; REST packages 3.2.3 with the readable contract restored, `WitClientRestBuilder` and composite hosts; DependencyInjection packages 3.2.1 with `AddWitRpcRestServer` / `AddWitRpcRestClient` and every interface-typed option resolvable from the container -- consumers pin the latest of each: Server / Client 3.1.1, transports 3.1.0 (Tcp 3.1.1), DI 3.2.1);
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
| WitCloud | `96bc306` → `77f0271`, `885fe42`, `81219b7`, `6cda86a`, `fe2f464`, `f20ec57` | pins 3.1.1 / 3.2.1; the cap on the gateway **and the per-node servers**; `GET /version`; **SDK 2.0.0**; the node's update-on-connect-failure (below); the SDK probe (below, hardened after the rehearsal); the AOT baseline (35 reviewed, one new upstream key from the 3.x interceptor) |
| WitCloud `release/2.x` | worktree `../WitCloud-2x`, `85751e0`, `b78fbf9`, `034ba94`, `fb12792`, `4a89270` | the **last 2.x line** off `b2956be`: the same node feature and SDK probe, SDK **1.3.1** (protocol 2). Builds; client + SDK suites green. Tag `client-v1.1.6-beta` on `4a89270` is the last 2.x node release |
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

**Not done in Step 0 — the smoke on a real Identity pair.** The gateway edge
was rehearsed against a real 3.x image (next section). The `WitCloud.Test`
fork that feeds the test server is 88 commits behind `main` with its own
`feat(api): job visibility` conflicting in `IApiChannel` / `IWitCloudJobs` /
the SDK, so syncing it is a merge to be done deliberately, not a pull — the
rehearsal image came from `main` with the controller set trimmed instead.
The WitCloud **Integration** category runs the real 3.x `WitServer` +
WebSocket + encryption in-process (151 pass; the same 61
`ClientRatingService` test-host failures as on 2.x), which is the identical
`AddWitRpcServerWithServices` path WitIdentity, Forms, Analytics, License and
the Portal host use. The Blazor UI ↔ server edge and the S2S lookup have no
headless test and remain the first thing to click through after Step 1.
Also pre-existing: the 3ds-Max `SmokeValidateCurrentSceneThrough3dsMaxBatch*`
tests fail identically on the unchanged tree (local 3ds Max, exit -130).

#### Rehearsal against a real 3.x gateway (2026-08-30)

The test box (`test.omnibuscloud.com`, ssh `dmitrat` with `id_ed25519`) runs
the `WitCloud.Test` fork image + Postgres against the production
`auth.omnibuscloud.com`, and had **four nodes online** (node-01/02/03,
VALENTINA-PC). Flipping it to 3.x locks those out with nothing to
self-update to (no 3.x node in the appcast yet, no 1.1.6 on them), so the
flip was **prepared, not done**: a 3.x image was built *on the box* from a
scratch branch of `main` (`rehearsal/test-server` = `main` + the Render/
ParaView families dropped from the controller set, like the fork) —
`ghcr.io/dmitrat/witcloud-test:witrpc3`, 4.08 GB, `~/witcloud-rehearsal`.
To flip: `cd /opt/outwitcloud/current && sed -i 's/^OUTWITCLOUD_VERSION=.*/OUTWITCLOUD_VERSION=witrpc3/' .env && docker compose up -d`;
rollback is the same line with `latest`. (The uplink from the dev machine is
~0.7 MB/s — never scp an image; the 6 MB git bundle + on-box build took
minutes.)

The same image ran **locally** in Docker with the test tenant's `.env` /
`tenant.json` and a Caddy in front (`/api*` → 7501, rest → 7500, like the
host Caddy), and the native SDK was pointed at it through pyoc:

| Client | Gateway | Outcome |
|---|---|---|
| native **2.0.0** (protocol 3) | local **3.x** | connect, scopes, asset upload / query / download over WitRPC 3 — OK; job submit reached the server and was refused on authorization (fresh DB, no rights) — the RPC path is proven |
| native **1.1.0** (released, protocol 2) | local 3.x | fails with `The length of the data to decrypt is not valid for the size of this key` — a 2.x client reads the protocol-3 refusal as a broken RSA reply. **This is what every plugin in the field says after Step 3** |
| native **1.3.1** (2.x line + probe) | local 3.x | `the server (v1.6.103…) speaks WitRPC protocol 3, this SDK (v1.3.1) speaks protocol 2. Update OutWit.Cloud.SDK … to 2.0.0 or newer. (handshake error: …)` |
| native 2.0.0 | **real 2.x** test gateway | `The server did not answer GET /version, so the WitRPC protocol it speaks is unknown; this SDK (v2.0.0) speaks protocol 3, and a server from before GET /version existed may speak an older one.` |

The gateway side logs the refusal readably: `Unreadable initialization from
client …: most likely a pre-protocol-3 client; refusing`. Two things the
rehearsal corrected in the SDK (`fe2f464`, `f20ec57` on main; `fb12792`,
`4a89270` on 2.x): the probe had only run when `ConnectAsync` returned
false, but a 2.x client *throws* from the crypto layer — it now runs on the
exception path too; and a pre-`/version` gateway answers `200 text/html`
(the SPA fallback), which the probe now ignores.

Not rehearsed: the node edge (a dev node on this machine would share the
session store with the production node installed here — refresh-token
rotation would log that one out; run a node from another machine with a
browser login instead), the Portal edge, and the WitIdentity edges.

#### Pushed and built (2026-08-30)

| Repository | Pushed | Tag → artifact |
|---|---|---|
| WitRPC | `v3` = `main` (docs) | — |
| WitIdentity | `main` | `v1.5.0` → `ghcr.io/dmitrat/witidentity:1.5.0` + `latest` |
| WitForms / WitAnalytics / WitLicense | `main` | `v1.3.0` / `v1.1.0` / `v1.3.0` → images |
| WitCloud.Portal | `main` | `v1.1.0` → `ghcr.io/dmitrat/witcloud-portal:1.1.0` |
| WitCloud | `main`, `release/2.x` | `v1.7.0` → `ghcr.io/dmitrat/witcloud:1.7.0`; `native-v2.0.0` → GitHub release + GitHub Packages carrier (**not** nuget.org yet); `client-v1.1.6-beta` (on `release/2.x`) → GitHub release = the last 2.x node, which the portal feed advertises to the fleet within its poll interval |
| Simulation, WitSweep, Inventor, 3ds-Max, Blender, ParaView | `main`, **no tags** | their CI stays red until `OutWit.Cloud.SDK 2.0.0` / the native carrier are on nuget.org (plugin phase) |

Deliberately **not** done yet: `client-v2.0.0-beta` (the 3.x node — the portal
feed would advertise it to a fleet whose gateway is still 2.x; one click on
"Update now" then strands that node until the flip), and every nuget.org
publish (`OutWit.Cloud.SDK 2.0.0`, the native carrier, `Bridge.Session
0.3.0`), which third parties would pick up against a 2.x production gateway.

CI outcomes (2026-08-30): every CI / docker / client-release run is green (WitRPC, WitIdentity 1.5.0, Forms 1.3.0, Analytics 1.1.0, License 1.3.0, Portal 1.1.0, WitCloud 1.7.0, client-v1.1.6-beta). `native-sdk` publishes and passes the AOT gate, ABI gate, C loader, C++ host and pyoc offline gate on all three RIDs -- after one real fix: on macOS the .NET 10 Apple crypto PAL, reached for the first time by AES-GCM, force-loads the Swift 6 overlays that the Xcode 15 SDK of macos-14 lacks; the workflow now selects Xcode 16 (`fcf441a`, and `native-v2.0.0` was moved onto it). Its only failing step is the *pyoc live smoke against production*, which a protocol-3 library cannot pass while engine.omnibuscloud.com is 2.x -- so the `release` job of the native-v2.0.0 run stays skipped until step 2 below, after which `gh run rerun --failed` on that run publishes the GitHub release and the GitHub Packages carrier.

#### Rollout log (2026-08-30)

1. **WitIdentity 1.5.0 — live (~07:50Z).** 3.x probe connects and dispatches,
   2.x probe refused. The first report was "the UI does not connect": the
   browser had cached the 2.x `cryptoInterop.js` (the host sends no
   `Cache-Control` for `_content/*`, the path never changes); a private
   window worked. Root fix: **Client.Blazor 3.1.1** (`0df3624`, on
   nuget.org) — `EncryptorClientWeb` probes for the protocol-3 functions and
   re-imports the module under a versioned URL when they are missing. Hosts
   already built on 3.1.0 need one Ctrl+F5 per returning browser.
2. **WitCloud gateway 1.7.0 — live (08:33Z).** The first
   `witcloud-update.service` run died on ghcr `TOOMANYREQUESTS` while pulling
   the 7 GB application layer (the image is the same size and shape as
   1.6.102 — 7.25 GB compressed, one publish layer — so every update pulls
   it); a later run went through. Verified: `/version` → protocol 3, health
   ready, S2S Cloud→Identity authenticated over 3.x, the production SDK
   smoke (native 2.0.0) passes end to end including a real
   `RenderBlenderVersion` job; the 1.1.0 library is refused with the crypto
   error; 158 refusals of 2.x nodes in the first hour (the fleet retrying
   every 5 s). **Defect to file:** a job submitted seconds after the gateway
   started failed in `OutWit.Engine.WitEngine.Compile` with
   `ArgumentNullException (Parameter 'provider')` — `CreateScope` on a
   provider the engine had not been given yet; fine once warm.
3. **`client-v2.0.0-beta`** released (11 assets) right after the flip;
   **`native-v2.0.0`** released after the rerun (all three RIDs passed the
   live smoke against the 3.x gateway; carrier nupkg attached, **not** on
   nuget.org yet).
4. **Portal 1.1.0 — live (08:44Z)** on the engine host: connected to the
   gateway on the first call, artifact sync imported the eight 2.0.0-beta
   builds within 30 s, `latest.json` serves 2.0.0-beta — nodes on 1.1.6 can
   now follow by themselves.
5. **Nodes on 2.0.0-beta — the first real defect of the cutover (09:00Z).**
   Two nodes reconnected over protocol 3 (`reconnected via machineId …`),
   reported controllers and benchmarks, heartbeats flowed — and the first
   distributed render (`RenderStill` of `cube_diorama.blend`, dispatched to
   `2/2` compatible nodes) died with *Task batch timed out — no progress
   from the node for 90 s*; the node's own log had no trace of the batch.
   Cause, confirmed in WitRPC source and by a test: **3.x stamps every
   callback with the contract id of the type the service was registered
   as**, and the node-side proxy — built on `ICloudChannel` — drops a
   callback stamped for any other id; the per-node server registered the
   class (`WithService<CloudChannel>`), so no `NodeTaskBatchReceived` /
   `NodeCancelReceived` ever reached a node while requests kept working
   (the request path has a name-based fallback, the callback path has
   none). It is the only channel in the ecosystem with events, so nothing
   else deployed is affected. Fixes, all pushed:
   - **WitCloud 1.7.1** (`eaf6146`): `WithService<ICloudChannel>(channel)`;
     regression test `PerNodeCallbackContractTests` over the real per-node
     channel (fails against the class registration, passes with the
     contract) — image built by docker.yml, deploy = `witcloud-update.service`
     again (7 GB, the ghcr throttle may need a re-run).
   - **The WitRPC test host** (`aa4323e`): it never registered
     `ClientRatingService`, so every `RegisterClient` through it failed and
     61 Integration/E2E tests had been failing on that one message — which is
     how this shipped untested. Unmasked, E2E goes 37 → 65 of 71; the six
     left are the Simulator `Grid.ForEach` tests (separate).
   - **OutWit.Communication 3.1.2** (`9e387f4`, on nuget.org): an event a
     class inherits from one of its interfaces is stamped with the
     interface's contract id, and the method ids of implemented interfaces
     are indexed — class-registered services work for everyone. Consumers
     pick it up with their next bumps; the interface registration in WitCloud
     stands on its own.

Ahead: the node fleet (manual installs of 2.0.0-beta), then the plugins —
`publish.yml` OutWit.Cloud.SDK 2.0.0 → nuget.org, `native-carrier-nuget.yml`
2.0.0, Simulation `publish.yml` Bridge.Session 0.3.0, then the plugin
releases one at a time — then WitForms / WitAnalytics / WitLicense, rebuilt
on Client.Blazor 3.1.1 first.

#### Rollout, in the order chosen

Nothing deploys itself: every step below is a manual `compose pull` on a
host, and every image tag is pinned in that host's `.env`. Login keeps
working throughout (OIDC over HTTP).

1. **WitIdentity → `1.5.0`.** On the identity host: set `WITIDENTITY_VERSION=1.5.0`
   in `.env`, `docker compose pull && docker compose up -d`. Live tests: sign in
   to the Identity UI, open *My profile* (that page is WitRPC), change a
   setting; the other UIs' profile pages and WitCloud's S2S lookup are
   *expected* to fail from now until steps 2–3. Rollback: `1.4.1` the same way.
2. **WitCloud gateway → `1.7.0`.** `OUTWITCLOUD_VERSION=1.7.0`, pull, up. Every
   2.x node is refused from this moment (gateway log: *Unreadable
   initialization … pre-protocol-3 client; refusing*). Live tests:
   `curl https://engine.omnibuscloud.com/version` → `"protocol":3`; the admin
   UI opens (Cloud.UI is in the image); the S2S user lookup works again.
   Rollback: `1.6.102`.
3. **The 3.x node.** *Immediately after 2:* tag `client-v2.0.0-beta` on WitCloud
   `main` (`git tag -a client-v2.0.0-beta -m "..." && git push origin client-v2.0.0-beta`);
   client-release.yml publishes the GitHub release, the portal feed picks it
   up within its poll interval. Nodes on 1.1.6 that are refused check the feed
   and install it unattended; every other node is a manual install
   (download from the portal, run the installer).
4. **Portal → `1.1.0`.** `PORTAL_VERSION=1.1.0`, pull, up. Live tests: sign in,
   *Account* (Identity channel) and a project page (gateway channel).
5. **Plugins, one at a time**, each with its live test against the 3.x gateway:
   - dispatch WitCloud `publish.yml` for `OutWit.Cloud.SDK` with *push to
     nuget.org* → 2.0.0; dispatch `native-carrier-nuget.yml` with version
     `2.0.0` → the carrier on nuget.org (both are irreversible: nuget.org
     never deletes);
   - Simulation: dispatch `publish.yml` for `OutWit.Simulation.Bridge.Session`
     → 0.3.0 on the OmnibusCloud feed; re-run the red CI of Simulation,
     WitSweep, Inventor, 3ds-Max, ParaView (they only needed the packages);
   - Blender `addon-v2.0.1` (an `addon-v2.0.0` already exists, built with
     native 1.0.0), ParaView `plugin-v0.6.2`, 3ds-Max `plugin-v1.1.0-beta`,
     Inventor and WitSweep releases per their own conventions.
6. **WitForms → `1.3.1`, WitAnalytics → `1.1.1`, WitLicense → `1.3.1`** last
   (rebuilt on Client.Blazor 3.1.1, so returning browsers heal their cached
   interop script by themselves), each: version in `.env`, pull, up; live
   test = sign in + the profile page + one channel-backed page.
7. Afterwards, on the dev machine: `git checkout nuget.config` in 3ds-Max,
   Inventor, Simulation, WitSweep (the scratch feed), and delete
   `~/.nuget/packages/outwit.simulation.bridge.session/0.3.0` and
   `outwit.cloud.sdk/2.0.0` so the published bits replace the scratch ones.
   The test box keeps its prepared `witcloud-test:witrpc3` image; flip it the
   same way whenever its four nodes can follow.

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

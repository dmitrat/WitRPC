# WitRPC WebSocket host restart investigation for dynamic per-client servers

> **CLOSURE (2026-06-10)**: The lifecycle bug investigated here was fixed in `8b8501c` (restart-safe listener lifecycle for WebSocket/TCP/Pipes/MMF factories, `ITransportServerFactory : IDisposable`, synchronous bind failure, 2026-04-22), with `173744a` (stale-frame hardening) and `cb4a68d` (empty-response crash fix) completing the arc; regression-tested (`TransportFactoryLifecycleTests` and friends). Remaining: REST server lifecycle not hardened; broad-batch test run still unchecked. See `witrpc-transport-restart-fix-plan.md` and CHANGELOG.md. Note: this file is still untracked in git.

## Context

This note is an upstream task for the WitRPC solution.

Current downstream symptom in `OutWit.Cloud`:

- gateway reconnect after full host restart succeeds;
- `RegistrationChannel.Reconnect(...)` returns a fresh per-client endpoint;
- `ClientPoolManager.CreateSlot(...)` creates a fresh per-client `WitServer` and calls `StartWaitingForConnection()`;
- but the client still cannot connect to the recreated `/api/{clientId}/` WebSocket endpoint.

The downstream task that captured the symptom is `@Tasks\witrpc-dynamic-per-client-server-restart-blocker.md`.

## Why this now looks like a WitRPC transport/server lifecycle bug

`OutWit.Cloud` already recreates the per-client slot and server on reconnect.

In `Cloud\OutWit.Cloud\Managers\ClientPoolManager.cs`:

- `CreateSlot(...)` builds a brand new `WitServer`,
- immediately calls `server.StartWaitingForConnection();`,
- then returns the endpoint URL to the client.

So if the endpoint never becomes connectable after restart, the next likely layer is the WitRPC server lifecycle itself, not the cloud slot-selection logic.

## Strong source evidence inside `_Ecosystem\_WitRPC`

### 1. `WebSocketServerTransportFactory.StopWaitingForConnection()` does not actually stop or release the listener

File: `_Ecosystem\_WitRPC\OutWit.Communication.Server.WebSocket\WebSocketServerTransportFactory.cs`

Relevant code:

- constructor creates one long-lived `HttpListener` up front (`lines 38-40`);
- `StartWaitingForConnection(...)` starts a background task and calls `Listener.Start()` inside it (`lines 46-53`);
- the accept loop then waits on `await Listener.GetContextAsync()` (`line 59`);
- `StopWaitingForConnection()` only cancels the token (`lines 108-111`).

Problem:

- cancellation alone does **not** unblock `HttpListener.GetContextAsync()`;
- `StopWaitingForConnection()` never calls `Listener.Stop()`, `Listener.Close()`, or `Listener.Abort()`;
- therefore the old `HttpListener` can remain alive and continue owning the prefix after host stop/dispose.

This is the single strongest candidate for the restart blocker.

### 2. `WitServer.Dispose()` does not dispose the transport factory

File: `_Ecosystem\_WitRPC\OutWit.Communication.Server\WitServer.cs`

Relevant code:

- `Dispose()` only disposes active transports/connections (`lines 369-377`);
- it does **not** dispose or shut down `TransportFactory`.

Combined with the WebSocket transport behavior above, this means:

- old accepted WebSocket connections may be disposed,
- but the server-side `HttpListener` itself can remain alive after server disposal,
- so the OS-level prefix binding may survive the old host lifecycle.

### 3. `ITransportServerFactory` has no disposal contract

File: `_Ecosystem\_WitRPC\OutWit.Communication\Interfaces\ITransportServerFactory.cs`

Relevant code:

- interface exposes only `StartWaitingForConnection(...)`, `StopWaitingForConnection()`, and `Options` (`lines 10-19`);
- there is no `IDisposable` / `IAsyncDisposable` contract.

This makes transport cleanup incomplete by design for transports that own OS resources such as `HttpListener`.

### 4. Listener startup failures are very easy to lose silently

File: `_Ecosystem\_WitRPC\OutWit.Communication.Server.WebSocket\WebSocketServerTransportFactory.cs`

Relevant code:

- `StartWaitingForConnection(...)` returns `void` immediately;
- `Listener.Start()` is executed inside `Task.Run(...)` (`lines 50-53`);
- if `Listener.Start()` throws because the prefix is still owned by a stale listener, the caller does not observe that startup failure;
- `ClientPoolManager` will still log that the per-client server was created, because `StartWaitingForConnection()` itself did not fail synchronously.

This matches the downstream symptom very well:

- slot creation appears successful in logs,
- but the endpoint never becomes reachable.

### 5. WitRPC tests already contain an explicit hint that WebSocket restart is broken

File: `_Ecosystem\_WitRPC\OutWit.Communication.Tests\Communication\CommunicationTestsReconnection.cs`

Relevant comment (`lines 149-152`):

- auto-reconnect tests use only Pipes;
- `WebSocket has issues with HTTP listener port release on server restart`.

So the upstream codebase already implicitly acknowledges the same class of problem.

## Most likely root cause

The most likely root cause is this sequence:

1. old host stops;
2. old per-client or gateway `WitServer` calls `StopWaitingForConnection()`;
3. that only cancels a token and does **not** stop/close the underlying `HttpListener`;
4. old accept loop may remain blocked in `GetContextAsync()` and the old listener continues to own the prefix;
5. new host starts in the same process lifetime and creates a fresh dynamic server for the same `/api/{clientId}/` prefix;
6. `Listener.Start()` in the new server either fails silently in the background task or the new route never becomes the active reachable listener;
7. downstream logs still say slot/server creation succeeded, but the endpoint never accepts the client connection.

## Secondary hypothesis worth checking

A related possibility is that the apparently successful gateway reconnect after host restart is partly a false positive:

- the old gateway `HttpListener` may still be alive after host disposal,
- the reconnect call may still be hitting an orphaned old WitRPC listener,
- which would explain why the registration/reconnect step appears to work even though the restarted host lifecycle is inconsistent.

This should be verified by adding explicit listener instance/start-stop logging in WitRPC during investigation.

## Proposed upstream fix direction

### A. Make WebSocket listener shutdown real, not token-only

In `WebSocketServerTransportFactory`:

- `StopWaitingForConnection()` should actively stop the listener:
  - call `Listener.Stop()` and/or `Listener.Close()`/`Listener.Abort()` as appropriate;
  - dispose the current cancellation token source;
  - close/dispose all active transports/connections owned by the factory;
  - ensure the accept loop exits deterministically.

Important detail:

- because closing `HttpListener` usually makes that instance unusable for future restarts, the factory should likely recreate the listener on each start instead of constructing it once in the constructor.

### B. Move listener creation to the start phase, not the constructor

Current design creates `HttpListener` in the constructor and keeps it for the lifetime of the factory.

A more restart-safe design is:

- store only the transport options in the constructor;
- create a fresh `HttpListener` in `StartWaitingForConnection()`;
- add prefixes there;
- store the accept-loop task;
- on stop/dispose, stop/close the listener and clear the field;
- on a future start, create a new listener instance.

This makes restart behavior explicit and avoids reusing a stale listener object across lifecycles.

### C. Surface startup failure to the caller

Right now `StartWaitingForConnection()` is fire-and-forget and can fail invisibly.

At minimum, the implementation should:

- wrap `Listener.Start()` in explicit try/catch with strong logging,
- record startup failure in a field visible to callers,
- avoid reporting success when the listener did not actually bind.

Preferred direction:

- change the lifecycle API so startup can be awaited and can fail deterministically.

Even if the public API remains synchronous for now, it should not silently swallow bind failures.

### D. Add proper disposal at the server/factory boundary

Possible options:

1. make `ITransportServerFactory` implement `IDisposable` (and possibly `IAsyncDisposable` later), then have `WitServer.Dispose()` dispose it;
2. or add an explicit transport-factory shutdown method invoked from `WitServer.Dispose()`.

Without this, transports that own OS handles will keep leaking lifecycle responsibility.

## Minimal upstream regression tests to add

### 1. WebSocket server start-stop-start on same endpoint in same process

Scenario:

- create WebSocket `WitServer` on `http://127.0.0.1:{port}/api/test/`;
- `StartWaitingForConnection()`;
- connect a client successfully;
- stop and dispose the server;
- create a **new** server on the same endpoint in the same process;
- `StartWaitingForConnection()` again;
- verify client can connect again.

This is the closest transport-level reproduction of the current blocker.

### 2. Hosted restart with gateway + dynamic nested endpoint

Scenario:

- start gateway server on `/api/`;
- create dynamic per-client server on `/api/{clientId}/`;
- verify initial connection;
- stop/dispose everything;
- recreate gateway and recreate dynamic per-client server in the same process;
- verify reconnect to `/api/{clientId}/` succeeds.

This mirrors the `OutWit.Cloud` production path more closely.

### 3. Startup failure is observable

Scenario:

- intentionally keep a prefix occupied,
- start a new `WebSocketServerTransportFactory` for the same prefix,
- verify startup failure is surfaced clearly rather than only faulting a background task.

## Diagnostic logging worth adding temporarily in WitRPC

While fixing upstream, add temporary logs around:

- listener instance creation and disposal;
- full prefix string used by `HttpListener`;
- `Listener.Start()` success/failure;
- `StopWaitingForConnection()` calling `Stop`/`Close`;
- accept-loop exit reason;
- number of active transports disposed on shutdown.

That should quickly confirm whether old listeners survive host restart.

## Short conclusion

The most probable broken spot is not `ClientPoolManager` itself but the WebSocket transport lifecycle in WitRPC:

- `WebSocketServerTransportFactory.StopWaitingForConnection()` does not release `HttpListener`;
- `WitServer.Dispose()` does not dispose the transport factory;
- `StartWaitingForConnection()` can fail silently because listener start happens in fire-and-forget background code;
- existing upstream tests already contain a comment acknowledging WebSocket restart/port-release issues.

So the upstream fix should focus first on **real listener shutdown + restart-safe listener recreation + observable startup failure**.

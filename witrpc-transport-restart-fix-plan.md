# WitRPC transport restart fix plan

> **CLOSURE (2026-06-10)**: Fixed and shipped — `173744a` (stale-frame hardening, 2026-03-20), `8b8501c` (restart-safe transport lifecycle, `ITransportServerFactory : IDisposable`, synchronous bind failure, 2026-04-22), `cb4a68d` (empty-response crash fix, 2026-04-23); regression-tested via `TransportFactoryLifecycleTests`, `WitServerTransportEdgeCaseTests`, `WitClientIncomingPayloadTests`. Remaining: REST server (`WitServerRest`) lifecycle not hardened; the broad-batch test run (last checkbox below) is still unchecked. See CHANGELOG.md sections 2.3.2/2.3.3. Note: this file is still untracked in git.

## Goal

Fix restart-related transport lifecycle issues in `WitRPC`, cover them with regression tests, and verify that equivalent problems do not exist in other server transports.

## Progress

- [x] Investigate the upstream report and confirm the current failure points in `WebSocket` transport lifecycle.
- [x] Add a persisted implementation plan to the repository and keep progress updated.
- [x] Add a disposal contract for `ITransportServerFactory` and wire it into `WitServer.Dispose()`.
- [x] Rework `WebSocketServerTransportFactory` to use restart-safe listener lifecycle management.
- [x] Rework `TcpServerTransportFactoryBase` to use restart-safe listener lifecycle management.
- [x] Audit and harden `NamedPipeServerTransportFactory` stop/restart behavior.
- [x] Audit and harden `MemoryMappedFileServerTransportFactory` stop/restart behavior.
- [x] Add regression tests for `start -> stop/dispose -> recreate -> start` on the same endpoint/name/port for all server transports.
- [x] Add a server-level test to verify `WitServer.Dispose()` stops and disposes its transport factory.
- [ ] Run relevant transport tests in small batches and confirm the workspace build.

## Notes

### Confirmed issues

- `WebSocketServerTransportFactory` creates `HttpListener` in the constructor and does not release it on `StopWaitingForConnection()`.
- `TcpServerTransportFactoryBase` has the same lifecycle shape with `TcpListener`.
- `WitServer.Dispose()` currently disposes active transports but does not dispose the transport factory itself.
- `NamedPipe` and `MMF` factories also rely on fire-and-forget loops and need restart verification.

### Implementation order

1. Lifecycle contract: `ITransportServerFactory` + `WitServer.Dispose()`.
2. `WebSocket` listener lifecycle fix.
3. `Tcp` listener lifecycle fix.
4. `NamedPipe` / `MMF` restart hardening.
5. Regression tests.
6. Build + test validation.

## Validation log

- [x] `run_build` completed successfully after the lifecycle changes.
- [x] Run `WitServerTransportEdgeCaseTests` separately.
- [x] Run `TransportFactoryLifecycleTests` separately.
- [ ] Run a small existing regression slice after that if the targeted tests stay stable.

## Current validation status

- Targeted lifecycle/disposal validation is green.
- Additional broad regression slices through the Visual Studio test runner are unstable in this workspace and can hang even when the targeted transport tests pass.
- Further validation should stay at the level of single test classes or exact fully-qualified tests only.

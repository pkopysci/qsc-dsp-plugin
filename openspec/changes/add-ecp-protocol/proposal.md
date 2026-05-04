# M-ECP — External Control Protocol backend

## Why

QRC is the modern Q-SYS control protocol; ECP is the legacy fallback shipped on every Q-SYS Core since v8.x. The framework consumer chooses which the plugin speaks based on what the deployed Core supports — older Cores, hostile-network deployments where TLS-less ASCII is the only option through the firewall, and integrators who already have ECP-only tooling all need ECP. Today the plugin only speaks QRC; M-ECP closes that gap.

ECP is a strict subset of QRC. It can do named-control get/set, status, and snapshot recall. It **cannot** enumerate components, do matrix crosspoint addressing by index, or expose EngineStatus pushes for redundancy switchover. M-ECP records those gaps explicitly and the plugin logs `Logger.Notice` ("requires QRC; ignored") when a feature lands on the wrong wire — rather than silently doing nothing.

The protocol is selected automatically by port: `1710` → QRC (the M2-M7 path, unchanged), `1702` → ECP. Zero new public-API surface.

## What changes

1. **ECP protocol stack** (new `Protocol/Ecp/` namespace):
   - `EcpFramer` — line-based framer. Outbound: append `\n`. Inbound: split on `\n`, strip optional trailing `\r`. Transport-level shape mirrors `QrcFramer`.
   - `EcpCommand` / `EcpResponse` value types covering the §3 command set.
   - `EcpDispatcher` — equivalent of `JsonRpcDispatcher`. Routes `cv` / `cvv` / `cmv` / `cmvv` lines, `sr` (status), `cgpa` (poll ack), and the unsolicited `login_required` / `core_not_active` notifications.
   - `EcpQuoting` — handles the four-character escape table (`\n`, `\r`, `"`, `\`).

2. **Connection layer reuse**: a new `EcpConnectionAdapter` plugs into the existing M2 `ConnectionManager` state machine via the same `IConnectionTransport` seam. The `CommandQueue` carries a small `IWireRequest` abstraction so it can hold either `JsonRpcRequest` or `EcpCommand`. **Decision point**: see `design.md` §D-E1 — we keep the queue typed on `JsonRpcRequest` for v1.0 and ship a parallel `EcpCommandQueue` instead of widening every M3-M5 service to talk a new abstract type.

3. **Backend selection at `Initialize`**:
   - `QscDspTcp.Initialize(host, 1710, ...)` → QRC stack (unchanged).
   - `QscDspTcp.Initialize(host, 1702, ...)` → ECP stack.
   - Custom ports require an explicit hint via the existing `SetCredentials`-style API; default `Initialize` infers from the well-known port pair.

4. **Service-tier routing under ECP**:
   - **Audio control** (level/mute/preset): supported via `csv` / `csp` and `ssl`.
   - **Audio routing (matrix)**: ECP cannot address a matrix crosspoint by index. The router service detects the ECP backend and (a) honours operations whose `routerTag` resolves to a named matrix-crosspoint control, (b) logs `Logger.Notice` ("matrix-by-index requires QRC; control attempted") and returns false otherwise.
   - **Audio zones**: same treatment — supported when the zone control is a named control, not when the integration relies on a component-resolved control.
   - **Logic triggers**: `ct CONTROL_ID` covers it cleanly.
   - **Redundancy**: ECP exposes `IS_ACTIVE` via `sg` periodically, but no asynchronous EngineStatus push. The redundant-pair coordinator polls `sg` every 2 s on each side under ECP. `core_not_active` errors trigger an immediate failover. `SetBackupDeviceConnection` works the same way; the policy tier is unchanged.

5. **Authentication**:
   - On connect, accept a possible `login_required` banner. If credentials were configured via `Initialize`, reply with `login NAME PIN`.
   - Treat `login_failed` (followed by socket close) as a fatal error — surface as `Logger.Error` and reschedule reconnect with the existing 15 s cadence.
   - Anonymous mode (no `login_required`) skips the login step.

6. **Keepalive**: ECP closes idle sockets after 60 s. The keepalive timer sends `sg` every 30 s of outbound silence (matches the QRC `NoOp` keepalive cadence). Doubles as a periodic redundancy probe.

7. **`FakeEcpServer`** in `TestSupport/Fakes/` — symmetric to `FakeQrcServer`. Accepts ECP commands, replies with documented response shapes, supports `EmitMalformed()` / `RespondWithStandbyError()` knobs.

8. **Tests**:
   - Unit: `EcpFramerTests`, `EcpQuotingTests`, `EcpDispatcherTests`, `EcpCommandSerializerTests`. Property tests on the framer's split/strip behaviour and on quoting round-trip.
   - Integration: `EcpFakeServerEndToEndTests` with the new `FakeEcpServer`. Mirror the M2 `FakeServerEndToEndTests` happy paths.
   - Service-tier integration: protocol-parameterised tests that run the M3-M5 happy paths over ECP for the supported subset and assert the documented `Logger.Notice` for unsupported ops.

9. **`SPEC_COMPLIANCE.md` row 3.1 flips from "QRC complete; ECP scoped follow-on" to "✅".** New deviation **D-E1** records the parallel-queue choice. New deviation **D-E2** records the ECP feature subset (matrix-by-index N/A, EngineStatus N/A; periodic `sg` poll substitutes).

## Impact

- **New public API surface**: zero. Protocol selection is by port, no new methods or types are `public`.
- **Internal surface growth**: ~12 new types under `Protocol/Ecp/`, plus `EcpConnectionAdapter` and `EcpCommandQueue`. All `internal` — `PublicSurface.expected.txt` should not change.
- **DLL size budget**: estimate +25–40 KB for the new types (current budget headroom: 388 KB / 500 KB). Comfortable.
- **Risk**: the parallel-queue choice (D-E1) means the redundancy coordinator's `RoutingCommandQueue` does not extend to ECP — a redundant pair MUST run both sides on the same protocol (both QRC or both ECP). Mixed-protocol pairs are out of scope.

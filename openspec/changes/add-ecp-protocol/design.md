# M-ECP Design — External Control Protocol backend

## D-E1: Parallel queue vs widened abstraction

**Problem.** M3-M5 services (`AudioControlService`, `AudioRoutingService`, `AudioZoneEnableService`, `LogicTriggerService`) hold a `CommandQueue` and call `TryEnqueue(JsonRpcRequest)`. ECP commands are not JSON-RPC. To run those services over ECP we either (a) widen the abstraction so every service speaks an `IWireRequest` (or similar), or (b) ship a parallel `EcpCommandQueue` and a parallel set of services.

**Options.**

1. **Wide abstraction.** Define `IWireRequest` (or `record WireRequest(string Method, object? Params, long Id)`). Update every service signature, every test fixture, every service constructor. The QRC backend translates `IWireRequest` → `JsonRpcRequest`; the ECP backend translates → `EcpCommand`. Maintains a single service tier.
2. **Parallel queue.** Keep `CommandQueue<JsonRpcRequest>` for QRC. Ship `EcpCommandQueue` for ECP. Service tier stays untouched on the QRC path. The ECP path duplicates the small subset of service-tier code that needs to translate registry calls into wire commands.
3. **Service-tier wrapper that adapts at call time.** The service tier still talks `JsonRpcRequest`; an `IWireAdapter` (per-protocol) intercepts the queue at the `TryEnqueue` boundary and rewrites JSON-RPC into ECP. Single service tier; new translation layer.

**Decision.** **Option 2 — parallel queue + thin parallel service-translation tier.**

**Rationale.**
- Option 1 is the cleanest in steady state but the largest M-ECP PR by far. Every M3-M5 test fixture changes shape. The risk of regressing the QRC happy paths (which are the protocol every customer actually uses) is high relative to the user-visible benefit of shared code.
- Option 3 introduces a translation layer that has to understand the semantics of every QRC method (`Control.Set` → `csv`, `Snapshot.Load` → `ssl`, `ChangeGroup.AutoPoll` → `cgs`, etc.) yet still has to fall back to "N/A under ECP" for the gaps. The translation layer ends up as complex as just shipping a parallel service tier, with the added drawback that it's invoked per-message (hot-path) rather than once at construction.
- Option 2 isolates the ECP code. The QRC service tier — by far the more featureful path — stays exactly as it is in M2-M7. The duplication is small: `EcpAudioControlService`, `EcpAudioRoutingService`, `EcpAudioZoneEnableService`, `EcpLogicTriggerService`, each ~50–80 LOC because ECP's command set is shallower than QRC's.

**Tradeoff acknowledged.** A future feature added to QRC won't automatically appear in ECP. M-ECP-2 (if it happens) can revisit option 1 once we know which features actually need parity. For v1.0 the explicit gap list under D-E2 is more honest than a translation layer that pretends to bridge.

**Reversibility.** Reversible. Going from parallel-queue to wide-abstraction is a rename + interface introduction; the service-tier signatures are local to this codebase.

## D-E2: ECP feature subset and the N/A list

**Problem.** ECP cannot do everything QRC does. The plugin's public surface implements the full `IDsp` / `IAudioRoutable` / `IAudioZoneEnabler` / `IRedundancySupport` / `IDspLogicTriggerSupport` contract. When the integrator picks ECP and calls a method that ECP can't service, the plugin must do *something* coherent — silently no-op (bad), throw (worse), or log + return a documented fallback (the M3 pattern from when the bodies were stubs).

**Decision.** Match the M3 stub-and-log pattern.

For each service operation:

| Operation | ECP support | Fallback when unsupported |
|-----------|-------------|---------------------------|
| `SetAudioOutputLevel` (named gain) | ✅ via `csv` | n/a |
| `SetAudioOutputMute` (named mute) | ✅ via `css` (boolean as `"true"`/`"false"`) | n/a |
| `SetAudioInputLevel` / `SetAudioInputMute` | ✅ same | n/a |
| `RecallAudioPreset` | ✅ via `ssl BANK INDEX` | n/a |
| `AddAudioPreset` | ✅ stored in `AudioChannelRegistry`; emits `ssl` on recall | n/a |
| `SetAudioRoute` (named-control router) | ✅ via `csv` on the resolved router-tag | n/a |
| `SetAudioRoute` (matrix index) | ❌ ECP cannot address matrix crosspoints by index | `Logger.Notice("matrix-by-index requires QRC; operation refused"); return false`. Documented in the method's XML doc. |
| `SetAudioZoneEnable` (named zone control) | ✅ via `csv` | n/a |
| `SetAudioZoneEnable` (component-resolved) | ⚠ depends on whether the integration registered a named-control alias | If no alias, `Logger.Notice` + refuse |
| `PulseDspLogicTrigger` | ✅ via `ct CONTROL_ID` | n/a |
| `IRedundancySupport.PrimaryDeviceActive` | ✅ via 2 s `sg` polling per side | n/a |
| `RedundancyStateChanged` event | ✅ raised when polling detects an `IS_ACTIVE` flip | n/a |
| `BackupDeviceConnectionChanged` event | ✅ raised on TCP up/down (same as QRC) | n/a |
| AutoPoll-based cache hydration | ✅ via `cgsna` (no-ack) every 2 s on a `cgc` group containing every registered control | n/a |
| AutoPoll delta callbacks | ✅ via the same `IAutoPollSubscription` seam M3 introduced; ECP `cv` lines route through a thin shim | n/a |

**Reversibility.** The N/A list is documented in `SPEC_COMPLIANCE.md` as deviation D-E2. Future work could fill the gaps by either (a) extending ECP support if QSC adds matrix-by-index, or (b) refusing ECP at `Initialize` for designs that need the missing features. For v1.0 the log-and-refuse pattern is consistent with how the framework already expects unsupported operations to behave.

## D-E3: Redundancy under ECP

**Problem.** QRC redundancy (M6) hangs on `EngineStatus` async pushes. ECP has no equivalent push; the only signal is `sg`'s `IS_ACTIVE` field, which the client must poll.

**Decision.** Poll `sg` every 2 s on each side under ECP. Translate the `IS_ACTIVE` field into the same `EngineState.Active` / `EngineState.Standby` values that `EngineStatusObserver` produces under QRC. The pair coordinator (`RedundantConnectionPair`) is unchanged — it consumes `EngineState` transitions, not protocol-specific frames.

The 2 s poll cadence matches the QRC AutoPoll cadence (M3) so that switchover latency is comparable. Under heavy load the poll competes with the keepalive `sg` (30 s) — duplicates are harmless because `sg` is idempotent.

**Tradeoff.** Polling adds steady-state traffic that QRC's push model avoided. 2 s × ~50 bytes per probe × 2 sides = ~200 bytes/sec — not material on any modern Q-SYS network.

## D-E4: Connection layer reuse

**Problem.** M2's `ConnectionManager` was built around `JsonRpcDispatcher` and `CommandQueue<JsonRpcRequest>`. We want to reuse:

- The connect / connecting / connected / disconnecting / disconnected state machine
- The reconnect cadence (15 s)
- The keepalive timer (currently QRC `NoOp`)
- The transport seam (`IConnectionTransport`)

without the JsonRpc-specific assumptions.

**Decision.** Refactor `ConnectionManager` to be generic over the message + dispatcher pair. Constraint: the M2-M7 callers' constructor signature is `public sealed class ConnectionManager` taking `CommandQueue` and `JsonRpcDispatcher`. We don't want to break that signature (it's in the public surface snapshot).

**Implementation.** Introduce `ConnectionManager<TRequest, TDispatcher>` as an internal generic base. Make `ConnectionManager` (the existing public class) a sealed thin wrapper that closes the generic with `<JsonRpcRequest, JsonRpcDispatcher>` and forwards every member. Add `EcpConnectionManager` as a sibling that closes with `<EcpCommand, EcpDispatcher>`. Both inherit (or compose — final shape decided in the implementation slice) the same state machine.

**Alternative considered.** Duplicate the entire ConnectionManager file as `EcpConnectionManager.cs`. Less elegant but preserves isolation: any change to QRC's connection machinery doesn't accidentally regress ECP. Given the M3+M6 history of post-connect-chain races, isolation has paid off. Final implementation may favour duplication-by-copy over generics if the abstraction cost grows.

## D-E5: Authentication

**Problem.** ECP's `login_required` banner is pushed *before* any client command in some designs, and *after* the first command in others (the v8.2 docs say after; v9.10 docs say "immediately on connect or on the first command"). The client can't predict which.

**Decision.** Prime the read pump immediately after the TCP handshake; if the first inbound line is `login_required`, send `login NAME PIN` from the credentials supplied at `Initialize`. If credentials are absent, log `Logger.Error("ECP Core requires login but no credentials configured")` and let the reconnect cycle eventually close out. If `login_failed` arrives, the Core will close the socket — surface as a fatal-but-recoverable error that triggers the M2 reconnect.

For anonymous Cores (no `login_required` banner), the connection proceeds straight to ready state.

**Reversibility.** Reversible. Auth-state is contained in `EcpConnectionAdapter`.

## D-E6: Quoting

**Problem.** ECP escapes `\n`, `\r`, `"`, `\` inside quoted strings. Forgetting an escape on a control name with a space → `bad_id`. Forgetting an escape on a `css` value with embedded quotes → silent corruption.

**Decision.** Centralize escaping in `EcpQuoting` with `EscapeForString(string)` and `UnescapeFromString(string)`. Property test: `Unescape(Escape(x)) == x` for any UTF-16 string. Every `EcpCommand.ToWire()` and every `EcpResponse.Parse(string)` go through these helpers.

## What we explicitly will NOT do in M-ECP

- Mixed-protocol redundant pairs (QRC primary + ECP backup or vice versa). Both sides MUST use the same protocol. Documented in proposal §Impact.
- TLS for ECP. The Core does not support it.
- ECP debug port 1703. Developer-only per QSC docs.
- Component enumeration via ECP. Not possible.
- Bringing `CHANGELOG.md` up to date for M-ECP — that goes in the milestone's archive commit per project convention, not in the proposal scaffold.

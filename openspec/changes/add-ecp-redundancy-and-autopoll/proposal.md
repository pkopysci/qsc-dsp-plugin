# M-ECP part 3 — AutoPoll + redundancy

## Why

M-ECP-part-2 shipped ECP single-Core with full IDsp / IAudioRoutable / IAudioZoneEnabler / IDspLogicTriggerSupport coverage, but explicitly deferred two pieces that the spec promised:

1. **AutoPoll-driven cache hydration.** Today the ECP service tier maintains an *optimistic* cache: `Set` updates the local value and fires the change event before the Core acknowledges. Reads (`GetAudio*Level`, `Query*`) return the optimistic value. If the Core rejects (`bad_id`, `control_read_only`, `core_not_active`) or the value is clamped server-side, the cache disagrees with the wire.
2. **Redundancy under ECP.** `SetBackupDeviceConnection` against an ECP primary refuses with `Logger.Notice` deferring to part-3. ECP exposes `IS_ACTIVE` only through `sg` poll responses; there's no asynchronous push equivalent to QRC's EngineStatus.

This milestone closes both. Once it ships, ECP and QRC paths are at functional parity for everything the framework surface exposes.

## What changes

1. **`EcpHydrateAction`** — runs after auth completes. Builds a single change group via `cgc 1`, registers every named control from `AudioChannelRegistry` + `AudioZoneRegistry` + `LogicTriggerRegistry` via `cga 1 "tag"`, then starts a 2-s schedule via `cgsna 1 2000`.

2. **`EcpAutoPollSubscription`** — subscribes to `EcpDispatcher.ResponseReceived`, filters `cv` lines, and translates each into the M3 `AudioControlServiceFanout.Dispatch` callback shape so the existing service tier picks up Core-observed deltas without knowing the protocol.

3. **`EcpAudioControlService` cache reconciliation** — the optimistic-on-Set behaviour is preserved (the framework expects synchronous cache updates), but when an inbound `cv` reports a value that disagrees with the optimistic cache, the cache is corrected and the change event is re-fired with the Core's authoritative value. Same applies to mute, route, and zone-enable.

4. **`EcpEngineStateProbe`** — schedules an `sg` every 2 s on each connection, parses the `sr` reply, translates `IS_ACTIVE=1` → `EngineState.Active` / `IS_ACTIVE=0` → `Standby`, and feeds the existing M6 `RedundantConnectionPair` coordinator unchanged.

5. **`EcpRedundantConnectionPair`** — parallel coordinator typed on `EcpConnectionManager` + `EcpCommandQueue`. Same shape as M6's `RedundantConnectionPair`: subscribes both managers to the engine-state probe, runs the M6 `SwitchbackPolicy` unchanged, owns its own `RoutingCommandQueue`-equivalent (`EcpRoutingCommandQueue`).

6. **`QscDspTcp.SetBackupDeviceConnection`** — same-protocol ECP pairs now construct the `EcpRedundantConnectionPair` instead of refusing. Mixed-protocol refusal stays.

7. **Tests** — `EcpHydrateActionTests`, `EcpAutoPollSubscriptionTests`, `EcpEngineStateProbeTests` (unit). `EcpRedundancyEndToEndTests` (integration: two `FakeEcpServer` instances, drive `IS_ACTIVE` on each side via `SetActive`, assert routing flips through `RoutingCommandQueue`).

8. **Docs** — `SPEC_COMPLIANCE.md` row 3.1 final state ✅, `QUICKSTART.md` ECP section drops the "deferred" caveat on redundancy, `CHANGELOG.md` part-3 entry.

## Impact

- **Public surface:** zero net additions. All new types are `internal`.
- **DLL size budget:** estimate +15–20 KB (new ~600 LOC). Current 142.5 KB / 500 KB.
- **Risk:** the `EcpRedundantConnectionPair` is necessary duplication of M6's `RedundantConnectionPair`. Critic Pass-1 on part-2 (concern 11) flagged this — design.md §D-E5 records the choice and the path to a unified abstraction in a future milestone.
- **Final delivery:** after this milestone archives, the bet's deliverable is shipping-complete on both protocols.

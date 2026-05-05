# M-ECP part 3 — AutoPoll + redundancy

## Why

M-ECP-part-2 shipped ECP single-Core with the full `IDsp` /
`IAudioRoutable` / `IAudioZoneEnabler` / `IDspLogicTriggerSupport`
surface but left two pieces deferred:

1. **AutoPoll-driven cache hydration.** The ECP service tier today
   maintains an *optimistic* cache: `Set` updates the local value
   and fires the change event before the Core acknowledges. Reads
   return the optimistic value. If the Core rejects (`bad_id`,
   `control_read_only`, `core_not_active`) or clamps server-side,
   the cache disagrees with the wire.
2. **Redundancy under ECP.** `SetBackupDeviceConnection` against an
   ECP primary refuses with `Logger.Notice` deferring to part-3.
   ECP exposes `IS_ACTIVE` only through `sg` poll responses; there
   is no asynchronous push equivalent to QRC's `EngineStatus`.

This milestone closes both. Once it ships, the QRC and ECP paths are
at functional parity for everything the framework surface exposes.

## What changes

1. **`EcpHydrateAction`** — runs after auth completes (between the
   `Connected` transition and `_queue.StartAccepting()`). Builds a
   single change group via `cgc 1`, registers every named control
   from `AudioChannelRegistry` + `AudioZoneRegistry` +
   `LogicTriggerRegistry` via `cga 1 "tag"`, then schedules a 2-s
   poll via `cgsna 1 2000`.
2. **`EcpAutoPollSubscription`** — subscribes to
   `EcpDispatcher.ResponseReceived`, filters `cv` lines, and
   translates each into the M3 `AudioControlServiceFanout.Dispatch`
   callback shape so the existing service tier picks up
   Core-observed deltas without knowing the protocol.
3. **`EcpAudioControlService` cache reconciliation** — the
   optimistic-on-Set behaviour is preserved (the framework expects
   synchronous cache updates), but when an inbound `cv` reports a
   value that disagrees with the optimistic cache, the cache is
   corrected and the change event is re-fired with the Core's
   authoritative value.
4. **`EcpEngineStateProbe`** — schedules an `sg` every 2 s, parses
   the `sr` reply, translates `IS_ACTIVE=1` → `EngineState.Active`
   / `IS_ACTIVE=0` → `Standby`, and feeds the M6
   `RedundantConnectionPair` coordinator through the same
   `EngineState` consumer the QRC `EngineStatusObserver` uses.
5. **Same-protocol ECP redundancy** — `QscDspTcp.SetBackupDeviceConnection`
   for an ECP primary now constructs the redundant pair instead of
   refusing with `Logger.Notice`. The pair coordinator is reused
   unchanged; only its inputs (the per-side connection-manager,
   queue, and `EngineState` source) differ.
6. **Tests** — `EcpHydrateActionTests`, `EcpAutoPollSubscriptionTests`,
   `EcpEngineStateProbeTests` (unit). `EcpRedundancyEndToEndTests`
   (integration: two `FakeEcpServer` instances, drive `IS_ACTIVE`
   on each side via the existing `SetActive` knob, assert routing
   flips).
7. **Docs** — `SPEC_COMPLIANCE.md` row 3.1 final-state notes
   removed; `QUICKSTART.md` ECP section drops the "deferred" caveat
   on redundancy; `CHANGELOG.md` part-3 entry; aspirational coverage
   gate evaluation.

## Impact

- **Public surface:** zero net additions. All new types are `internal`.
- **DLL size:** estimate +15–20 KB (~600 LOC). Current 142.5 KB / 500 KB.
- **Risk:** the redundant-pair plumbing reuses `RedundantConnectionPair`
  via duck-typing on its inputs. No coordinator changes; only the
  QscDspTcp wiring differs.
- **Final delivery:** after this milestone archives, the bet's
  deliverable is shipping-complete on both protocols.

# Tasks — M-ECP

## 1. ECP wire layer

- [ ] 1.1 `Protocol/Ecp/EcpQuoting.cs` — `Escape(string)` / `Unescape(string)` covering `\n`, `\r`, `"`, `\` per ECP §1.3. Property test: `Unescape(Escape(x)) == x` for arbitrary strings.
- [ ] 1.2 `Protocol/Ecp/EcpFramer.cs` — outbound: append `\n`. Inbound: split on `\n`, strip optional trailing `\r`. Per `QrcFramer` shape: incremental parse over `ReadOnlySpan<byte>`, max-frame guard, `FrameTooLargeException` reuse.
- [ ] 1.3 `Protocol/Ecp/EcpCommand.cs` — discriminated union (record hierarchy) covering `sg`, `cg`, `csv`, `css`, `csp`, `cspr`, `csvr`, `csvv`, `cspv`, `cssv`, `ct`, `cgc`, `cgs`, `cgsna`, `cgp`, `cgpna`, `cga`, `cgr`, `cgd`, `ssl`, `login`. `ToWire()` produces the bytes; constructor validates.
- [ ] 1.4 `Protocol/Ecp/EcpResponse.cs` — discriminated union covering `sr`, `cv`, `cvv`, `cmv`, `cmvv`, `cgpa`, `login_required`, `login_success`, `login_failed`, `core_not_active`, `bad_id`, `control_read_only`, plain-error lines. `TryParse(string)` static factory.
- [ ] 1.5 `Protocol/Ecp/EcpDispatcher.cs` — equivalent of `JsonRpcDispatcher`. Routes pending commands to their replies via correlation by the next-response convention (ECP has no request id; commands and responses interleave on the wire one-at-a-time per server constraint). Surfaces unsolicited `login_required`, `cv`/`cvv` poll lines, and error notifications via events.

## 2. ECP connection adapter

- [ ] 2.1 `Connectivity/Ecp/EcpCommandQueue.cs` — same shape as `Protocol/CommandQueue.cs` but typed on `EcpCommand`. Standard Dispose pattern, `IsAccepting`, `StartAccepting`, `Drain`, `DequeueAsync`, `TryEnqueue`, `SnapshotPending`.
- [ ] 2.2 `Connectivity/Ecp/EcpConnectionManager.cs` — copy-by-design from `ConnectionManager.cs` (per design.md §D-E4 final decision: duplicate over generics). Reuses `ReconnectStrategy` and `ThreadCensus`. Three steady-state task-loop registrations: `session`, `send`, `keepalive`. Keepalive sends `sg` every 30 s of outbound silence.
- [ ] 2.3 `Connectivity/Ecp/EcpConnectionManager` post-connect chain: read first line; if `login_required`, send `login NAME PIN` from supplied credentials; on `login_failed` log Error + drop connection; on `login_success` or anonymous-mode silence, proceed.
- [ ] 2.4 `Connectivity/Ecp/EcpHydrateAction.cs` — equivalent of `HydrateChangeGroupAction`. Builds a single change group via `cgc` then registers every named control via `cga` and starts a 2 s `cgsna` poll.
- [ ] 2.5 `Connectivity/Ecp/EcpAutoPollSubscription.cs` — implements `IAutoPollSubscription` so the M3 `AudioControlServiceFanout` can route deltas without knowing about the protocol.

## 3. Service-tier (parallel, per D-E1)

- [ ] 3.1 `AudioControl/Ecp/EcpAudioControlService.cs` — translates `Set/GetAudioOutputLevel`, `Set/GetAudioOutputMute`, etc., into `csv` / `css` commands against the registered named controls.
- [ ] 3.2 `AudioControl/Ecp/EcpAudioRoutingService.cs` — supports `SetAudioRoute` only when `routerTag` resolves to a named control. Matrix-by-index calls log `Logger.Notice("matrix-by-index requires QRC; operation refused")` and return false.
- [ ] 3.3 `AudioControl/Ecp/EcpAudioZoneEnableService.cs` — same shape as the QRC service but emits `csv` for boolean controls.
- [ ] 3.4 `LogicTriggers/Ecp/EcpLogicTriggerService.cs` — `Pulse(id)` → `ct CONTROL_ID`.
- [ ] 3.5 Audio-preset support: `RecallAudioPreset(id)` → `ssl BANK INDEX`, where `BANK` and `INDEX` come from the existing `AudioPreset` registry.

## 4. Backend selection at Initialize

- [ ] 4.1 `QscDspTcp.Initialize`: detect protocol from port — `1710` → QRC (M2-M7 path, unchanged); `1702` → ECP. Other ports default to QRC with a `Logger.Notice("non-standard port; assuming QRC — call SetEcpProtocol() if this is an ECP Core")`. Out of scope: explicit `SetProtocol()` API (kept internal for M-ECP-2).
- [ ] 4.2 `QscDspTcp.BuildConnectionResources` (currently QRC-only) gains a sibling `BuildEcpConnectionResources`. The construction switch is local to `Initialize` and `BuildRedundantPair`.
- [ ] 4.3 `QscDspTcp` event surface (`AudioOutputLevelChanged`, `RedundancyStateChanged`, etc.) routes from whichever service is wired. No public-API change.

## 5. Redundancy under ECP (per D-E3)

- [ ] 5.1 `Connectivity/Ecp/EcpEngineStateProbe.cs` — periodic `sg` poll, every 2 s. Maps `IS_ACTIVE=1` → `EngineState.Active`; `0` → `Standby`. Reuses the existing `RedundantConnectionPair` coordinator unchanged.
- [ ] 5.2 Mixed-protocol pairs are refused at `SetBackupDeviceConnection`: if backup port is 1702 and primary is 1710 (or vice versa), `Logger.Error("redundant pair must use same protocol on both sides; call refused")`.

## 6. FakeEcpServer

- [ ] 6.1 `TestSupport/Fakes/FakeEcpServer.cs` — symmetric to `FakeQrcServer`. TCP listener on a random port, accepts ECP commands, parses, replies with documented response shapes.
- [ ] 6.2 Fault-injection knobs: `EmitMalformed()` (next reply ignores quoting rules), `RespondWithCoreNotActive()` (every command returns `core_not_active`), `RequireLogin(name, pin)` (sends `login_required` banner).
- [ ] 6.3 `GetReceivedFrames()` returns parsed commands for assertions in integration tests.

## 7. Tests

- [ ] 7.1 Unit: `EcpFramerTests`, `EcpQuotingTests`, `EcpCommandSerializerTests`, `EcpResponseParserTests`, `EcpDispatcherTests`, `EcpEngineStateProbeTests`, `EcpHydrateActionTests`.
- [ ] 7.2 Property: `EcpQuotingProperties` — escape/unescape round-trip; `EcpFramerProperties` — split-on-LF preserves payload regardless of `\r` placement.
- [ ] 7.3 Integration: `EcpFakeServerEndToEndTests` — connect, hydrate, set + get a named control, recall a preset.
- [ ] 7.4 Integration: protocol-parameterised happy-path tests for the supported subset of M3-M5 features over ECP.
- [ ] 7.5 Integration: `EcpRedundancyEndToEndTests` — primary + backup over ECP with `FakeEcpServer` instances; failover triggered by changing one side's `IS_ACTIVE` reply.
- [ ] 7.6 Integration: assert `Logger.Notice` is emitted for every documented N/A operation under ECP (matrix-by-index `SetAudioRoute`, etc.).

## 8. Documentation

- [ ] 8.1 `ARCHITECTURE.md` — append M-ECP section: command-set cheat sheet, protocol-selection diagram, deviation links.
- [ ] 8.2 `SPEC_COMPLIANCE.md` row 3.1 flips to ✅. New deviation rows D-E1 and D-E2 added.
- [ ] 8.3 `QUICKSTART.md` — add an ECP wiring snippet (port 1702 in `Initialize`); add a "Feature support by protocol" table.
- [ ] 8.4 `CHANGELOG.md` — M-ECP entry on archive.

## 9. Build, format, coverage, size gates

- [ ] 9.1 `dotnet build -c Release`: 0 warnings, 0 errors.
- [ ] 9.2 `dotnet format --verify-no-changes`: clean.
- [ ] 9.3 `dotnet test`: full matrix green per-suite; 5 consecutive runs.
- [ ] 9.4 Local merged line coverage ≥ 90 % (the M2-set floor; M-ECP doesn't raise the gate).
- [ ] 9.5 DLL size (`-c Release`) ≤ 500 KB. Track delta from M7 baseline (112 KB).
- [ ] 9.6 `openspec validate add-ecp-protocol --strict`: passes.
- [ ] 9.7 Run `qsc-critic` agent locally; address blockers; record Pass 1 + Pass 2 in `REVIEW.md`.
- [ ] 9.8 Public surface snapshot: should not change. Verify `PublicSurfaceTests` is green without a snapshot edit.

## 10. Commit + PR + archive

- [ ] 10.1 Commit incrementally per top-level task group.
- [ ] 10.2 Open PR against `main`.
- [ ] 10.3 After merge, `openspec archive add-ecp-protocol --yes`.

# Tasks — add-redundancy

## 1. Engine-status observation

- [ ] 1.1 `Connectivity/Redundancy/EngineStatusObserver.cs` — subscribes to a `JsonRpcDispatcher.NotificationReceived` event; on every notification whose method is `EngineStatus`, parses `result.State` (Active / Standby / Idle) and forwards to a registered callback.
- [ ] 1.2 Tolerate malformed pushes (missing State, unknown values) — log `Logger.Warn` and ignore.

## 2. Switchback policy

- [ ] 2.1 `Connectivity/Redundancy/SwitchbackPolicy.cs` — small immutable record `{ bool RespectQscFailoverGuidance }`. Default `false` (per README D3).
- [ ] 2.2 Decision function: given `(currentActiveSlot, slotStates)`, return the new active slot. Pin the README behaviour and the QSC-guidance behaviour with separate tests.

## 3. Routing command queue

- [ ] 3.1 `Connectivity/Redundancy/RoutingCommandQueue.cs` — facade with the same public `TryEnqueue(JsonRpcRequest)` shape as `Protocol/CommandQueue`. Holds an `_activeQueue` reference under `_lock`. Forwards every enqueue.
- [ ] 3.2 When `_activeQueue is null`, `TryEnqueue` returns `false` and logs `Logger.Error` ("no active Core; refusing send").
- [ ] 3.3 `SetActive(CommandQueue?)` swaps under the lock; emits `Logger.Notice` on transition.

## 4. Redundant connection pair

- [ ] 4.1 `Connectivity/Redundancy/RedundantConnectionPair.cs` — owns two `ConnectionManager` instances (`Primary`, `Backup`). Tracks last observed `EngineStatus.State` per slot.
- [ ] 4.2 `Connect()` starts both managers; `Disconnect()` stops both; `Dispose()` disposes both.
- [ ] 4.3 Subscribes `EngineStatusObserver` on each manager's dispatcher.
- [ ] 4.4 On State change: ask `SwitchbackPolicy` for the new active slot; if changed, swap `RoutingCommandQueue.SetActive` and re-subscribe the AutoPoll fanout to the new manager's `ChangeGroupManager`. Fire `RedundancyStateChanged`.
- [ ] 4.5 Implements 4 properties: `PrimaryDeviceActive`, `BackupDeviceActive` (mutually exclusive; both false when no Core is active), `BackupDeviceOnline` (backup TCP up), `BackupDeviceExists` (`SetBackupDeviceConnection` was called).
- [ ] 4.6 Fires `BackupDeviceConnectionChanged` when the backup's `ConnectionState` flips into / out of `Connected`.

## 5. QscDspTcp wiring

- [ ] 5.1 `SetBackupDeviceConnection(hostname, port)` — stash the backup config; on a subsequent `Connect()`, construct the pair instead of the single manager.
- [ ] 5.2 Replace the M2-stub bodies for the 4 `IRedundancySupport` properties with delegate-to-pair (returning the M2 single-Core defaults — `PrimaryDeviceActive = IsOnline`, `BackupDeviceExists = false`, etc. — when the pair is null).
- [ ] 5.3 Forward `RedundancyStateChanged` and `BackupDeviceConnectionChanged` events from the pair. Remove the CS0067 suppression for both.
- [ ] 5.4 No-backup deployments (Initialize without SetBackupDeviceConnection): unchanged path; the M5 stack runs as-is.

## 6. Tests — unit

- [ ] 6.1 `EngineStatusObserverTests` — happy-path State extraction (Active / Standby / Idle); malformed push (missing State, unknown value) silently logged.
- [ ] 6.2 `SwitchbackPolicyTests` — README behaviour (switchback to primary on its return-to-Active); QSC-guidance behaviour (sticky-on-backup); double-Active tie break; double-Standby returns "no active".
- [ ] 6.3 `RoutingCommandQueueTests` — TryEnqueue forwards to active; null-active returns false + logs; SetActive swap is observable.
- [ ] 6.4 `RedundantConnectionPairTests` — using two `StubTransport` instances: failover on State change; switchback on primary return; backup-online event raise; properties reflect current state.

## 7. Tests — integration (xUnit + two FakeQrcServer instances)

- [ ] 7.1 `Failover_from_primary_to_backup_when_primary_pushes_Standby` — both servers up, primary EngineStatus pushes Standby, writes route to backup.
- [ ] 7.2 `Switchback_to_primary_when_it_returns_to_Active` — primary pushes Standby then Active again; writes return to primary.
- [ ] 7.3 `BackupDeviceConnectionChanged_fires_on_backup_TCP_up_and_down` — kill backup TCP and observe the event; restore and observe again.
- [ ] 7.4 `Writes_during_double_Standby_window_are_refused_with_log` — both servers push Standby, framework Set call logs error and refuses.

## 8. Documentation

- [ ] 8.1 `ARCHITECTURE.md` — add a redundant-pair state-machine diagram and the failover sequence.
- [ ] 8.2 `SPEC_COMPLIANCE.md` — discharge rows 5.5 (redundancy + switch-back), 7.2 (lost-connection switch-to-backup), and the two `IRedundancySupport` event rows.
- [ ] 8.3 Update SPEC_COMPLIANCE row 4.1 (3-thread budget) to note the per-pair doubling and document the deviation.

## 9. Build, format, and review gates

- [ ] 9.1 `dotnet build`: 0 warnings, 0 errors (Debug + Release).
- [ ] 9.2 `dotnet format --verify-no-changes`: clean.
- [ ] 9.3 `dotnet test`: full matrix green, 3 consecutive runs, no flakes.
- [ ] 9.4 Coverage on `QscDspDevices.dll`: ≥ 90 % line.
- [ ] 9.5 DLL size (`-c Release`): ≤ 500 KB.
- [ ] 9.6 `openspec validate add-redundancy --strict`: passes.
- [ ] 9.7 Run `qsc-critic` agent locally; address blockers.

## 10. Commit + PR

- [ ] 10.1 Commit incrementally — observer, policy, queue facade, pair, surface wiring, tests, docs.
- [ ] 10.2 Open PR against `main`.

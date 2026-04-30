# Hardware Validation Checklist — QscDspDevices

> **Purpose.** A short, manual checklist the reviewer (or any operator)
> can run against a real Q-SYS Core (or the Q-SYS Designer emulator) to
> verify the plugin behaves as advertised on real hardware. CI cannot
> exercise this — the in-process fake QRC server stands in for unit and
> integration tests, but only this checklist demonstrates real-world
> behaviour.

## Prerequisites

- A running Q-SYS Core (firmware ≥ 9.10) **or** the Q-SYS Designer
  software with the emulator started (`Tools → Run with Emulator` —
  listens on TCP/1710 the same as a real Core).
- Network reachability to the Core on port `1710` (QRC) and `1702` (ECP).
- A Q-SYS design loaded with at least:
  - One named control of type `Gain` exposed in Designer.
  - One named control of type `Mute` exposed in Designer.
  - One named control associated with a `Snapshot Bank`.
  - (Optional) A `Router` block named control for matrix tests.
  - (Optional) A second Q-SYS Core configured as backup for redundancy
    tests.

## Test runner

A small console runner lives at
`tests/QscDspDevices.HardwareValidation/` (added in M7). It loads a
single instance of `QscDspTcp`, prints every event raised, and accepts
keyboard commands to drive the plugin. Run with:

```bash
dotnet run --project tests/QscDspDevices.HardwareValidation -- \
    --host <core-ip> --port 1710 \
    [--user <user> --password <password>] \
    [--backup <backup-ip>]
```

## Checklist

> Tick each item after running it. Each row references the README
> requirement that the test exercises (see `SPEC_COMPLIANCE.md`).

### A. Connection lifecycle

- [ ] **A.1 — Connect** (7.1)
  Start the runner pointing at a reachable Core.
  *Expect:* `IsOnline` flips to `true`, `NotifyOnlineStatus()` fires,
  one `Logger.Notice` "connected" entry, no errors.

- [ ] **A.2 — Disconnect** (7.1)
  Run `disconnect` in the runner.
  *Expect:* `IsOnline` flips to `false`, `NotifyOnlineStatus()` fires,
  one `Logger.Notice` "disconnected" entry, the command queue empty,
  the runner accepts no further commands until `connect`.

- [ ] **A.3 — Cable pull, no backup** (7.3)
  Disconnect the Core's network cable while connected.
  *Expect:* `Logger.Error` "connection lost", reconnect attempt every
  15 s. Reconnect Cable → next attempt succeeds; full state hydration
  visible in events.

- [ ] **A.4 — Refuse-while-disconnected** (8.4)
  Disconnect and immediately call `SetAudioOutputLevel`.
  *Expect:* Method returns without throwing, one `Logger.Error` "command
  attempted while disconnected", queue length stays 0.

### B. Audio control

- [ ] **B.1 — Set output level** (6.2, 8.7)
  Set output gain to 75 (0–100). *Expect:* Designer's Gain control
  shows the corresponding native value (≈ -10 dB depending on the
  configured min/max). `AudioOutputLevelChanged` fires after the value
  is mirrored.

- [ ] **B.2 — Set output mute** (6.3) *Expect:* Designer's Mute control
  flips on/off. `AudioOutputMuteChanged` fires after the value is
  mirrored.

- [ ] **B.3 — Externally-driven change** (8.6, 8.7)
  Change a control directly in Designer.
  *Expect:* `AudioOutputLevelChanged` (or Mute) fires within ~250 ms
  via the AutoPoll change group. The plugin's reported value matches
  Designer.

- [ ] **B.4 — Recall preset** (6.6)
  Run `recall <preset-id>`.
  *Expect:* Designer shows the corresponding snapshot loaded; relevant
  control change events fire.

### C. Routing (only if a router named control is configured)

- [ ] **C.1 — Set crosspoint** (5.4, 6.4)
  Route input 2 → output 3.
  *Expect:* Designer's router block reflects the change. Reading the
  routing back returns the same crosspoint.

### D. Logic triggers

- [ ] **D.1 — Pulse trigger** (3.5 IDspLogicTriggerSupport)
  Run `trigger <trigger-id>`.
  *Expect:* Designer's logic input pulses (visible in the logic
  inspector). Plugin's `LogicTriggerActivated` event fires.

### E. Redundancy (only if a backup Core is configured)

- [ ] **E.1 — Failover to backup** (5.5, 7.2, D3)
  Power down the primary Core (or pull its cable).
  *Expect:* Within ~10 s, plugin switches writes to backup. `IsOnline`
  remains `true` overall (stays online via the backup).
  `Logger.Notice` "primary lost, switching to backup" appears.

- [ ] **E.2 — Switchback to primary** (5.5, D3)
  Restore the primary Core.
  *Expect:* When `EngineStatus` reports the primary as `Active` again,
  plugin switches writes back to the primary. `Logger.Notice` "primary
  restored" appears.

### F. Negative cases

- [ ] **F.1 — Wrong credentials**
  Run with bogus `--password`.
  *Expect:* `Logger.Error` "logon failed". Plugin remains in the
  reconnect loop (does not silently send commands without auth).

- [ ] **F.2 — Bogus channel id**
  `SetAudioOutputLevel("nope", 50)`.
  *Expect:* `Logger.Warn` "channel not found", method returns, no
  command sent on the wire.

- [ ] **F.3 — DLL size**
  `ls -l QscDspDevices.dll` (Release build). *Expect:* ≤ 500 KB.

- [ ] **F.4 — Restart cycle**
  Restart the runner three times in a row.
  *Expect:* No memory growth (resident set roughly constant), no
  leaked threads, no orphan TCP connections to the Core (verify with
  `ss -tn dst <core-ip>`).

## Failure handling

If any row fails:

1. Capture the runner's `Logger` output and any `tail` of the framework
   service log on the host.
2. Open an issue in the repo with the failing row, reproducer steps,
   and the captured logs.
3. The OpenSpec change for the next milestone takes a `bug-fix` task
   citing the issue number.

## Sign-off

Add the runner output (one screenshot or log paste per failed/passed
row, summary lines suffice) to the delivery PR's description.

`Reviewer: ____________________`  `Date: ____________`

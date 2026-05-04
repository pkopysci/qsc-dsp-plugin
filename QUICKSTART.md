# QscDspDevices — Quickstart

Integrator-facing quickstart for wiring `QscDspDevices.dll` into an
AV-Framework host. The README in this repo is the **spec**; this file
is the **how-to**.

## Supported environment

- .NET 8.0
- AV Framework `gcu-hardware-service` (any released version that exposes the IDsp + IAudioRoutable + IAudioZoneEnabler + IRedundancySupport + IDspLogicTriggerSupport interfaces — surface verified against the framework-docs snapshot at the time of writing)
- Crestron RMC4 processor (production); the plugin loads identically on any .NET 8 host that loads the framework's `gcu-hardware-service`
- One QSC Q-SYS Core (or two for redundancy) reachable via TCP/1710

## Minimum wiring (single Core, no backup)

```csharp
using QscDspDevices;

// The framework's plugin loader does the construction and Initialize call.
// Below is what your manual harness or test driver would write.

var dsp = new QscDspTcp();
dsp.Id = "dsp-1";

// Initialize is called by the framework with the discovered host config.
dsp.Initialize(
    hostname: "192.168.1.50",
    port: 1710,
    username: "admin",       // omit / empty if Core accepts anonymous QRC
    password: "...");        // see SECRETS section below

// Configure controls BEFORE Connect — mid-session AddX is partial today
// (registry stages but doesn't subscribe until the next reconnect).
dsp.AddInputChannel("input1",  levelTag: "Mixer.gain.1",  muteTag: "Mixer.mute.1");
dsp.AddOutputChannel("output1", levelTag: "Output.gain.1", muteTag: "Output.mute.1");
dsp.AddDspLogicTrigger("trigger1", tagName: "Logic.button.1", tags: new());

// Subscribe to status events you care about.
dsp.AudioOutputLevelChanged   += (_, args) => Console.WriteLine($"out level: {args.Arg}");
dsp.RedundancyStateChanged    += (_, args) => Console.WriteLine($"redundancy state: {args.Arg}");
dsp.BackupDeviceConnectionChanged += (_, args) => Console.WriteLine($"backup TCP: {args.Arg}");

// Connect drives the M2 lifecycle (Connecting → Connected → hydrate).
dsp.Connect();

// Use the framework surface as normal.
dsp.SetAudioOutputLevel("output1", 50);
dsp.SetAudioOutputMute("output1", true);
dsp.PulseDspLogicTrigger("trigger1");
```

## Redundant pair (primary + backup Cores)

```csharp
var dsp = new QscDspTcp();
dsp.Id = "dsp-1";
dsp.Initialize("192.168.1.50", 1710, "admin", "...");
dsp.SetBackupDeviceConnection("192.168.1.51", 1710);

// AddX calls land before Connect as before.
dsp.AddInputChannel("input1", "Mixer.gain.1", "Mixer.mute.1");

dsp.Connect();   // builds the RedundantConnectionPair and starts both managers

// Routing follows EngineStatus pushes:
//   - Default policy (README): switches back to Primary when it returns to Active
//   - QSC-recommended (sticky): stays on the current active until it goes Standby
// To choose the QSC variant today, pre-construct the pair via the
// internal RedundantConnectionPair ctor — exposed via InternalsVisibleTo
// for tests; not on the public surface for v1.0.
```

## Threading contract

- ≤ 3 plugin-owned threadpool tasks per `ConnectionManager` (`session`, `send`, `keepalive`). The receive path is event-driven on the framework's `BasicTcpClient` callback (not plugin-owned).
- A redundant pair runs two managers simultaneously, so the per-pair count is ≤ 6.
- All public methods on `QscDspTcp` return synchronously. `Task` /`async` is internal-only.

Per-state-change events fire **after** the state transition is observable (i.e. `PrimaryDeviceActive` is already true by the time `RedundancyStateChanged` fires). This matches the AV Framework's "set IsOnline THEN call NotifyOnlineStatus" rule.

## Deviation summary

| Code | Description | Where recorded |
|------|-------------|----------------|
| **D1** | Audio level scaling: framework uses `[0, 100]`, QRC uses device-native ranges. Translation in `LevelScaler` with ±1 round-trip tolerance. | `SPEC_COMPLIANCE.md` row 6.1 |
| **D2** | Cache initialisation: framework expects synchronous "loaded" state at `Initialize` time, QRC requires a live socket. We populate from the post-connect hydration and surface stale values until the first AutoPoll cycle completes. | `SPEC_COMPLIANCE.md` row 7.x |
| **D3** | Switchback policy: README requires switch-back-to-primary; QSC's official guidance is sticky-on-current. We default to the README behaviour, with `SwitchbackPolicy.QscRecommended` available for the QSC reading. | M6 archive, `SPEC_COMPLIANCE.md` row 7.2 |
| **D-T1** | Threading shape: workers are long-running `Task` loops on the threadpool, not OS `Thread` instances. The literal README rule is "≤ 3 concurrent threads"; the prior M2-spec interpretation prescribed `Thread`-trio + role names + RunSessionAsync removal — those were elaborations beyond the README and M7 walks them back. | `SPEC_COMPLIANCE.md` row 4.1 |
| **D-T2** | Per-pair threading budget doubles to ≤ 6 in redundant deployments (3 per side). | `SPEC_COMPLIANCE.md` row 4.1 |

## Logging

All errors, warnings, notices, and debug messages route through `gcu_common_utils.Logging.Logger` with category `LogServiceTypes.Hardware` / `LogDeviceTypes.Dsp` and the supplied device id. The plugin never writes to `Console`, `Trace`, or any other sink.

Outbound `Logon` payloads are redacted via `RedactingDebugFormatter` before any `Logger.Debug` formatting — the `password` field is replaced with `***` for log capture; on-wire bytes are unchanged.

## ECP (legacy fallback)

Modern Q-SYS Cores speak QRC on TCP/1710. Older Cores and hostile-network deployments where TLS-less ASCII is the only option through the firewall can use the ECP backend on TCP/1702. Selection is automatic by port:

```csharp
// QRC (default — modern Cores)
dsp.Initialize("192.168.1.50", 1710, "admin", "...");

// ECP (legacy / firewall-restricted)
dsp.Initialize("192.168.1.50", 1702, "admin", "...");
```

Public surface is identical. The plugin internally routes every call to either the QRC or ECP backend based on the port supplied at `Initialize`.

### ECP feature support

| Feature | QRC | ECP |
|---------|-----|-----|
| `Set/GetAudioInputLevel` (named gain) | ✅ | ✅ via `csv` |
| `Set/GetAudioOutputMute` (named mute) | ✅ | ✅ via `css` |
| `RecallAudioPreset` | ✅ | ✅ via `ssl` |
| `RouteAudio` (named-control router) | ✅ | ✅ via `csv` on `routerTag` |
| `SetAudioZoneEnable` (named zone control) | ✅ | ✅ via `css` |
| `PulseDspLogicTrigger` | ✅ | ✅ via `ct` |
| `SetBackupDeviceConnection` redundancy | ✅ EngineStatus push-based | ⚠ same-protocol pairs deferred to M-ECP-part-3 (sg-poll-based) |

Mixed-protocol pairs (one side QRC, the other ECP) are refused with `Logger.Error` per the `redundancy` capability spec.

## What v1.0 does NOT include

- Mid-session `AddInputChannel` / `AddOutputChannel` / `AddPreset` subscribe-on-the-wire. Today the registry add is staged and applied at next hydration. Configuration before `Connect()` works as advertised.
- Per-symbol `public` → `internal` reduction. The current public surface is wider than necessary because tests reference internals directly.
- ECP redundant-pair via `sg`-polling (deferred to M-ECP-part-3).

## Filing issues

Public PRs and issues live at <https://github.com/pkopysci/qsc-dsp-plugin>. The OpenSpec history under `openspec/changes/archive/` is the authoritative record of what shipped per milestone, including critic Pass-1 + Pass-2 reviews.

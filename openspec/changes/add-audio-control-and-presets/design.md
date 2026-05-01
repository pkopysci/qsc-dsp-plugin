# Design — Add Audio Control (level / mute) and Snapshot Presets

## Context

M2 finished with a connection-and-command-pipe that does nothing the
end user sees. The framework calls `Initialize` → `Connect` and the
plugin does come online (IsOnline = true, NotifyOnlineStatus()), but
every `Set/Get` audio call returns the documented `NotSupported` no-op.

M3 is the milestone that turns the plugin into something useful. It
has three concrete goals:

1. Implement the 12 methods + 4 events of `IAudioControl` so that the
   Crestron framework's audio scenes can drive a Q-SYS Core through
   our plugin.
2. Subscribe to the right change-group / AutoPoll machinery so the
   framework events fire when the Core's state changes (mute toggled
   from a touchpanel, level scrolled by another control system, etc.)
   without our plugin polling.
3. Honour the README's 3-thread budget literally for the first time.
   M2's single threadpool task was a stand-in; M3 lays down the actual
   send / receive / timer trio that every later milestone will share.

## Goals / Non-Goals

**Goals**
- `IAudioControl` (12 methods, 4 events) implemented end-to-end against
  the FakeQrcServer integration suite.
- `Control.Set`, `Control.Get`, and `Snapshot.Load` glued through the
  M2 `JsonRpcDispatcher` and `CommandQueue`.
- A single AutoPoll change group keeping the cache in step with the
  Core, recreated on every reconnect.
- `Logon` post-connect action when credentials are configured;
  credential redaction in the framer's debug-log path.
- Three named threads — `qsc-send`, `qsc-recv`, `qsc-timer` — each
  registered with `ThreadCensus`. The threadpool task M2 used is
  removed.
- README §"Volume / Mute / Preset" requirements discharged in
  `SPEC_COMPLIANCE.md`.

**Non-Goals**
- Matrix routing, audio zones, logic triggers, redundancy, ECP
  (separate milestones).
- Persisting the audio cache across plugin restarts (Crestron framework
  manages persistence above us).
- Configurable AutoPoll rate, configurable change-group strategy
  (one group is sufficient up to and including M5; M6 may need a
  redundancy-specific group).
- Snapshot save / Snapshot.Save (read-only milestone for snapshots).
- Configurable `Ramp` on `Snapshot.Load` (the framework surface has no
  ramp parameter; sending no `Ramp` field tells the Core to use 0).

## Architecture

```
              QscDspTcp (public)            <-- IDsp + IAudioControl entry points
                  │
                  │ Set/Get/Recall/Add* called synchronously by framework
                  ▼
        AudioControlService                  <-- in-process orchestration
        ┌───────────────┐                       (no I/O; talks to dispatcher)
        │ cache (id→val)│                    holds the latest known framework
        └───────────────┘                    level / mute per channel id
                  │
                  │ enqueue Control.Set / Control.Get / Snapshot.Load
                  ▼
            CommandQueue          ──── drained by the qsc-send thread
                  │
                  ▼
            JsonRpcDispatcher     ──── responses & AutoPoll routed by
                  │                       qsc-recv thread
                  ▼
        ChangeGroupManager        ──── handles AutoPoll deltas; calls
                  │                       AudioControlService.OnDeviceUpdate
                  ▼
        AudioControlService.OnDeviceUpdate
                  │
                  ▼ updates cache, fires the 4 IAudioControl events on
                    the framework-expected thread (BaseDevice convention)
        IAudioControl events to the framework
```

The Send / Receive / Timer threads correspond directly to the README
§4 budget. They are real `Thread` instances, not `Task.Run` jobs, so
that `ThreadCensus` registration matches the OS-level thread for the
lifetime of the connection. The `RunSessionAsync` method from M2 is
gone — `ConnectionManager.Connect()` now starts the trio and returns
immediately; `Disconnect()` signals them to stop and joins (with a
5-second deadline borrowed from M2's existing dispose path).

## Key design decisions

### D-1: Cache reads, write-through writes

`Get*` reads from the in-process cache. `Set*` writes to the cache
optimistically (so the next `Get*` returns the new value) AND enqueues
the corresponding `Control.Set` to the Core. If the wire write fails,
the AutoPoll subscription will eventually correct the cache when the
Core reports its real state.

Rationale: `IAudioControl.Get*` returns `int`/`bool` synchronously;
blocking on a round-trip would take the framework's audio thread out
of action for a network RTT. Optimistic-write keeps the framework
responsive at the cost of brief inconsistency on a wire failure (which
AutoPoll closes within 250 ms anyway).

Trade-off accepted: `Set/Get` race during a transient disconnect can
return a stale value. Pass-3 of the critic should specifically look
for whether this can corrupt event semantics (it should not — the
event is fired on cache *change*, not on `Set` call).

### D-2: 0–100 ↔ device-native scaling lives in `LevelScaler`

The framework `IAudioControl` works in the integer range 0–100. The
device works in whatever native range `AddInputChannel` /
`AddOutputChannel` declared via `levelMin` / `levelMax`. QSC's docs
say `Control.Set Value` is a number with a per-control native range
that may be in dB (`-100..0`), normalized (`0.0..1.0`), or
device-defined integer counts (`0..50`).

`LevelScaler` is a pure utility:

```csharp
public static double ToDevice(int framework0to100, int min, int max);
public static int    ToFramework(double deviceValue, int min, int max);
```

with these guarantees (FsCheck-pinned):

- `ToFramework(ToDevice(f, min, max), min, max) == f` for every
  `f ∈ [0, 100]` and every `min < max`.
- Out-of-range framework input clamps; logs `Logger.Warn` once per
  channel id (not per call — log spam suppression via a HashSet).
- The result is rounded half-up, not half-to-even, because the QSC
  Core uses half-up rounding internally and we want the wire-level
  value to match the user's intent symmetrically.

### D-3: Single AutoPoll change group named `qsc-plugin-state`

Per `research/QRC_PROTOCOL.md` §5, change-group `Id` is a free-form
string that creates the group on first reference. We pick a fixed
name so reconnect logic can always recreate the same group.
AutoPoll rate is 250 ms (4 Hz). On Disconnect we explicitly call
`ChangeGroup.Destroy` so the Core doesn't keep the group around for
the next plugin instance — but if the TCP socket dies hard, the Core
GCs the group on its own.

The change-group teardown intentionally runs synchronously on the
qsc-recv thread before the threads are joined. The send thread is
quiesced first (no more outbound writes), then the recv thread sends
the destroy, then both stop.

### D-4: Logon redaction at the framer-debug log path only

Production `Logger.Notice` / `Logger.Warn` / `Logger.Error` paths
never log full request bodies — they log method names and error
codes. The only path that ever logs a `Logon` payload is
`Logger.Debug` (off by default at runtime, on for diagnostics). The
redacting formatter is therefore **only** wired into the
debug-log path; it does not alter request encoding or the wire
format. Tests pin: `Logon` request body's `Password` field is
replaced with `"***"` in the captured log line; everything else
(JsonRpc id, method, User) is preserved verbatim.

### D-5: 3-thread layout — explicit ownership

| Thread | Owns | Reads from | Writes to |
|---|---|---|---|
| `qsc-send` | outbound socket writes | `CommandQueue` (Reader.WaitToReadAsync) | `_transport.Send(byte[])` |
| `qsc-recv` | inbound framing + dispatch | `_transport.RxBytesReceived` event | `JsonRpcDispatcher`, `ChangeGroupManager` |
| `qsc-timer` | keepalive cadence + reconnect interval | `IQrcClock` (system in prod, deterministic in tests) | `CommandQueue.TryEnqueue(NoOp)` (keepalive); reconnect signal handed to `ConnectionManager` |

Lock-order rule (enforced in code review): no thread takes more than
one lock at a time, and no thread blocks waiting on another thread's
lock. The CommandQueue uses a single internal lock; the dispatcher
uses ConcurrentDictionary; the change-group manager uses a single
lock for its subscription map. There is no path where two of these
locks would be held simultaneously.

### D-6: `Snapshot.Load` ignores response

`IAudioControl.RecallAudioPreset(string id)` is `void`-returning
and has no error channel back to the framework. We send
`Snapshot.Load` as a request (with id) and log `Logger.Notice` on
the response or `Logger.Error` on a non-success error response. The
framework gets no notification of recall failure; this is consistent
with how `Set*` behaves when the device rejects a value, and with
the framework spec which gives us no surface to surface failures
through.

## Risks

1. **AutoPoll burst overwhelms the receive thread.** A 250 ms
   AutoPoll on a large change group could push tens of KB per tick.
   The receive thread parses each frame and calls into the change-
   group manager which raises framework events. The events are raised
   synchronously on the receive thread to honour the README's "no
   public async" rule, so a slow framework subscriber can back-press
   the receive thread. Mitigation: time-budget the receive thread
   per tick (drop log on overshoot, don't drop frames). Re-evaluate
   at M5+.
2. **Logon races with the first AutoPoll subscribe on reconnect.**
   We submit Logon and the change-group subscribe as separate JSON-
   RPC calls to the dispatcher. If the Core processes them in
   reverse order on a slow link, the subscribe could fail with code
   10 (Logon required). Mitigation: the subscribe action waits on
   the Logon response before issuing the subscribe (the dispatcher
   already supports id correlation; this is a 2-line ordering
   constraint).
3. **Cache-vs-wire skew during sustained disconnect.** A long
   disconnect window plus framework-driven `Set` calls grows the
   "client thinks X but server has Y" gap. We accept this: the
   reconnect's first AutoPoll response replays current device state
   and corrects every cached value. The framework will see one
   batch of "Changed" events on reconnect; this is the desired
   behaviour.

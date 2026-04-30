# Q-SYS External Control Protocol (ECP) — Research Report

**Project:** qsc-dsp-plugin (.NET 8 plugin implementing both QRC and ECP)
**Doc role:** Wire-format ground truth for the real ECP client *and* the in-memory fake ECP server we will ship for tests.
**Last updated:** 2026-04-29
**Primary sources:** QSC Q-SYS Help v9.10 (current), v9.8, v8.2, plus two reference implementations.

> **TL;DR for impatient readers** — ECP is an ASCII, line-based, TCP-only protocol on **port 1702**. Commands end with **`<LF>` (0x0A)**; responses end with **`<CR><LF>`**. No TLS. No mandatory login (only required when the Core is configured for it). Maps cleanly to "named-control get/set/poll" and snapshot recall — but **cannot enumerate components, cannot do raw-component crosspoint addressing, cannot do mixer crosspoint by index**. For everything beyond named controls, you need QRC. **Recommendation: default to QRC; expose ECP only as a fallback transport for hostile/legacy networks.** See section 6.

---

## Table of contents

1. [Transport & framing](#1-transport--framing)
2. [Authentication / session](#2-authentication--session)
3. [Command set (full wire-format reference)](#3-command-set-full-wire-format-reference)
4. [Mapping to our `IAudioControl` needs](#4-mapping-to-our-iaudiocontrol-needs)
5. [Failure modes](#5-failure-modes)
6. [Recommendation: which protocol to default to](#6-recommendation-which-protocol-to-default-to)
7. [Method index (full table)](#7-method-index)
8. [Version differences across firmware](#8-version-differences-across-firmware)
9. [Reference implementations consulted](#9-reference-implementations-consulted)
10. [Citations](#10-citations)

---

## 1. Transport & framing

### 1.1 TCP port

| Purpose | Port | Notes |
|--------|------|-------|
| ECP — ASCII control | **1702/TCP** | Default. Confirmed across v8.2, v9.0, v9.3, v9.5, v9.8, v9.10. [1][2][3] |
| ECP — debug | **1703/TCP** | Alluded to in the overview ("debugging with command/response logging") but QSC publishes very little about it. Treat as developer-only. [1] |
| QRC — JSON-RPC | 1710/TCP | For comparison. [3] |

### 1.2 Line termination — **this is the single most important fact**

From the official command reference (verbatim):

> *"All commands and responses end with the End of Message (EOM) character. The EOM character is `<LF>` (0x0A)."*
> *"The Core will ignore a `<CR>` (0x0D) character if it precedes the EOM."*
> *"The Core terminates all responses with `<CR><LF>`."*
> — `ECP_Commands.htm`, Q-SYS Help v9.8/v9.10 [4][5]

Practical implication for the .NET client:

- **When sending:** terminate every command with **`\n`** (single LF).
- **When receiving:** parse on **`\n`** boundaries and **strip a trailing `\r`** if present. Do not treat bare `\r` as a delimiter.
- **When implementing the fake server:** accept either `\n` or `\r\n` from clients; always emit `\r\n` to match Core behavior.

Confirmed by reference implementation: `Maxx105/QSYS_Node_Core_Control` writes `` `${title} ${name} ${val}\n` `` to the socket on port 1702 — single LF, no CR. [9]

### 1.3 Encoding

ASCII. The official spec calls the protocol "ASCII" in the port table [3] and "Unicode" in the security-scope table [12], which is contradictory but in practice means: **7-bit-clean ASCII for command tokens; UTF-8 byte-safe for the payloads of double-quoted strings.** All command keywords (`csv`, `cga`, …) are lowercase ASCII letters.

Special characters inside double-quoted string arguments are escaped: [4]

| Char | Wire form |
|------|-----------|
| `\n` (0x0A) | `\\n` |
| `\r` (0x0D) | `\\r` |
| `"` | `\"` |
| `\` | `\\` |

Example: `css text "multi\\r\\nline"`

### 1.4 Streaming long responses

ECP doesn't have a length-prefix or chunked encoding. Long responses are simply **a sequence of `<CR><LF>`-terminated lines**. The client keeps reading until it sees a terminating sentinel for the operation:

- For `cgp` / `cgsna` polls: one or more `cv` / `cvv` / `cmv` / `cmvv` lines, then **`cgpa`** as the explicit "end of poll" marker (acknowledged variant).
- For `cgpna` / `cgsna` (no-ack variants): the client must rely on time-based grouping or its own cadence — there is **no end marker**.
- For all other commands: at most one response line, which arrives within a single `recv`.

> *"Change Group Poll … in response the Core responds with the state of every control in the Change Group that has changed since the last poll, if any, followed by the Change Group Poll Ack (cgpa) response."* [13]

### 1.5 TLS support

**None.** The Q-SYS security-scope reference is explicit:

> *"Q-SYS External Control Protocol | Protocol: Unicode | Transport: TCP | Port: 1702 | Encryption: None"* — `Scope_Protocols.htm` v9.10 [12]

If we need encryption, the only options offered by the Core are:
- HTTPS for the Media Management API (port 443).
- QRC over a separate VPN/tunnel.

For this plugin: **document loudly that ECP is plaintext** and warn integrators against routing it across untrusted networks.

### 1.6 Keep-alive

> *"An External Control client must communicate with the Q-SYS Core at least once every 60 seconds, or the socket connection will be closed by the Core."* [1][4]

There is **no dedicated keep-alive command**. The recommended approach (v8.2 docs, repeated in later versions): [3]

> *"Most client programs poll a Change Group at a much higher rate which serves as a keep-alive. If not, the client program can issue a 'Status Get' command periodically, or a 'Control Set Value' on an unused control if no return response is desired."*

Strategy for our client:
- If a change group is scheduled with `cgs` at ≤ 30 s, that's enough.
- Otherwise, send `sg` every 30 s. It produces a single `sr` response we can also use to verify the design hasn't changed under us.

---

## 2. Authentication / session

### 2.1 When auth is required

Auth is **only required** when the Q-SYS Administrator has been configured with one or more users that have "External Control" privileges. Out-of-the-box, ECP is anonymous. [4]

The Core signals required auth by sending:

```
login_required
```

…immediately on connection or on the first command. The client must then issue:

### 2.2 Login command

```
login NAME PIN<LF>
```

- `NAME`: user account name (case-sensitive, ASCII, no quoting unless it contains spaces — then double-quote).
- `PIN`: numeric or alphanumeric PIN.

Example wire (client→core):
```
login Joe 1234\n
```

Possible responses:

| Response | Meaning | Connection state |
|----------|---------|-------------------|
| `login_success\r\n` | Auth OK | Stays open |
| `login_failed\r\n` | Wrong creds | **Core closes the socket** [4] |
| `login_required\r\n` | Sent before auth, in reply to any other command | Stays open, waiting for `login` |

### 2.3 Anonymous mode

When no user is configured, the Core simply accepts commands immediately after the TCP handshake — no banner, no welcome line. The first command the client sends gets a normal response.

### 2.4 Session lifetime

A session is the TCP connection itself. There is no logout command — closing the socket ends the session. All change groups created on a connection are **auto-destroyed when the connection drops**. [14]

---

## 3. Command set (full wire-format reference)

Conventions in the table below:
- All commands sent by the client end with `<LF>`.
- All responses from the Core end with `<CR><LF>`.
- `CONTROL_ID` may be quoted (`"My Control"`) if it contains spaces.
- Strings are double-quoted; numbers are bare.

### 3.1 Status & session

#### `sg` — Status Get

```
sg<LF>
```

Response:
```
sr "DESIGN_NAME" "DESIGN_ID" IS_PRIMARY IS_ACTIVE<CR><LF>
```

Example: [4]
```
> sg
< sr "MyDesign" "NIEC2bxnVZ6a" 1 1
```

Fields:
- `DESIGN_NAME` — quoted string, design as named in Designer.
- `DESIGN_ID` — quoted string, opaque per-design ID; *changes when the design is reloaded* — useful as a "design version" guard.
- `IS_PRIMARY` — `0` or `1`. In a redundant pair, only the primary is `1`.
- `IS_ACTIVE` — `0` or `1`. The standby Core in a redundant pair returns `IS_ACTIVE=0` and rejects writes with `core_not_active`.

#### `login` — see §2.2.

### 3.2 Control get

#### `cg` — Control Get

```
cg CONTROL_ID<LF>
```

Response is a `cv` (scalar) or `cvv` (vector) line, see §3.4.

Example: [3]
```
> cg gain1
< cv "gain1" "-100dB" -100 0
```

Errors: `bad_id "CONTROL_ID"`.

### 3.3 Control set

All set commands return a `cv` echo (so the client gets the actual clamped/coerced value back) **except** the ramp variants, which return nothing. [3]

| Command | Syntax | Example | Notes |
|---------|--------|---------|-------|
| `csv` | `csv CONTROL_ID VALUE` | `csv id4 6.2` | Set by raw value |
| `css` | `css CONTROL_ID STRING` | `css id4 5.0db` | Set by display string ("5.0db", "true", etc.) |
| `csp` | `csp CONTROL_ID POSITION` | `csp gain1 1` | Position 0.0–1.0 |
| `cspr` | `cspr CONTROL_ID POSITION RAMP_SEC` | `cspr id4 0.7 5` | **No response** |
| `csvr` | `csvr CONTROL_ID VALUE RAMP_SEC` | `csvr id4 6.2 5` | **No response** |
| `csvv` | `csvv CONTROL_ID N V1 V2 … Vn` | `csvv id4 4 7.0 2 3.2 5.3` | Vector value set; no response |
| `cspv` | `cspv CONTROL_ID N P1 P2 … Pn` | `cspv id4 3 0.2 0.5 0` | Vector position set |
| `cssv` | `cssv CONTROL_ID N "S1" "S2" … "Sn"` | `cssv textbox14 4 "one" "two" "dog" "cat"` | Vector string set |
| `ct` | `ct CONTROL_ID` | `ct play` | Trigger a button-style control |

Errors: `bad_id`, `control_read_only`, `core_not_active`.

> Note on ramp variants: docs explicitly state *"cspr does not return a 'cv' response of the final value"* and *"csvr … requires subsequent `cg` command to verify final value"*. [3] Implication for our client: after a ramp, do not block on `cv`; either fire-and-forget or follow with a `cg`.

### 3.4 Control value responses

#### `cv` — scalar control value

```
cv "CONTROL_ID" "DISPLAY_STRING" VALUE POSITION<CR><LF>
```

Example:
```
cv "gain1" "-100dB" -100 0
```

#### `cvv` — vector control value

```
cvv "CONTROL_ID" N "S1" "S2" … "SN" N V1 V2 … VN N P1 P2 … PN<CR><LF>
```

Each of the three sections (strings, values, positions) is prefixed by its count (always identical N).

Example: [3]
```
cvv "meter1" 2 "-100dB" "-100dB" 2 -100 -100 2 0 0
```

#### `cmv` / `cmvv` — control metadata responses

These describe *metadata aspects* (range, choices, color, indeterminate, invisible, disabled, legend) rather than the value itself. They are returned by `cgm` and may also appear unsolicited inside a `cgp` poll when metadata changes. [3]

```
cmv "CONTROL_ID" ASPECT "STRING" VALUE POSITION<CR><LF>
cmvv "CONTROL_ID" ASPECT N "S1" … "Sn" N V1 … Vn N P1 … Pn<CR><LF>
```

Aspects (unsigned int): [3]

| ID | Name |
|----|------|
| 0 | metadata_aspect_none |
| 1 | metadata_aspect_range |
| 2 | metadata_aspect_choices |
| 3 | metadata_aspect_color |
| 4 | metadata_aspect_indeterminant |
| 5 | metadata_aspect_invisible |
| 6 | metadata_aspect_disabled |
| 7 | metadata_aspect_legend |

Example: [4]
```
cmv "pos1" 6 "false" 0 0
cmvv "Slope" 2 4 "12 dB/Oct" "24 dB/Oct" "36 dB/Oct" "48 dB/Oct" 0 0
```

### 3.5 Change groups

A change group is a per-connection bag of controls that the Core efficiently polls and reports deltas for. **Per the v8.2 docs, a single connection may have at most 4 change groups; v9.x docs raise this implicitly to "many" but explicitly cap the *system-wide* total at 512.** [3][14] Treat 4 as the safe limit; if you exceed it, the Core may refuse with `too_many_change_groups`.

| Command | Syntax | Purpose |
|---------|--------|---------|
| `cgc` | `cgc GROUP_ID` | Create group with this 32-bit-uint ID |
| `cga` | `cga GROUP_ID CONTROL_ID` | Add a control to the group |
| `cgr` | `cgr GROUP_ID CONTROL_ID` | Remove a control |
| `cgclr` | `cgclr GROUP_ID` | Empty the group, keep group |
| `cgd` | `cgd GROUP_ID` | Destroy the group |
| `cgi` | `cgi GROUP_ID` | Invalidate — force every control to be reported on next poll |
| `cgp` | `cgp GROUP_ID` | One-shot poll, ack'd |
| `cgpna` | `cgpna GROUP_ID` | One-shot poll, **no ack** |
| `cgs` | `cgs GROUP_ID PERIOD_MS` | Schedule periodic poll, ack'd. `PERIOD_MS=0` disables. Min 30 ms. |
| `cgsna` | `cgsna GROUP_ID PERIOD_MS` | Scheduled poll, no ack |

Polled response shape (acked variant):

```
cv "ctrlA" "..." V P<CR><LF>
cv "ctrlB" "..." V P<CR><LF>
cmv "ctrlA" 6 "true" 1 0<CR><LF>      ; metadata change
cgpa<CR><LF>                            ; <-- end-of-poll sentinel
```

The no-ack variants (`cgpna`, `cgsna`) emit the same `cv`/`cvv`/`cmv`/`cmvv` lines but **omit** the trailing `cgpa`. Use only when you can group by time.

Worked example session (verbatim from docs / our reference impl, reconstructed): [3][13]

```
> cgc 1
> cga 1 gain1
> cga 1 mute1
> cgs 1 100         ; poll every 100 ms
< cv "gain1" "-100dB" -100 0
< cv "mute1" "false" 0 0
< cgpa
... 100 ms later ...
< cv "gain1" "-99.5dB" -99.5 0.0025
< cgpa
```

Errors: `bad_change_group_handle GROUP_ID`, `bad_id CONTROL_ID`, `too_many_change_groups`.

### 3.6 Snapshots

| Command | Syntax | Example |
|---------|--------|---------|
| `ssl` | `ssl BANK NUMBER RAMP_SEC` | `ssl snapshot1 2 5` |
| `sss` | `sss BANK NUMBER` | `sss snapshot1 3` |

`ssl` (Snapshot Load) recalls snapshot `NUMBER` from bank `BANK` over `RAMP_SEC` seconds. **Produces no response** on success. [3][15]
`sss` (Snapshot Save) overwrites snapshot `NUMBER` in bank `BANK` with the current state. **No response** on success.

Errors: `core_not_active`, plus generic `bad_command` if the bank/number is malformed.

> ECP has no native equivalent of QRC's `Snapshot.Load` *with named-snapshot lookup*; you must know the bank's named-control name in advance, exactly as exposed in Designer.

### 3.7 Disconnect / "rc"

The v9.8/v9.10 docs describe behavior when the 60-second silence triggers a server-side close: [4]

> *"the Core will send out a `response close` or `rc`, command and then drop the connection."*

Wire form (best inference from the doc wording — QSC has not published a worked example):
```
rc<CR><LF>
```

…immediately followed by the Core closing the TCP socket. Our client should treat *any* unexpected `rc` line as "session terminated" and trigger reconnect.

### 3.8 Errors (single index)

| Wire form | Trigger |
|-----------|---------|
| `bad_command "string"` | Unknown verb or extra args before EOM. [3] |
| `bad_id "CONTROL_ID"` | Named control doesn't exist or isn't externally exposed. |
| `bad_change_group_handle GROUP_ID` | Group not created on this connection. |
| `too_many_change_groups` | More than 4 groups attempted. |
| `control_read_only "CONTROL_ID"` | Set on a read-only control (e.g. a meter). |
| `core_not_active` | Set / snapshot / trigger to standby Core in redundant pair. |
| `login_required` | First command on an auth-required Core before login. |
| `login_failed` | Bad creds. Core closes connection. |
| `login_success` | Auth OK. |

---

## 4. Mapping to our `IAudioControl` needs

The plugin's `IAudioControl` interface (per `framework-docs/`) needs: gain, mute, snapshot recall, router crosspoint. Here is the ECP mapping with the gotchas called out.

### 4.1 Set channel gain

**Precondition:** the gain control must be exposed as a Named Control in Designer (right-click → "Script Access → External" or named-component pin).

```
csv "Channel_1_Gain" -12.0\n
```
or by display string:
```
css "Channel_1_Gain" "-12.0dB"\n
```
or by normalized position:
```
csp "Channel_1_Gain" 0.6\n
```

Echoed back as:
```
cv "Channel_1_Gain" "-12.0dB" -12 0.6\r\n
```

For ramped gain changes (preferred for live audio):
```
csvr "Channel_1_Gain" -12.0 0.5\n
```
*No response* — follow up with `cg` if you need confirmation.

### 4.2 Set mute

Same pattern, boolean control. Either:
```
csv "Channel_1_Mute" 1\n          ; 1 = muted, 0 = unmuted
```
or
```
css "Channel_1_Mute" "true"\n
```

### 4.3 Recall snapshot / preset

```
ssl "Snapshots_Main" 3 0.5\n
```

`Snapshots_Main` here is the **Snapshot Bank's named control**, not the bank's display name in Designer. You must wire this in advance.

### 4.4 Set router crosspoint

**This is the biggest ECP limitation.** Q-SYS routers (matrix mixers, signal routers) typically expose crosspoints internally as `xpoint.N.M` style addresses, but ECP cannot reach them unless each crosspoint has been *individually exposed* as a named control. Designer does not auto-expose them.

**Workarounds:**

1. **Manual exposure.** In Designer, name every crosspoint you care about (`Mtx_In3_Out2`, etc.). Then:
   ```
   csv "Mtx_In3_Out2" 1\n
   ```
2. **Designer scripting wrapper.** Add a Block Controller / Lua component that exposes a single named control like `RouteCmd` taking strings like `"3,2,1"` and parses them internally. Set via:
   ```
   css "RouteCmd" "3,2,1"\n
   ```
3. **Use QRC instead** for crosspoint operations. QRC's `Component.Set` lets you address `Mixer.xpoint.3.2` directly without per-point Designer exposure. **This is the recommended path for any non-trivial routing matrix.**

### 4.5 What ECP CAN do that QRC also does

- Get/set any Named Control (scalar, position, string, vector).
- Subscribe to changes via change groups.
- Recall and save snapshots.
- Trigger button controls.
- Read Core status (`sg`).

### 4.6 What ECP CANNOT do (vs. QRC)

| Capability | QRC | ECP |
|-----------|-----|-----|
| Enumerate components in the design (`Component.GetComponents`) | ✅ | ❌ |
| Enumerate controls of a component (`Component.GetControls`) | ✅ | ❌ |
| Address a sub-control by component path (`Mixer.xpoint.3.2`) | ✅ | ❌ — must be a Named Control |
| Get/set multiple controls atomically | ✅ (`Control.SetValues` array) | ❌ — one command per control |
| TLS / encryption | partial (via VPN or wrapper; no native TLS either, but QRC can be tunneled cleanly) | ❌ |
| PA Router / Page Manager APIs | ✅ | ❌ |
| Telephone / SIP block control | ✅ | partial (only via Named Controls) |
| ChangeGroup of arbitrary component sub-controls | ✅ | ❌ — Named Controls only |
| Loudspeaker Tuning / SC-class operations | ✅ | ❌ |
| Streaming logs / EngineStatus events | ✅ (`EngineStatus`) | partial (only `sg` poll) |
| Multi-line responses with id correlation | ✅ (JSON-RPC `id`) | ❌ — best you can do is in-order responses |
| Atomic transaction with rollback | ❌ (neither) | ❌ |

### 4.7 Plugin-design implication

Our `IAudioControl` should be **modelled around named controls** as the canonical addressing scheme so the same call site works for ECP and QRC. The QRC backend then has the *option* to short-circuit through `Component.Set` for performance, but the ECP backend always falls back to the named-control path. This is the only abstraction that spans both protocols cleanly.

---

## 5. Failure modes

### 5.1 Error responses (already enumerated in §3.8)

All errors are single-line responses with `<CR><LF>` termination. They are never wrapped in JSON — the verb is the first whitespace-delimited token.

### 5.2 Connection-drop behavior

**Causes the Core will drop us:**
1. **60 s silence** — Core sends `rc` then closes. [4]
2. **`login_failed`** — Core closes immediately after the response. [4]
3. **Design reload** — the Core will close all open ECP/QRC sessions when a new design is uploaded. The new connection's `sg` will return a different `DESIGN_ID`.
4. **Core failover** in a redundant pair — the standby promotes; clients connected to the *old* primary lose the socket. The new primary is reachable on the same IP but the connection must be re-established.
5. **Standby in redundant pair** — the standby Core does not drop the connection but rejects writes with `core_not_active`. Reads still work on `sg`/`cg`/`cgp` but values are stale.

**Causes we drop the Core:**
- TCP-level RST/timeout.
- Explicit close from our side (e.g. plugin shutdown). All change groups created on that connection are GC'd by the Core. [14]

**Reconnect strategy in our client:**
1. Treat any of (`rc`, EOF, RST, read timeout > 65 s) as "disconnected".
2. Exponential backoff starting at 500 ms, capped at 30 s.
3. On reconnect: re-issue `login` (if configured), then `sg` (capture `DESIGN_ID`), then re-create change groups, re-add controls, re-arm schedules.
4. If `DESIGN_ID` changed, surface a "design changed under us" event so the higher layer can refresh its known controls (the prior names may no longer exist).

### 5.3 Corrupt or partial input

The Core *waits for EOM before parsing*. [3] So sending `csv gain1 6.` then waiting 5 seconds then sending `2\n` is parsed as `csv gain1 6.2`. This is friendly but means any mid-command socket break leaves a half-line buffered server-side; the next reconnect starts clean (new TCP). No "abort current command" verb exists — disconnect is the only recovery.

If we send a malformed line (e.g. extra arg), we get `bad_command "<the entire line>"` and the connection stays open. [3]

---

## 6. Recommendation: which protocol to default to

> **Default to QRC. Implement ECP as a secondary transport selectable per-Core in config.**

### Why QRC by default

1. **Capability.** QRC can do everything ECP can plus component enumeration, sub-control addressing, atomic multi-set, PA Router, Snapshot.Load by name, and EngineStatus events. The set of useful audio operations that *only* QRC can do is large; the inverse set is empty.
2. **Schema.** JSON-RPC 2.0 has framed `id`-correlated responses, so we can multiplex requests without ordering hacks. ECP responses are positional and ambiguous when several `cv` lines arrive together (e.g. inside a `cgp` poll vs. as echoes of `csv` calls).
3. **Tooling.** Existing community libraries (`mexxs/pyqsys`, `VideoGameRoulette/qsys-qrc-py`, MatKlucznyk's S#P module) target QRC. Our tests and FakeServer can borrow shape from them.
4. **Future-proofing.** QSC explicitly labels QRC as "the newest and most advanced API" and ECP as "legacy" / "the original Named Control-based external control protocol… superseded by the newer Q-SYS Remote Control Protocol (QRC), though it is still supported". [3][8]

### Why still ship ECP

1. **The user explicitly asked for both.** Honor that.
2. **Some integrators are stuck.** Old SIMPL programs and Crestron 2-series modules speak only ECP; if our plugin needs to coexist with one of those on the same Core in test mode, an ECP test harness is genuinely useful.
3. **Easier debugging.** Telnet-friendly. A human with a netcat session can reproduce a control bug in seconds, which is invaluable when triaging.
4. **Smaller embedded clients** — some toy/low-resource controllers (think an ESP32-grade device) implement ECP much more cheaply than QRC's JSON parser.

### Practical config

In `appsettings.json`:

```json
"qsys": {
  "host": "core.local",
  "protocol": "qrc",          // "qrc" | "ecp"
  "port": null,                // null → 1710 for qrc, 1702 for ecp
  "auth": { "user": null, "pin": null },
  "useTls": false              // ECP cannot, QRC cannot natively — ignored unless we add a tunnel
}
```

The `IAudioControl` implementation is selected from `protocol`. Both share the named-control addressing model so user-facing config (channel names, mute names, snapshot bank names) is identical regardless of transport.

---

## 7. Method index

Single canonical table — every ECP verb our client and fake server must handle.

| Verb | Direction | Syntax | Response | Notes |
|------|-----------|--------|----------|-------|
| `login` | C→S | `login NAME PIN` | `login_success` / `login_failed` | Only when Core requires it. Failed = socket close. |
| `sg` | C→S | `sg` | `sr "name" "id" P A` | Status. Use as 30-s keepalive. |
| `cg` | C→S | `cg CTRL` | `cv` or `cvv` | Read named control. |
| `csv` | C→S | `csv CTRL VALUE` | `cv` | Set by raw value. |
| `css` | C→S | `css CTRL "STRING"` | `cv` | Set by display string. |
| `csp` | C→S | `csp CTRL POS` | `cv` | Set by position 0–1. |
| `cspr` | C→S | `cspr CTRL POS RAMP` | *(none)* | Position with ramp (s). |
| `csvr` | C→S | `csvr CTRL VAL RAMP` | *(none)* | Value with ramp (s). |
| `csvv` | C→S | `csvv CTRL N V1..Vn` | *(none)* | Vector value set. |
| `cspv` | C→S | `cspv CTRL N P1..Pn` | *(none)* | Vector position set. |
| `cssv` | C→S | `cssv CTRL N "S1".."Sn"` | *(none)* | Vector string set. |
| `ct` | C→S | `ct CTRL` | *(none)* / `bad_id` | Trigger. |
| `cgm` | C→S | `cgm CTRL` | `cmv` / `cmvv` | Get metadata aspects. |
| `cgc` | C→S | `cgc GID` | *(none)* / `bad_change_group_handle` | Create change group. |
| `cga` | C→S | `cga GID CTRL` | *(none)* / errors | Add control. |
| `cgr` | C→S | `cgr GID CTRL` | *(none)* | Remove control. |
| `cgclr` | C→S | `cgclr GID` | *(none)* | Clear group contents. |
| `cgd` | C→S | `cgd GID` | *(none)* | Destroy group. |
| `cgi` | C→S | `cgi GID` | *(none)* | Invalidate (force full report next poll). |
| `cgp` | C→S | `cgp GID` | `cv*` `cmv*` `cgpa` | One-shot poll, ack'd. |
| `cgpna` | C→S | `cgpna GID` | `cv*` `cmv*` *(no ack)* | One-shot poll, no ack. |
| `cgs` | C→S | `cgs GID PERIOD_MS` | scheduled `cv*` `cgpa` bursts | Min 30 ms. 0 disables. |
| `cgsna` | C→S | `cgsna GID PERIOD_MS` | scheduled `cv*` *(no ack)* | Min 30 ms. |
| `ssl` | C→S | `ssl BANK NUM RAMP_SEC` | *(none)* | Snapshot load. |
| `sss` | C→S | `sss BANK NUM` | *(none)* | Snapshot save. |
| `sr` | S→C | `sr "name" "id" P A` | — | Status response. |
| `cv` | S→C | `cv "ctrl" "str" V P` | — | Scalar value response/echo. |
| `cvv` | S→C | `cvv "ctrl" N "S".. N V.. N P..` | — | Vector value response. |
| `cmv` | S→C | `cmv "ctrl" ASP "str" V P` | — | Metadata response. |
| `cmvv` | S→C | `cmvv "ctrl" ASP N "S".. N V.. N P..` | — | Vector metadata response. |
| `cgpa` | S→C | `cgpa` | — | Poll-ack sentinel. |
| `rc` | S→C | `rc` | — | Server is closing the connection. |
| `bad_command` | S→C | `bad_command "..."` | — | Unparseable. |
| `bad_id` | S→C | `bad_id "ctrl"` | — | Unknown control. |
| `bad_change_group_handle` | S→C | `bad_change_group_handle GID` | — | Unknown group. |
| `too_many_change_groups` | S→C | `too_many_change_groups` | — | > 4 per connection. |
| `control_read_only` | S→C | `control_read_only "ctrl"` | — | Read-only target. |
| `core_not_active` | S→C | `core_not_active` | — | Standby Core write. |
| `login_required` | S→C | `login_required` | — | Auth needed. |
| `login_success` | S→C | `login_success` | — | Auth OK. |
| `login_failed` | S→C | `login_failed` | — | Auth fail (closes socket). |

---

## 8. Version differences across firmware

QSC's docs are remarkably consistent across v8.2 → v9.10. Notable points of drift:

| Area | v8.2 [3] | v9.0–9.5 | v9.8–9.10 [4][5] |
|------|---------|----------|-------------------|
| Vector ramp variants `cspvr` / `csvvr` | not documented | mentioned in v9.3 [16] | mentioned |
| Per-connection change group cap | "up to four" (hard) | softened wording | softened — only system-wide 512 cap is explicit, per-connection cap not stated |
| Disconnect message `rc` | not mentioned | not mentioned | explicitly mentioned ("response close or rc") [4] |
| `login_required` semantics | mentioned | mentioned | mentioned |
| Unicode encoding | called "ASCII" in protocols table | called "ASCII" | called "Unicode" in security table, "ASCII" in protocols table — likely just docs sloppiness; in practice 7-bit ASCII for tokens, byte-safe inside `"…"` strings |

**Practical guidance:** target the v9.8/v9.10 spec — it is the most explicit and current — but keep the per-connection 4-group limit as a defensive client-side cap, since older Cores genuinely enforce it.

---

## 9. Reference implementations consulted

| Repo | Lang | Coverage | Useful insights |
|------|------|---------|------------------|
| **`Maxx105/QSYS_Node_Core_Control`** [9][17] | Node.js | ECP `csv`/`css`/`csp` + QRC `Control.Set` | Confirms LF-only termination on outbound (`` `${title} ${name} ${val}\n` ``). Confirms port 1702/1710 split. Trivial implementation — useful as a smoke-test target. |
| **`MatKlucznyk/Qsys`** [10] | C# / SIMPL+ | Primarily QRC | Has a real Communications/ subsystem we can mine for connection-management patterns (TCP reconnect, heartbeat). Their primary protocol is JSON-RPC, not ECP. |
| **`mexxs/pyqsys`** [6] | Python | QRC only | JSON-RPC reference, not relevant to ECP wire format but shows the canonical change-group lifecycle that ECP must mirror. |
| **`VideoGameRoulette/qsys-qrc-py`** [7] | Python | QRC only | Same as above. |

We were unable to find a *full* open-source ECP client that exercises every verb (change groups, snapshots, metadata, vector controls). The Maxx105 sample only implements scalar set. The reference truth therefore comes from the QSC docs themselves, cross-validated against Maxx105 for the framing characters.

---

## 10. Citations

1. **ECP Overview, v9.8** — https://help.qsys.com/q-sys_9.8/Content/External_Control_APIs/ECP/ECP_Overview.htm
2. **ECP Overview, v9.5** — https://help.qsys.com/q-sys_9.5/Content/External_Control_APIs/ECP/ECP_Overview.htm
3. **Q-SYS External Control Protocol, v8.2** (most detailed legacy spec) — https://q-syshelp.qsc.com/q-sys_8.2/Content/External_Control/Q-SYS_External_Control/007_Q-SYS_External_Control_Protocol.htm
4. **ECP Commands, v9.8 (current)** — https://help.qsys.com/q-sys_9.8/Content/External_Control_APIs/ECP/ECP_Commands.htm
5. **ECP Commands, v9.10 (current)** — https://help.qsys.com/q-sys_9.10/Content/External_Control_APIs/ECP/ECP_Commands.htm
6. **`mexxs/pyqsys`** — https://github.com/mexxs/pyqsys
7. **`VideoGameRoulette/qsys-qrc-py`** — https://github.com/VideoGameRoulette/qsys-qrc-py
8. **External Control APIs Overview** — https://q-syshelp.qsc.com/Content/External_Control_APIs/External_Control_APIs_Overview.htm
9. **`Maxx105/QSYS_Node_Core_Control` — `src/index.js`** — https://raw.githubusercontent.com/Maxx105/QSYS_Node_Core_Control/master/src/index.js
10. **`MatKlucznyk/Qsys`** — https://github.com/MatKlucznyk/Qsys
11. **Q-SYS Network Services and Protocols (port table), v8.1** — https://q-syshelp.qsc.com/q-sys_8.1/Content/Networking/Protocols.htm
12. **System Scope and Security Protocols, v9.10 (TLS confirmation)** — https://help.qsys.com/q-sys_9.10/Content/Security/Scope_Protocols.htm
13. **Change Groups for ECP and QRC** — https://q-syshelp.qsc.com/Content/External_Control_APIs/external_controls_change_groups.htm
14. **Q-SYS Training: Part C — Managing Change Groups** — https://training.qsc.com/mod/book/tool/print/index.php?id=2757
15. **Snapshots schematic-library page** — https://q-syshelp.qsc.com/Content/Schematic_Library/snapshots.htm
16. **ECP Commands, v9.3** — https://q-syshelp.qsc.com/q-sys_9.3/content/external_control_apis/ecp/ECP_Commands.htm
17. **`Maxx105/QSYS_Node_Core_Control` repo** — https://github.com/Maxx105/QSYS_Node_Core_Control
18. **Test External Controls (telnet walkthrough), v9.8** — https://help.qsys.com/q-sys_9.8/Content/External_Control_APIs/ECP/Test_External_Controls.htm
19. **Application note: Testing ECP with Telnet** — https://support.qsys.com/en_US/application-notes/how-to-%7C-testing-external-control-protocol-ecp-commands-with-telnet
20. **Application note: Set up ECP on a Q-SYS Core processor** — https://support.qsys.com/en_US/application-notes/how-to-%7C-set-up-ecp-on-a-q-sys-core-processor

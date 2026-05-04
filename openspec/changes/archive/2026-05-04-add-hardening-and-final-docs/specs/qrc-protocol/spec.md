# QRC Protocol — Spec Delta (M7)

## MODIFIED Requirements

### Requirement: Logon payload is redacted in debug logs

When the plugin emits a `Logger.Debug` log of an outbound `Logon` JSON-RPC request — whether from `LogonAction`, the `CommandQueue` send-loop, or the `JsonRpcDispatcher` outbound path — the `password` field of the request's `params` block MUST be replaced with the literal string `"***"` before the message is formatted. The on-wire payload MUST NOT be modified; only the logged representation is altered.

#### Scenario: Logon debug log shows redacted password

- **GIVEN** debug logging is enabled and `LogonAction` runs with `password = "hunter2"`
- **WHEN** the outbound `Logon` request is formatted for `Logger.Debug`
- **THEN** the logged JSON contains `"password":"***"`
- **AND** the bytes written to the transport contain `"password":"hunter2"`

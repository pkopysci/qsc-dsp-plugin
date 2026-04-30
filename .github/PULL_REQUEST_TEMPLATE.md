<!--
Why this template exists:
- Every PR ties back to an OpenSpec change so the reviewer can audit the
  contract → implementation → test path in one click.
- Every PR cites the README requirement(s) it satisfies, with line numbers.
- The qsc-critic agent's findings are summarised so the human reviewer
  doesn't have to dig through CI logs.
-->

## Summary

<!-- 2–3 sentences: what changed and why. Link the OpenSpec change-id. -->

OpenSpec change: `<change-id>`

## README requirements satisfied

<!-- For each line of the README this PR addresses, cite the section + bullet. Example:
- §"Device Connection" / "wait 15 seconds, and attempt to reconnect" — implemented in `Connectivity/ReconnectStrategy.cs:42`, tested in `tests/QscDspDevices.IntegrationTests/ReconnectTests.cs:18`.
-->

## Tests added / changed

<!-- Bulleted list. Mark unit / integration / property. -->

## qsc-critic review summary

<!-- Paste the agent's verdict line and blocker count.
     Example: "Verdict: ✅ ship — 0 blockers, 2 nits addressed."
     The qsc-critic agent runs LOCALLY via Claude Code (CI has no API
     access). Open `.claude/agents/qsc-critic.md` in Claude Code and ask
     it to review the active branch before pushing. -->

## Spec compliance matrix updated?

- [ ] Yes — `SPEC_COMPLIANCE.md` rows added/updated.
- [ ] N/A — this PR doesn't touch a spec-tracked requirement.

## Build & test results

- `dotnet build` — <0/0 expected>
- `dotnet test` — <count passing>, coverage <%>
- DLL size (Release): <KB> / 500 KB budget

## Threading audit

<!-- For PRs that touch threading or shared state, answer:
     - Which of the 3 internal threads do you use?
     - What new shared state do you introduce, and what guards it?
     - Any new public async signatures? (Should be no.)
-->

## Risk and rollback

<!-- One sentence on the worst-case behaviour if this lands buggy, and how
     to revert (commit hash if obvious). -->

## Checklist

- [ ] Branch is up to date with `main`
- [ ] CI is green (build, format, tests, coverage, DLL size)
- [ ] OpenSpec proposal is approved or this PR is the proposal
- [ ] qsc-critic agent run, findings addressed or triaged
- [ ] No commented-out code, no `TODO` left in shipped library
- [ ] No password / credential strings appear in any logger call

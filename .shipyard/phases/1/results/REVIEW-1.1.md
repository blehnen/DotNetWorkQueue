# Review: Plan 1.1 (NetMQ API probe + SetCountMsg)

Two-stage review performed by separate reviewer agent dispatches.

## Verdict: PASS (with minor suggestions)

Stage 1 (spec compliance) verdict: **PASS**. Stage 2 (code quality) verdict: **MINOR_ISSUES** — nothing blocking, 0 critical, 0 important, 8 suggestions. Neither stage produced blockers.

## Stage 1 — Spec Compliance

### Checklist results

- File paths match spec: PASS
- Test file contents byte-match plan snippets: PASS
- `[Collection("NetMQ")]` attribute: PASS
- Block-scoped namespace: PASS
- No license header: PASS
- Struct declaration exact signature `internal readonly record struct SetCountMsg(int Port, long Count)`: PASS
- Struct placement inside namespace, after class close, before namespace close: PASS
- XML doc comment preserved: PASS
- No other changes to `TaskSchedulerJobCountSync.cs`: PASS
- Acceptance criteria (test pass + Release build clean): PASS
- CONTEXT-1.md decision #1 respected (no HALT): PASS
- Commit messages exact match (verified separately by orchestrator via `git log`): PASS

### Regression sentinel

`_lockSocket` count in `TaskSchedulerJobCountSync.cs` = **9** (baseline preserved).

### Stage 1 deviations

None.

## Stage 2 — Code Quality

### Critical findings

None.

### Important findings

None.

### Suggestions (deferred, non-blocking)

1. `poller.Stop()` is dead code when `Assert` fails mid-test — `using var poller` disposes anyway, so the explicit `Stop()` can be dropped or wrapped in try/finally. Minor hygiene.
2. `ManualResetEventSlim` not in a `using`. GC-safe (no kernel handle without contention) but inconsistent with nearby `using var` usage.
3. `default(SetCountMsg)` yields `Port=0, Count=0` — not a bug today (only positional ctor produces instances) but a theoretical trap if the struct is ever made public or produced via reflection.
4. `int Port` vs `ushort`: `int` is correct because it matches `System.Uri.Port` and `_hostPort` in the existing class — flagged for completeness, no action required.
5. Struct placement inside `TaskSchedulerJobCountSync.cs` is acceptable for one-field internal type; consider splitting to its own file if Phase 1 grows more wire-format types.
6. `NetMqQueueApiProbeTests` name signals "throwaway"; if it sticks around as a regression guard, rename to `NetMqQueueBehaviorTests` after Phase 1.
7. `[Collection("NetMQ")]` on `SetCountMsg_Equality_IsValueBased` is cargo-culted (that particular test touches no NetMQ primitives). Harmless; keeps class-level consistency.
8. 5-second timeout is generous for in-process NetMQ — acceptable.

### Positive findings

- Proper happens-before relationship on `received`: `signal.Set()` on poller thread → `signal.Wait()` on test thread gives correct release/acquire semantics.
- The round-trip test genuinely exercises the poller event loop — not a tautology.
- `readonly record struct` provides free value equality, already asserted.
- `internal` visibility keeps the wire type off the public API surface.
- Consistent file style with existing tests in the project.

## Decision

PLAN-1.1 is **APPROVED**. All 8 minor suggestions are deferred — none block Wave 2. Decisions 1 and 2 can be revisited in a late-phase simplification pass if they still matter; the others are style/preference calls.

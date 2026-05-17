# Build Summary: Plan 2.1 (Phase 4 Wave 2 — PostgreSQL Sync Handler Fork)

## Status: complete

## Tasks Completed

- Task 1: Modify `SendMessageCommandHandler.cs` — complete — inserted 2-line early-branch in `Handle()` after lazy-init block; appended `private long HandleExternalTx(SendMessageCommand)` (~80 lines incl. XML doc) before `CreateStatusRecord`. Self-managed-tx path unchanged.
- Task 2: Add structural smoke tests for sync fork — complete — `SendMessageCommandHandlerForkSmokeTests.cs` (120 lines, 3 [TestMethod]).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `d3462836` | 1 | `shipyard(phase-4): add HandleExternalTx fork to PostgreSQL sync handler` |
| `21993800` | 2 | `shipyard(phase-4): add structural smoke tests for PG sync handler fork` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` — MODIFIED (+101 lines)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs` — NEW (120 lines)

## Decisions Made

- **No deviations from plan.** Fork body matches PLAN-2.1.md verbatim with all three PG-specific swaps:
  1. `_getTime.GetCurrentUtcDate()` materialized as 8th argument to `CreateMetaDataRecord` (PG-specific 8-param signature)
  2. `NpgsqlDbType.Bytea` for `@body` and `@headers` parameters
  3. `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` + `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` typed queries
- **CONTEXT-4 Rule B compliance from the start.** Trailing comment uses the exact wording `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` — no method-call substrings present. Smoke test's plain source-text grep passes without preprocessing (Phase 3 PLAN-2.1 used test-side line-comment stripping; Phase 4 converges on Phase 3 PLAN-2.2's cleaner source-side rephrase).
- **`InternalsVisibleTo` already configured** in `InternalsVisibleForTests.cs` — no new visibility wiring.

## Issues Encountered

- **None.** All edits landed first try; Release build clean; all 3 smoke tests passed first run.
- Pre-existing 14 NU1902 OpenTelemetry advisory warnings (ISSUE-032) carried forward.

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| `commandSend.ExternalTransaction != null` grep | 1 | 1 |
| `private long HandleExternalTx` grep | 1 | 1 |
| `return HandleExternalTx(commandSend);` grep | 1 | 1 |
| Fork body: `_getTime.GetCurrentUtcDate()` | 1 | 1 |
| Fork body: `NpgsqlDbType.Bytea` | 2 | 2 |
| Fork body: `SqlDbType` (none) | 0 | 0 |
| Fork body: Rule B literal wording | 1 | 1 |
| Fork body: lifecycle calls `.Commit()/.Rollback()/.Close()/.Dispose()` | 0 | 0 |
| Release build PG main | 0 errors | 0 errors, 14 pre-existing NU1902 |
| 3 sync fork smoke tests | 3 pass | 3 pass, 0 fail |
| Full PG.Tests suite regression | Failed: 0 | 143 pass, 0 fail (includes PLAN-2.2 additions) |

## Disjointness from PLAN-2.2

Only `SendMessageCommandHandler.cs` + new sync smoke tests file touched. PLAN-2.2's target `SendMessageCommandHandlerAsync.cs` and async smoke tests file untouched here.

## Phase 4 Hand-off

1. PG `SendMessageCommandHandler.Handle()` dispatches to `HandleExternalTx` when `ExternalTransaction != null`. Branch placed after lazy-init block so `_messageExpirationEnabled.Value` is materialized once regardless of branch.
2. Lifecycle-ownership invariant enforced at source-text level by `HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose`. Phase 6 integration tests cover runtime side.
3. Reflection signature contract pinned by `HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature` — future renames/return-type changes break loud.
4. Combined with PLAN-2.2 (async), sync + async PG outbox path is end-to-end functional at the unit-test level.

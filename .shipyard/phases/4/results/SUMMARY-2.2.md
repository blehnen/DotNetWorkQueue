# Build Summary: Plan 2.2 (Phase 4 Wave 2 — PostgreSQL Async Handler Fork)

## Status: complete

## Tasks Completed

- Task 1: Modify `SendMessageCommandHandlerAsync.cs` — complete — inserted 3-line early-branch in `HandleAsync()` after lazy-init block; appended `private async Task<long> HandleExternalTxAsync(SendMessageCommand)` (~97 lines incl. XML doc) before `CreateStatusRecordAsync`. Self-managed-tx async path unchanged.
- Task 2: Add structural smoke tests for async fork — complete — `SendMessageCommandHandlerAsyncForkSmokeTests.cs` (3 [TestMethod]).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `7b216c42` | 1 | `shipyard(phase-4): add HandleExternalTxAsync fork to PostgreSQL async handler` |
| `d5f28102` | 2 | `shipyard(phase-4): add structural smoke tests for PG async handler fork` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` — MODIFIED (+100 lines)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs` — NEW

## Decisions Made

- **CONTEXT-4 Rule B applied verbatim from start** — lifecycle-invariant comment uses exact wording `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` Smoke test uses plain substring search, no preprocessing.
- **Sync sub-handlers in async path** — `_jobExistsHandler.Handle(...)` and `_sendJobStatus.Handle(...)` invoked sync because they have no async overload. Same pattern as the pre-existing self-managed-tx async path at lines 122 + 167. RESEARCH §3 confirmed.
- **`_getTime.GetCurrentUtcDate()` invoked directly** (not awaited) — `IGetTime` is sync. Passed as 8th argument to `CreateMetaDataRecordAsync` per PG-specific signature.
- **3 structural smoke tests only** — direct execution infeasible (sealed `NpgsqlConnection`/`NpgsqlTransaction`/`NpgsqlCommand` types + CLAUDE.md sync-vs-async mocking lesson). Runtime side covered by Phase 6 integration tests.

## Issues Encountered

- None. Parallel-edit handoff with PLAN-2.1 (sync handler sibling file) clean — files disjoint as planned.
- 14 pre-existing NU1902 OpenTelemetry advisory warnings (ISSUE-032) carried forward.

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| `commandSend.ExternalTransaction != null` grep | 1 | 1 |
| `private async Task<long> HandleExternalTxAsync` grep | 1 | 1 |
| `await HandleExternalTxAsync(commandSend).ConfigureAwait(false)` grep | 1 | 1 |
| `_getTime.GetCurrentUtcDate()` in fork body | 1 | 1 |
| `NpgsqlDbType.Bytea` in fork body | 2 | 2 |
| `SqlDbType` in fork body (sentinel) | 0 | 0 |
| `ExecuteScalarAsync()` in fork body | 1 | 1 |
| Lifecycle comment exact wording | 1 | 1 |
| Lifecycle calls in fork (8 patterns sync+async variants) | 0 | 0 |
| Release build PG main | 0 errors | 0 errors, 14 pre-existing NU1902 |
| 3 async fork smoke tests | 3 pass | 3 pass, 0 fail |
| Full PG.Tests suite regression | Failed: 0 | 143 pass, 0 fail |

## Disjointness from PLAN-2.1

Only `SendMessageCommandHandlerAsync.cs` + async smoke tests file touched. PLAN-2.1's `SendMessageCommandHandler.cs` (sync) and sync smoke tests file untouched here.

## Phase 4 Hand-off

- Both Wave 2 forks (sync via PLAN-2.1, async via PLAN-2.2) on master, file-disjoint, matching CONTEXT-4 Rule B wording exactly.
- PG async handler branches on `commandSend.ExternalTransaction != null` → `HandleExternalTxAsync(commandSend)` reusing caller's tx. Existing self-managed-tx async path unchanged.
- Phase 6 integration tests cover runtime side of lifecycle-ownership invariant.
- 143 PG.Tests pass total = 130 baseline + 7 Wave 1 (Plan 1.1) + 3 Plan-2.1 + 3 Plan-2.2 = 143. ✓

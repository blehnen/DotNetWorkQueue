# Build Summary: Plan 2.2 (Phase 3 Wave 2 — Async Handler Fork)

## Status: complete

## Tasks Completed

- Task 1: Modify `SendMessageCommandHandlerAsync.cs` — complete — inserted 2-line early-branch in `HandleAsync()` after the lazy-init block; appended `private async Task<long> HandleExternalTxAsync(SendMessageCommand)` (~97 lines incl. XML doc) before `CreateStatusRecordAsync`. Self-managed-tx path unchanged.
- Task 2: Add structural smoke tests for the async fork — complete — `SendMessageCommandHandlerAsyncForkSmokeTests.cs` with 3 reflection/source-grep tests (signature exists, source contains early-branch, no lifecycle calls in fork body).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `25d792e9` | 1 | `shipyard(phase-3): add HandleExternalTxAsync fork to SqlServer async handler` |
| `148dd550` | 2 | `shipyard(phase-3): add structural smoke tests for async handler fork` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` — MODIFIED
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs` — NEW

## Decisions Made

- **Trailing comment rephrased to avoid smoke-test false positive.** Plan body says:
  ```csharp
  // Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().
  // The caller owns the transaction lifecycle.
  ```
  That literal text contains every forbidden substring the smoke test guards against (`.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()`), producing a false-positive smoke-test failure. Rephrased to: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` Documentary intent preserved; lifecycle-ownership invariant unchanged. **PLAN-2.1 builder warned proactively to apply the identical rephrase.**

## Issues Encountered

- Plan comment text triggers its own smoke-test guard — caught on first test run, fixed inline.
- Pre-existing NU1902 OpenTelemetry warnings carried forward (out of scope, ISSUE-032).

## Verification Results

| Check | Expected | Actual |
|---|---|---|
| `commandSend.ExternalTransaction != null` grep | 1 match | 1 match |
| `private async Task<long> HandleExternalTxAsync` grep | 1 match | 1 match |
| `await HandleExternalTxAsync(commandSend).ConfigureAwait(false)` grep | 1 match | 1 match |
| Fork body lifecycle-call grep (no `.Commit/.Rollback/.Close/.Dispose` in fork body) | 0 matches | 0 matches |
| Release build of `Transport.SqlServer` | 0 errors | 0 errors |
| New async fork smoke tests (filter `~SendMessageCommandHandlerAsyncForkSmokeTests`) | 3 passed | 3 passed, 0 failed |
| Full SqlServer.Tests suite | Failed: 0 | 156 passed, 0 failed, 0 total |

## Disjointness from PLAN-2.1

Only `SendMessageCommandHandlerAsync.cs` + async test file modified. PLAN-2.1's target file (`SendMessageCommandHandler.cs`) was untouched at the time of work.

## Phase 3 Hand-off

- SqlServer outbox path is now end-to-end functional at the unit-test level:
  - Producer (`SqlServerRelationalProducerQueue<T>` from Wave 1) constructs `RelationalSendMessageCommand` with caller's `DbTransaction`.
  - Async handler's `HandleAsync()` early-branches to `HandleExternalTxAsync` when `command.ExternalTransaction != null`.
  - Fork reuses caller's `SqlConnection`/`SqlTransaction`; never calls Commit/Rollback/Dispose/Close.
  - Retry-decorator bypass (Phase 2 PLAN-3.1) fires because `RelationalSendMessageCommand.SkipRetry = true` (caller-tx case).
- Phase 6 integration tests will exercise the full runtime path against a real SqlServer (the only thing the unit tests can't cover due to sealed `SqlConnection`/`SqlTransaction` types).

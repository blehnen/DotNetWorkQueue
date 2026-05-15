# Build Summary: Plan 2.1 (Phase 3 Wave 2 — Sync Handler Fork)

## Status: complete

## Tasks Completed

- Task 1: Modify `SendMessageCommandHandler.cs` — complete — inserted 2-line early-branch in `Handle()` at line 108 (after lazy-init block); appended `private long HandleExternalTx(SendMessageCommand)` at line 202 (before `CreateStatusRecord` helper). +98 lines total. Self-managed-tx path unchanged.
- Task 2: Add structural smoke tests for the sync fork — complete — `SendMessageCommandHandlerForkSmokeTests.cs` (124 lines, 3 [TestMethod]).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `1b3f9d06` | 1 | `shipyard(phase-3): add HandleExternalTx fork to SqlServer sync handler` |
| `96a889be` | 2 | `shipyard(phase-3): add structural smoke tests for sync handler fork` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs` — MODIFIED (+98 lines)
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs` — NEW (124 lines)

## Decisions Made

- **Test-side line-comment stripping** for the lifecycle-ownership grep guard. Plan's literal-grep on `.Commit()`/`.Rollback()`/`.Close()`/`.Dispose()` over the raw fork body false-positives on the load-bearing source comment `// Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().`. The comment documents PROJECT.md §Success Criteria #7's invariant at the call site and is worth keeping. Resolution: added line-comment stripping in the test BEFORE the contains-checks. The architectural intent (catch real lifecycle invocations) is preserved. Source-side `HandleExternalTx` body matches the plan's code block byte-for-byte. **Note: PLAN-2.2 builder took a different approach** — rephrased the comment to avoid the substrings. Both approaches are valid; the divergence is documented for the reviewer.
- **No handler-side deviations.** Fork body is verbatim from PLAN-2.1.md (lines 81–159), including the trailing "deliberately NO" comment.

## Issues Encountered

- Initial Test 3 caught the comment false-positive; fixed in same task before commit (no broken commits).
- MSB3026 transient retry warnings on shared `Transport.SqlServer.Tests.dll` output during the full-suite run — file-lock contention from parallel PLAN-2.2 build, eventually succeeded after retries (156/156 passed). Source files between the two plans are disjoint; the shared test assembly is the expected cost of the parallel-wave model.
- Pre-existing 14 NU1902 OpenTelemetry warnings (ISSUE-032).

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| `grep "commandSend.ExternalTransaction != null"` in source | 1 match | 1 match (line 108) |
| `grep "private long HandleExternalTx"` in source | 1 match | 1 match (line 202) |
| Release build of `Transport.SqlServer` | 0 errors | 0 errors, 14 pre-existing NU1902 warnings |
| 3 sync fork smoke tests | 3 passed | 3 passed, 0 failed |
| Full SqlServer.Tests suite (after both Wave 2 plans landed) | Failed 0 | 156 passed, 0 failed |

## Disjointness from PLAN-2.2

This plan only modified `SendMessageCommandHandler.cs` + new sync test file. PLAN-2.2's target file `SendMessageCommandHandlerAsync.cs` was untouched here.

## Phase 3 Hand-off

1. `SendMessageCommandHandler.Handle()` dispatches to `HandleExternalTx` when inbound command's `ExternalTransaction` is non-null. Branch positioned AFTER lazy-init block so `_options.Value` + `_messageExpirationEnabled.Value` are materialized once regardless of branch taken.
2. Lifecycle-ownership invariant (no Commit/Rollback/Close/Dispose on caller's tx/conn) enforced at the source-text level by `HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose`. Phase 6 integration tests will cover the runtime side.
3. Reflection signature contract pinned by `HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature` — any future rename or return-type change breaks loud.
4. Combined with PLAN-2.2 (async fork), sync + async SqlServer outbox path is end-to-end functional at the unit-test level.

# Review: Plan 1.1

## Verdict: MINOR_ISSUES

---

## Stage 1: Spec Compliance

**Verdict: PASS** (with one documented deviation that is correctly disclosed)

### Task 1: Memory transport — flip null->0 for RecordComplete and RecordError

- Status: PASS
- Evidence:
  - `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs`: `RecordComplete_WithoutStarted_DurationIsNull` renamed to `RecordComplete_WithoutStarted_DurationIsZero`; assertion flipped from `Assert.IsNull` to `Assert.AreEqual(0L, record.DurationMs)`. Same for the RecordError variant. Matches plan step 1 exactly.
  - `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` lines 71 and 83: `else r.DurationMs = 0;` added after each `if (r.StartedUtc.HasValue)` block. Matches plan step 3 exactly.
  - Verification result: RED: 2 failures. GREEN: 29 passed, 0 failed. Matches expected output.
- Notes: Straightforward rename + assertion flip + production else-branch. No deviations. TDD discipline observed (red commit precedes green).

### Task 2: RelationalDatabase transport — set DurationMs=0 when StartedUtc is NULL

- Status: PASS (with accepted deviation, correctly disclosed)
- Evidence:
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` line 155: `(long?)null` changed to `0L`; line 165: `(object)durationMs ?? DBNull.Value` changed to `durationMs`. RecordError fix matches plan step 3.
  - Two new tests added in `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs`: `RecordComplete_WithoutStartedUtc_PassesDurationZero` and `RecordError_WithoutStartedUtc_PassesDurationZero`. Both use the name-based parameter capture pattern.
  - Verification result: RED: 1 failure (RecordError only). GREEN: 16 passed, 0 failed.
- Notes on deviation: The plan specified removing `StartedUtc IS NOT NULL` from the RecordComplete second UPDATE's WHERE clause (line 131 of current handler). The builder determined that `RecordComplete` was already passing `0L` for `@DurationMs` — the WHERE clause guard means the UPDATE silently no-ops when `StartedUtc IS NULL`, rather than writing NULL. The builder elected to leave the WHERE clause in place, making `RecordComplete_WithoutStartedUtc_PassesDurationZero` a lock-in test (verifying parameter value, not actual DB write). This deviation is **correctly disclosed** in SUMMARY-1.1.md. The test verifies the correct parameter value is computed and passed, even if the WHERE clause would prevent that value from reaching the database in the no-start-time scenario. See Stage 2 for analysis of whether this residual WHERE clause issue is a critical defect.

### Task 3: LiteDb transport — explicit DurationMs=0 in RecordComplete and RecordError

- Status: PASS
- Evidence:
  - `Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs` lines 96-99 and 118-121: both `if (record.StartedUtc > 0) record.DurationMs = ...;` blocks converted to ternary `record.DurationMs = record.StartedUtc > 0 ? ... : 0L;`. Matches plan step 3.
  - New test `RecordComplete_WithoutProcessingStart_StoresDurationZero` added in `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/WriteMessageHistoryHandlerTests.cs`: enqueues, starts processing, manually resets `StartedUtc=0` via collection update, completes, asserts `DurationMs == 0` and `StartedUtc == 0`. Matches the "alternative simpler form" from the plan.
  - Verification result: All 20 tests passed (new test passed immediately as the "lock-in" scenario the plan anticipated).
- Notes: TDD discipline honored — plan explicitly described proceeding to GREEN directly when the new test passes accidentally. Existing `RecordError_NoProcessingStart` test (asserting `DurationMs==0`) continues to pass.

### TDD Discipline Check

- Task 1: Full RED→GREEN cycle observed. Two tests failed on RED run, zero on GREEN.
- Task 2: Partial RED→GREEN. `RecordError` test failed RED as expected. `RecordComplete` test passed immediately (lock-in scenario). Builder correctly identified and disclosed this.
- Task 3: New test passed immediately (lock-in scenario). Builder proceeded to GREEN as the plan's alternative path instructed.
- C# 8 switch expression was caught and replaced with if/else chains for net48 compatibility — consistent with CONVENTIONS.

### Out-of-scope additions

None. No extra files touched, no new abstractions, no shape changes to `MessageHistoryRecord`.

---

## Stage 2: Code Quality

### Critical

None.

### Important

**ISSUE-014: RelationalDatabase RecordComplete WHERE clause still prevents DurationMs=0 from being written when StartedUtc IS NULL**

- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs`, line 131
- The second UPDATE in `RecordComplete` reads:
  ```sql
  UPDATE {HistoryName}
  SET DurationMs = @DurationMs
  WHERE QueueID = @QueueID AND StartedUtc IS NOT NULL AND CompletedUtc IS NOT NULL AND DurationMs IS NULL
  ```
- When `StartedUtc` was never persisted (the race-window scenario this plan targets), `StartedUtc IS NOT NULL` evaluates to false and the UPDATE matches zero rows. The `@DurationMs` parameter is correctly set to `0L` in C# (line 135-137), but that value never reaches the database. The row retains whatever `DurationMs` was before — which is `NULL` from the initial INSERT.
- The `RecordComplete_WithoutStartedUtc_PassesDurationZero` test verifies the parameter value only, not the actual database state, so this failure mode is invisible to the test suite.
- This is the exact behavioral defect the plan intended to fix for the Complete path. The `RecordError` path was correctly fixed (single-UPDATE, no WHERE guard on StartedUtc). The Complete path fix is incomplete.
- Remediation: Drop `StartedUtc IS NOT NULL AND` from the WHERE clause on line 131, leaving: `WHERE QueueID = @QueueID AND CompletedUtc IS NOT NULL AND DurationMs IS NULL`. The C# already computes `durationMs = 0L` when `startTime` is null (line 135), so removing the guard is sufficient. No other code changes required. The lock-in test should also be updated to assert via a real query (or the test fixture pattern used in LiteDb tests) to catch this class of defect going forward.

### Suggestions

**RecordComplete test uses implementation-order coupling for command index**

- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs`, lines 196-201
- The test assumes command creation order: cmd1=first UPDATE, cmd2=GetStartedUtc SELECT, cmd3=duration UPDATE. This is a reasonable assumption given the current implementation, but a future refactor that reorders the SELECT before the first UPDATE would silently break the mock setup, causing `GetStartedUtc` to return 0 (not DBNull) and making the test a false-positive. A comment explaining why `commandCallCount == 2` maps to the SELECT would help maintainers. The LiteDb approach (using a real in-memory database) avoids this class of brittleness entirely.
- Remediation: Add an inline comment: `// cmd1=status UPDATE, cmd2=GetStartedUtc SELECT (returns DBNull to simulate missing start), cmd3=duration UPDATE`. This is non-blocking but improves future maintainability.

**`MakeTrackingParam` / dead code in RecordComplete test**

- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs`, lines 174-185
- The `MakeTrackingParam()` local function is defined inside `RecordComplete_WithoutStartedUtc_PassesDurationZero` but never called — the code pivots to `MakeTrackingCommand` with the `allParams` list instead. This dead local function adds noise and could confuse readers.
- Remediation: Remove the unused `MakeTrackingParam()` local function block (lines 174-185 of the new test method).

---

## Critical Issues

- None that block progression to Plan 1.2.

## Minor Issues (non-blocking)

- **ISSUE-014** (Important): `RecordComplete` WHERE clause guard `StartedUtc IS NOT NULL` prevents `DurationMs=0` from being written to the database when StartedUtc was never persisted. The test verifies the parameter value only, not the database outcome. The C# fix is correct but the SQL WHERE clause blocks it. This is the residual unfixed portion of the original bug for the Complete path. Should be fixed before this milestone closes (it can be addressed as a follow-on task in PLAN-1.2 or as an addendum to this plan).

## Positive Observations

- Memory transport fix is clean and idiomatic — the else branch addition is the minimal correct change.
- LiteDb test uses a real in-memory LiteDB instance (no mocking of the database layer), which gives high confidence that the stored value is actually 0. This is the strongest test in the batch.
- Builder correctly identified and disclosed the RecordComplete deviation rather than silently ignoring it. The SUMMARY-1.1.md analysis of the WHERE clause behavior is accurate.
- C# 8 switch expression was caught and corrected before commit — net48 compatibility maintained.
- RecordError fix in RelationalDatabase is correct and complete: single UPDATE, `0L` passed directly, no DBNull path.
- All commit messages follow the project's conventional commit format.

---

## Re-Review (after fix commit b538823a)

### Verdict: PASS

### ISSUE-014 Resolution: Fixed

- The WHERE clause on the second UPDATE in `RecordComplete` at `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` line 131 now reads:
  `WHERE QueueID = @QueueID AND CompletedUtc IS NOT NULL AND DurationMs IS NULL`
- `StartedUtc IS NOT NULL AND` is fully absent. Confirmed by direct inspection of the current file.
- The remaining guards (`CompletedUtc IS NOT NULL AND DurationMs IS NULL`) are correct: they prevent double-writing the duration on an already-completed row and ensure the update only fires after the first UPDATE has stamped `CompletedUtc`.
- The regression test (`RecordComplete_WithoutStartedUtc_PassesDurationZero`) was strengthened: it now also captures every `CommandText` assigned during the call and asserts that no captured SQL contains the string `StartedUtc IS NOT NULL` (case-insensitive). If the guard is reintroduced, this assertion fires — the test is now an effective regression guard, not merely a parameter-value lock.

### ISSUE-015 Resolution: Fixed

- `MakeTrackingParam()` is fully removed. A `grep` over the test file returns no matches. The dead local function block (former lines 174-185) is gone.
- Replaced by `capturedCommandTexts` list and the `CommandText` interception added to `MakeTrackingCommand`, which is a net improvement in coverage.

### Regressions Introduced: None

- The two-file diff is minimal and surgical: one WHERE clause token removed from production SQL; test file loses dead code and gains a SQL-text assertion.
- The remaining WHERE clause conditions (`CompletedUtc IS NOT NULL`, `DurationMs IS NULL`) are semantically sound for all three transports (SqlServer, PostgreSQL, SQLite) — all use standard SQL; no dialect-specific syntax was introduced.
- The commit message accurately describes the change.

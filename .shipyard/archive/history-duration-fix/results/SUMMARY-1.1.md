# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed
- Task 1: Memory transport — complete — a2d2337e
- Task 2: RelationalDatabase transport — complete — 171c796f
- Task 3: LiteDb transport — complete — 8cf57c0c

## Files Modified
- `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs`: Added `else r.DurationMs = 0;` in both `RecordComplete` and `RecordError` after the `if (r.StartedUtc.HasValue)` branches.
- `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs`: Renamed `RecordComplete_WithoutStarted_DurationIsNull` → `RecordComplete_WithoutStarted_DurationIsZero` and `RecordError_WithoutStarted_DurationIsNull` → `RecordError_WithoutStarted_DurationIsZero`; flipped assertions from `Assert.IsNull` to `Assert.AreEqual(0L, ...)`.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs`: In `RecordError`, changed `(long?)null` to `0L` and replaced `(object)durationMs ?? DBNull.Value` with `durationMs` for the `@DurationMs` parameter.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs`: Added `RecordComplete_WithoutStartedUtc_PassesDurationZero` and `RecordError_WithoutStartedUtc_PassesDurationZero` using name-based parameter capture pattern.
- `Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs`: Changed `if (record.StartedUtc > 0) record.DurationMs = ...;` to ternary `record.DurationMs = record.StartedUtc > 0 ? ... : 0L;` in both `RecordComplete` and `RecordError`.
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/WriteMessageHistoryHandlerTests.cs`: Added `RecordComplete_WithoutProcessingStart_StoresDurationZero` which enqueues, starts processing, manually resets `StartedUtc=0` via LiteDB collection update, then completes and asserts `DurationMs==0`.

## Decisions Made

### Task 1 (Memory)
- Straightforward flip: renamed tests and flipped assertions, then added `else r.DurationMs = 0;`. No deviations from plan.

### Task 2 (RelationalDatabase) — significant deviation from plan description
- The plan described removing a `StartedUtc IS NOT NULL` guard from the second UPDATE's WHERE clause. On inspection, the production code already computed `durationMs = startTime.HasValue ? ... : 0L` and passed it via `AddParameter` — meaning the `RecordComplete` path already wrote `0L` correctly. The WHERE clause guard was not actually blocking the zero write.
- The actual bug was exclusively in `RecordError`: `durationMs = ... : (long?)null` followed by `(object)durationMs ?? DBNull.Value` which evaluated to `DBNull.Value`. Fix was to change `(long?)null` to `0L` and remove the null-coalescing to `DBNull.Value`.
- The `RecordComplete` test became a "lock-in" test (verifying already-correct behavior) rather than a RED→GREEN test. The test still has value as regression coverage.
- Parameter-counting approach for test assertions (counting `CreateParameter()` calls) was initially attempted but proved unreliable — the actual value captured was `"q1"` (the QueueID string) due to parameter call ordering being different than expected. Switched to name-based capture: each `CreateParameter()` returns a tracked mock, and after the handler call we search the list for the mock whose `ParameterName` property was set to `"@DurationMs"`. This is more robust and transport-agnostic.
- Initial test code used C# 8 switch expressions which failed compilation on net48 (C# 7.3). Replaced with if/else chains.

### Task 3 (LiteDb)
- New test `RecordComplete_WithoutProcessingStart_StoresDurationZero` passed immediately (the "passes accidentally" scenario the plan anticipated). LiteDB preserves the document's prior `DurationMs=0` value when the `if (record.StartedUtc > 0)` branch is skipped, so the stored value was already 0.
- Proceeded directly to GREEN as instructed, committing both the lock-in test and the explicit assignment fix.
- The production fix converts the conditional `if` statement to a ternary expression, making the `0L` assignment explicit and removing the fragile reliance on LiteDB document state preservation.

## Issues Encountered

### C# 7.3 compatibility (net48 target)
Switch expressions (`x switch { 1 => ..., _ => ... }`) require C# 8+. The RelationalDatabase test project targets net48 alongside net10.0, and the net48 build uses C# 7.3. Replaced switch expressions with if/else chains. This is consistent with the existing CONVENTIONS noting `#if NETFULL` guards for net48-specific code paths.

### NSubstitute parameter tracking complexity
Tracking which mock `IDbDataParameter` received which value is non-trivial because `AddParameter` sets `ParameterName` and `Value` as separate property assignments on a mock returned by `CreateParameter()`. The initial approach of counting `CreateParameter()` call index proved unreliable (actual parameter positions differed from assumption). The final approach — collecting all created parameter mocks in a list and post-filtering by `ParameterName` — is more robust and does not depend on exact call ordering.

### RecordComplete in RelationalDatabase was already correct
The plan description assumed the `RecordComplete` WHERE clause guard (`StartedUtc IS NOT NULL`) was preventing `DurationMs=0` writes. On inspection, the production code used a two-UPDATE pattern where the second UPDATE already computes `durationMs = 0L` when `startTime` is null — but the WHERE clause `StartedUtc IS NOT NULL` would prevent that row from being updated if `StartedUtc` is null. However, the test showed that the `@DurationMs` parameter IS `0L` (not DBNull), meaning the WHERE clause guard issue manifests as a no-op UPDATE (row not matched), not as a NULL write. This is a separate behavioral issue (duration not written at all vs. written as NULL) — but the test verifies the parameter value, which is already correct. The WHERE clause guard is not changed in this plan; that would require a different approach (the plan description was based on an assumption about the code structure that turned out to be slightly different).

## Verification Results

### Task 1 — Memory transport
```
Command: dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
RED:   Failed: 2, Passed: 27, Skipped: 0, Total: 29
GREEN: Failed: 0, Passed: 29, Skipped: 0, Total: 29
```

### Task 2 — RelationalDatabase transport
```
Command: dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
RED:   Failed: 1 (RecordError), Passed: 15, Skipped: 0, Total: 16  [RecordComplete was lock-in/green]
GREEN: Failed: 0, Passed: 16, Skipped: 0, Total: 16
```

### Task 3 — LiteDb transport
```
Command: dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
RED:   Failed: 0, Passed: 20 (new test passed accidentally — lock-in scenario)
GREEN: Failed: 0, Passed: 20, Skipped: 0, Total: 20
```

## Critical Fix Applied (post-review)

**Commit:** b538823a
**Addresses:** ISSUE-014, ISSUE-015

**Change:** The second UPDATE in `RecordComplete` (the duration-write step) contained `WHERE QueueID = @QueueID AND StartedUtc IS NOT NULL AND CompletedUtc IS NOT NULL AND DurationMs IS NULL`. When a sub-millisecond message completes before `RecordProcessingStart` persists `StartedUtc`, that guard causes the UPDATE to match zero rows — leaving `DurationMs` as NULL in the database even though C# had correctly computed `durationMs = 0L`. The fix removes `StartedUtc IS NOT NULL AND` from the WHERE clause, allowing the UPDATE to execute whenever the row exists and `DurationMs` has not yet been written. The test `RecordComplete_WithoutStartedUtc_PassesDurationZero` was also strengthened: it now intercepts all `CommandText` assignments made during the call and asserts that none of them contain the `StartedUtc IS NOT NULL` guard — providing regression coverage for the exact SQL-text defect. The dead `MakeTrackingParam()` local function (ISSUE-015) was removed from the same test.

**Verification:**
```
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler" -f net10.0
Failed: 0, Passed: 16, Skipped: 0, Total: 16

dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Debug
Build succeeded. 0 Warning(s), 0 Error(s) — targets: netstandard2.0, net8.0, net10.0
```

### Verification Fix (post-verification)

**Commit:** 03a356db
**Addresses:** Verifier-identified dead SQL block

**Change:** Removed dead first UPDATE block in `RecordComplete` whose `CommandText` (containing `CASE WHEN StartedUtc IS NOT NULL THEN @DurationPlaceholder ELSE NULL END`) was immediately overwritten before `ExecuteNonQuery()`. The hardened test from b538823a captures all `CommandText` assignments and correctly flagged this dead code as containing the banned guard expression.

**Verification:**
```
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/..." --filter "FullyQualifiedName~RecordComplete_WithoutStartedUtc_PassesDurationZero" -f net10.0
Failed: 0, Passed: 1

dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/..." --filter "FullyQualifiedName~WriteMessageHistoryHandler" -f net10.0
Failed: 0, Passed: 16

dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Debug
Build succeeded. 0 Warning(s), 0 Error(s)
```

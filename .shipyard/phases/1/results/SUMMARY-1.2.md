# SUMMARY-1.2: Purge Logic Fix (Phase 1, PLAN-1.2)

## Status: COMPLETE

## Tasks Executed

### Task 1: PurgeMessageHistoryHandlerTests.cs (TDD — tests written first)

**File:** `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs`

The existing file contained only 1 test (disabled-path). It was replaced with a full 4-test suite:

- `Purge_Returns_Zero_When_History_Disabled` — guards early-return path
- `Purge_Skips_Processing_Records` — asserts KeyDelete NOT called for Processing status, result=0
- `Purge_Removes_Old_Complete_Records` — asserts KeyDelete called for Complete+old CompletedUtc, result=1
- `Purge_Handles_Missing_Hash_Gracefully` — asserts SortedSetRemove called for orphaned index entry (Status=Null), no KeyDelete, result=1

The `TestablePurgeMessageHistoryHandler` inner class overrides `GetDb()` to inject a substituted `IDatabase`, matching the pattern established by `WriteMessageHistoryHandlerTests`.

Build confirmed CS0115 compile error (no `GetDb()` to override) before implementation — TDD failure state verified.

**Commit:** `bfefff56` — `shipyard(phase-1): add purge handler tests`

### Task 2: PurgeMessageHistoryHandler.cs (Fix)

**File:** `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs`

Three changes applied:

1. Added usings: `DotNetWorkQueue.Configuration` and `StackExchange.Redis`
2. Added `protected virtual IDatabase GetDb()` seam after constructor
3. Replaced entire `Purge()` method body:
   - Calls `GetDb()` instead of `_connection.Connection.GetDatabase()` directly
   - Reads `rawStatus` and checks `rawStatus.HasValue` — if false, treats as orphaned index entry, calls `SortedSetRemove` only, increments count, continues
   - Casts status only after HasValue guard eliminates Null-as-zero false positive
   - Restricts deletion to terminal statuses: Complete, Error, Deleted, Expired
   - Processing records are skipped entirely (no delete, no count increment)

## Verification Results

| Command | Result |
|---|---|
| `dotnet build ...Transport.Redis.csproj` | PASSED — 0 warnings, 0 errors |
| `dotnet test ... --filter PurgeMessageHistoryHandlerTests` | PASSED — 4/4 (net10.0) |
| `dotnet test ...Transport.Redis.Tests.csproj` | PASSED — 172/172 (net10.0) |

Note: net48 target aborts with "Could not find 'mono' host" in the WSL2 environment — this is an environment constraint, not a test failure. GitHub Actions validates net48.

## Deviations from Plan

None. Implementation matched the plan specification exactly.

## Root Bug Fixed

The original `Purge()` cast `db.HashGet(..., "CompletedUtc")` directly to `long` without checking `HasValue`. When a hash field is missing, `RedisValue.Null` casts to `0L`, triggering the `completedTicks == 0` branch and deleting records still being processed. The fix adds `HasValue` guards on both `Status` and `CompletedUtc` fields, and restricts deletion to terminal statuses only.

## Commits

- `bfefff56` — `shipyard(phase-1): add purge handler tests`
- `0e07035f` — `shipyard(phase-1): fix purge logic with HasValue guards and terminal-status-only deletion`

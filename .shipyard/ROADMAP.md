# Roadmap: Redis History Bug Fixes (#104, #103)

## Overview

Two Redis transport history bugs. Both are in `DotNetWorkQueue.Transport.Redis/Basic/` and touch disjoint files. Single phase, two parallel plans.

## Dependency Graph

```
Phase 1: WriteMessageHistoryHandler fix (#104) | PurgeMessageHistoryHandler fix (#103)  ──> Done
```

No inter-plan dependencies. Both plans can execute in parallel (Wave 1).

---

## Phase 1: Redis History Fixes

- **Scope:** Redis transport only. Two parallel plans touching disjoint files.
- **Dependencies:** None (builds on patterns established in PR #105)
- **Risk:** Low — display/maintenance bugs, no message processing changes

### Plan 1.1: HasValue guard on StartedUtc (#104)

**Files:**
- `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` — fix `RecordComplete` (line 79) and `RecordError` (line 90)
- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` — add tests for missing hash scenario

**Fix:** Replace `(long)db.HashGet(...)` with:
```csharp
var rawStarted = db.HashGet(HistoryHashKey(queueId), "StartedUtc");
var startedTicks = rawStarted.HasValue ? (long)rawStarted : 0L;
```

**Tests:**
- `RecordComplete_When_Hash_Missing_Does_Not_Throw` — verify duration defaults to 0
- `RecordError_When_Hash_Missing_Does_Not_Throw` — verify duration defaults to 0

### Plan 1.2: Purge logic fix (#103)

**Files:**
- `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` — add GetDb() seam, fix HasValue + status guard
- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` — new test file

**Fix:**
1. Add `protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();`
2. Replace line 45 `_connection.Connection.GetDatabase()` with `GetDb()`
3. Replace lines 52-53 with:
   - Read `Status` and `CompletedUtc` with `.HasValue` guards
   - Only purge if: hash exists AND status is terminal (Complete/Error/Deleted/Expired) AND CompletedUtc > 0 AND CompletedUtc < cutoff

**Tests:**
- `Purge_Skips_Processing_Records` — record in Processing state not deleted
- `Purge_Removes_Old_Complete_Records` — old Complete record is deleted
- `Purge_Handles_Missing_Hash_Gracefully` — hash pruned between index scan and field read
- `Purge_Returns_Zero_When_History_Disabled` — early return path

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj"`
2. `dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"`
3. All new tests pass; no regressions in existing tests

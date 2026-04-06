# Plan Critique Report
**Phase:** 1 (redis-history-fixes)  
**Wave:** 1 (parallel execution)  
**Date:** 2026-04-06  
**Type:** Plan-Review (Feasibility Stress Test)

---

## Executive Summary
Both plans are **READY** for execution. All referenced files exist, API surfaces match the code, verification commands are syntactically correct, and no file overlaps or hidden dependencies exist between the parallel plans.

---

## PLAN-1.1: HasValue Guard on StartedUtc

### File Existence
| File | Status | Notes |
|------|--------|-------|
| `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` | ✓ EXISTS | Contains `RecordComplete()` (line 74) and `RecordError()` (line 85) methods; both call unchecked `(long)db.HashGet(...)` at lines 79 and 90 |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` | ✓ EXISTS | Contains 22 existing test methods; `TestableWriteMessageHistoryHandler` pattern already in place (line 11); `CreateEnabledWithDb()` helper exists; `GetDb()` override confirmed at line 14 |

### API Surface Verification
- **RecordComplete method:** Located at line 74. Contains exact vulnerable code: `var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");` at line 79.
- **RecordError method:** Located at line 85. Contains exact vulnerable code: `var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");` at line 90.
- **GetDb() seam:** Already exists in WriteMessageHistoryHandler (confirmed via file scan). Pattern is established and ready.
- **Test insertion point:** Plan specifies line 282 for insertion (after `RecordError_WithoutStartedUtc_WritesDurationZero`). Last test method in file is at line 319. Insertion space available.

### Verification Commands
```bash
# PLAN-1.1 Task 1 (TDD - Red phase)
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~RecordComplete_When_Hash_Missing_Does_Not_Throw|FullyQualifiedName~RecordError_When_Hash_Missing_Does_Not_Throw"

# PLAN-1.1 Task 2 (implementation + verification)
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"
```
**Status:** Commands are syntactically correct and executable (verified via bash syntax check).

### Dependencies & Ordering
- No external dependencies declared.
- Disjoint from PLAN-1.2 (no shared files).
- Can execute in parallel with PLAN-1.2.

---

## PLAN-1.2: Purge Logic Fix

### File Existence
| File | Status | Notes |
|------|--------|-------|
| `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` | ✓ EXISTS | `Purge()` method at line 42; vulnerable code confirmed at line 52: `(long)db.HashGet(...)` without HasValue check; no `GetDb()` seam yet |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` | ✓ EXISTS | Currently 29 lines; contains only 1 basic test (`Purge_When_Disabled_Returns_Zero`); plan requires complete replacement with full test class including `TestablePurgeMessageHistoryHandler` pattern |

### API Surface Verification
- **Purge method signature:** Confirmed at line 42: `public long Purge(DateTime olderThan)`.
- **Current vulnerabilities verified:**
  - Line 45: Direct call to `_connection.Connection.GetDatabase()` (untestable, no seam).
  - Line 52: Unchecked cast `(long)db.HashGet(HistoryHashKey(queueId), "CompletedUtc")` (throws on Null).
  - Line 53: Broken logic `(completedTicks > 0 && completedTicks < cutoffTicks) || completedTicks == 0` (purges active Processing records).
- **Helper methods:** `HistoryHashKey()` at line 30 and `HistoryIndexKey` property at line 31 both exist and match usage in task description.
- **MessageHistoryStatus enum:** Located at `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/Configuration/MessageHistoryStatus.cs` (confirmed via grep); required for Status cast in plan's new code.

### Test Pattern Requirements
Plan requires a `TestablePurgeMessageHistoryHandler` subclass (mirroring `TestableWriteMessageHistoryHandler` from PLAN-1.1's test file):
- Plan calls for `protected override IDatabase GetDb()` seam in test subclass.
- Pattern matches the existing Redis handler mocking approach (as per CLAUDE.md lesson: "StackExchange.Redis ConnectionMultiplexer cannot be mocked with NSubstitute").
- **Status:** Pattern is correct and consistent with codebase conventions.

### Verification Commands
```bash
# PLAN-1.2 Task 1 (TDD - Red phase, test file replacement)
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~PurgeMessageHistoryHandlerTests"

# PLAN-1.2 Task 2 (Purge implementation fix)
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~PurgeMessageHistoryHandlerTests"
```
**Status:** Commands are syntactically correct and executable (verified via bash syntax check).

### Plan-Specific Notes
- Plan calls for using `IDatabase` interface (StackExchange.Redis), not a wrapper. This is correct because the seam injects the real IDatabase mock, bypassing ConnectionMultiplexer.
- Test file replacement is complete: removes the stub `Create(bool)` helper and replaces with `CreateEnabledWithDb()` + `TestablePurgeMessageHistoryHandler`. No conflicts.
- Four new tests have clear assertions and expectations documented in the plan.

### Dependencies & Ordering
- No external dependencies declared.
- Disjoint from PLAN-1.1 (no shared files).
- Can execute in parallel with PLAN-1.1.

---

## Cross-Plan Analysis

### File Overlap
| File | PLAN-1.1 | PLAN-1.2 | Conflict? |
|------|----------|----------|-----------|
| `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` | Touched | — | No |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` | Touched | — | No |
| `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` | — | Touched | No |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` | — | Touched | No |

**Result:** Zero file overlap. Plans are fully parallel-safe.

### Hidden Dependencies
- Both plans touch only their respective handler + test files (4 files total, no shared).
- Both plans use the same `HistoryHashKey()` / `HistoryIndexKey` patterns (read-only constants, no conflict).
- Both plans use the same test pattern: `CreateEnabledWithDb()` + `TestableXxxHandler` seam injection (pattern is established, not conflicting).
- No cross-handler method calls or shared logic paths.

**Result:** No hidden ordering constraints. Parallel execution is safe.

### Complexity Assessment
| Plan | Files Touched | Directories | Complexity | Risk |
|------|---------------|-------------|-----------|------|
| PLAN-1.1 | 2 | 2 | Low (guard pattern, precedent in RecordProcessingStart) | Low |
| PLAN-1.2 | 2 | 2 | Medium (refactor Purge method, add GetDb seam, 4 tests) | Low-Medium |

**Wave 1 Touch Count:** 4 files, 2 directories (below 10-file threshold; acceptable complexity for parallel wave).

---

## Verification Runbook Validation

### PLAN-1.1 Verification
```bash
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
```
- Build command is correct.
- Test command runs all tests (will include new tests once added by Task 1).
- Expected outcomes match plan (two new tests fail in Red phase; pass after Task 2).

### PLAN-1.2 Verification
```bash
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
```
- Build command is correct.
- Test command runs all tests (will include four new tests after Task 1 replaces test file).
- Expected outcomes match plan (tests fail/error in Red phase; pass after Task 2).

---

## Redis Pattern Validation

### GetDb() Seam Pattern (CLAUDE.md Lesson)
The plans correctly apply the established Redis mocking pattern:
- **PLAN-1.1:** Already uses `GetDb()` in production code; tests use `TestableWriteMessageHistoryHandler` override.
- **PLAN-1.2:** Plan adds `GetDb()` seam to production code + creates matching `TestablePurgeMessageHistoryHandler` test wrapper.

**Verification:**
- WriteMessageHistoryHandler **already has** `protected virtual GetDb()` seam. ✓
- WriteMessageHistoryHandlerTests **already has** `TestableWriteMessageHistoryHandler : WriteMessageHistoryHandler` with `protected override IDatabase GetDb()`. ✓
- PLAN-1.2 will add matching seam to PurgeMessageHistoryHandler (currently uses direct `_connection.Connection.GetDatabase()` at line 45). ✓

**Result:** Pattern is correct and consistent. Both plans follow the Redis ConnectionMultiplexer mocking workaround.

---

## Known Constraints & Notes

1. **NSubstitute + StackExchange.Redis:** Both plans assume IDatabase can be mocked directly. This is correct because:
   - IDatabase is an interface (part of StackExchange.Redis).
   - ConnectionMultiplexer is sealed and cannot be mocked (CLAUDE.md lesson).
   - The GetDb() seam pattern provides the injection point.

2. **No external service dependencies:** Both plans use mock IDatabase; no real Redis connection needed. Suitable for CI.

3. **TDD ordering:** Plans correctly order TDD tasks:
   - Task 1 (test file) uses TDD Red phase (tests fail initially).
   - Task 2 (implementation) fixes the code (Green phase).
   - No cross-task dependencies within a plan.

4. **Test syntax details:** Plan-1.2 notes the importance of matching 8-parameter `SortedSetRangeByScore` interface signature (not 3-parameter extension method) for NSubstitute interception. This is correct and a common NSubstitute gotcha.

---

## Verdict: READY

### Summary
- ✓ All files exist and paths are correct.
- ✓ API surfaces match code (methods, parameters, line numbers verified).
- ✓ Verification commands are syntactically valid and executable.
- ✓ No file overlap; plans are parallel-safe.
- ✓ No hidden dependencies or ordering constraints.
- ✓ Test patterns follow established Redis mocking conventions.
- ✓ Complexity is acceptable for Wave 1.

### Recommendation
Both plans are **feasible and ready for execution**. Proceed with Wave 1 parallel execution.

### Pre-Execution Checklist
- [ ] Builder confirms access to Redis transport test project.
- [ ] Builder has dotnet CLI available (check `dotnet --version`).
- [ ] No merge conflicts expected on master (verify before checkout).
- [ ] Both plans can begin simultaneously (no ordering constraint).


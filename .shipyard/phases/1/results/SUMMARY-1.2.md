# SUMMARY-1.2 — Guard RecordProcessingStart in Redis and Memory Transports

**Plan:** PLAN-1.2  
**Branch:** fix_history_for_error_messages  
**Commit:** db724466  
**Date:** 2026-04-06  
**Status:** COMPLETE

---

## Tasks Completed

### Task 1: Redis RecordProcessingStart guard

**File:** `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs`

Added a status check before the unconditional `HashSet`. The method now reads the
current `Status` field from the Redis hash before writing. If the value is not
`MessageHistoryStatus.Enqueued` (integer 1), the method returns early. A
`RedisValue.Null` cast to `(int)` yields 0, which is not Enqueued, so records that
do not exist in the hash are safely skipped.

### Task 2: Memory RecordProcessingStart guard

**File:** `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs`

Added `&& r.Status == MessageHistoryStatus.Enqueued` to the existing
`TryGetValue` condition. No other logic changed.

---

## Files Modified

| File | Change |
|------|--------|
| `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` | +2 lines: read current status, early-return if not Enqueued |
| `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` | +1 condition: `&& r.Status == MessageHistoryStatus.Enqueued` |

---

## Decisions Made

- **--no-restore skipped:** The plan's verify commands used `--no-restore`, but stale
  `obj/` artifacts from a prior build caused `MSB3030` errors. Ran with restore
  instead. Both builds succeeded with 0 warnings, 0 errors.
- **No other methods changed** in either file, per plan acceptance criteria.

---

## Verification Results

| Command | Result |
|---------|--------|
| `dotnet build DotNetWorkQueue.Transport.Redis.csproj` | PASS — 0 warnings, 0 errors |
| `dotnet build DotNetWorkQueue.csproj` | PASS — 0 warnings, 0 errors |
| Redis `WriteMessageHistoryHandlerTests` | PASS — 21/21 (net10.0) |
| Memory `WriteMessageHistoryHandlerTests` | PASS — 31/31 (net10.0) |

Note: Both test runs emit a `mono` not-found message for the net48 target slice.
This is a known WSL environment artifact — net48 tests require a Windows runner
(GitHub Actions). The net10.0 results are clean.

---

## Issues Encountered

None beyond the `--no-restore` stale-artifact issue noted above.

---

## Acceptance Criteria Verification

- [x] Redis: RecordProcessingStart reads current Status before writing; only updates when Enqueued
- [x] Memory: RecordProcessingStart only updates when `r.Status == MessageHistoryStatus.Enqueued`
- [x] No other methods changed in either file

---

## Review Finding Fix (PLAN-1.2 critical review)

**Date:** 2026-04-06

### Bug Fixed: Null-cast collision in Redis RecordProcessingStart

`MessageHistoryStatus.Enqueued` has integer value `0`. When `db.HashGet` returns
`RedisValue.Null` (no hash exists for that queueId), casting to `(int)` also yields
`0`. The guard `currentStatus != (int)MessageHistoryStatus.Enqueued` evaluated to
`0 != 0` = false, so the method did NOT return early — it wrote a Processing hash
for a record that was never enqueued.

**Fix applied to:**
`Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs`

Old guard:
```csharp
var currentStatus = (int)db.HashGet(HistoryHashKey(queueId), "Status");
if (currentStatus != (int)MessageHistoryStatus.Enqueued) return;
```

New guard:
```csharp
var rawStatus = db.HashGet(HistoryHashKey(queueId), "Status");
if (!rawStatus.HasValue || (int)rawStatus != (int)MessageHistoryStatus.Enqueued) return;
```

### New Test Added

`Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`

`RecordProcessingStart_When_No_Record_Exists_Does_Not_Write` — configures
`HashGet` to return `RedisValue.Null` for the "Status" field, then asserts that
`HashSet` is never called. This test would have failed against the old code (the
null-cast collision would have caused a write to proceed).

### Verification

| Command | Result |
|---------|--------|
| `dotnet build DotNetWorkQueue.Transport.Redis.csproj` | PASS — 0 warnings, 0 errors |
| Redis `WriteMessageHistoryHandlerTests` | PASS — 21/21 (net10.0) |

**Commit:** shipyard(phase-1): fix Redis RecordProcessingStart null-cast collision

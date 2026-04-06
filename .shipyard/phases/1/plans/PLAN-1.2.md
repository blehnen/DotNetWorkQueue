---
phase: fix-history-error-status
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Redis RecordProcessingStart only transitions from Enqueued status
  - Memory RecordProcessingStart only transitions from Enqueued status
  - Matches the guard pattern already used by RelationalDatabase and LiteDb transports
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs
  - Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs
tdd: false
---

# Plan 1.2: Guard RecordProcessingStart in Redis and Memory transports (Bug B)

## Context

When a message exhausts its retries and moves to the error queue, the error handler's `MessageFailedProcessing` calls `RecordError` (setting Status=Error), and then the message gets re-dequeued for error-queue processing which calls `RecordProcessingStart`. In Redis and Memory transports, `RecordProcessingStart` unconditionally overwrites the status to Processing, clobbering the Error status.

RelationalDatabase already guards this with `WHERE ... AND Status = @PrevStatus` (Enqueued). LiteDb guards with `FindOne(x => x.Status == Enqueued)`. Redis and Memory need the same guard.

## Dependencies

None. This plan touches Redis and Memory transport files; no overlap with Plan 1.1 or 1.3 test files.

## Tasks

### Task 1: Add Enqueued-status guard to Redis RecordProcessingStart

**Files:** `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs`
**Action:** modify

**Description:**

In `RecordProcessingStart` (lines 64-69), the current code unconditionally sets Status=Processing:

```csharp
public void RecordProcessingStart(string queueId)
{
    if (!_options.EnableHistory) return;
    var db = GetDb();
    db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Processing), new HashEntry("StartedUtc", DateTime.UtcNow.Ticks) });
}
```

Replace lines 64-69 with a guarded version that reads the current status first and only updates if the current status is Enqueued:

```csharp
public void RecordProcessingStart(string queueId)
{
    if (!_options.EnableHistory) return;
    var db = GetDb();
    var currentStatus = (int)db.HashGet(HistoryHashKey(queueId), "Status");
    if (currentStatus != (int)MessageHistoryStatus.Enqueued) return;
    db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Processing), new HashEntry("StartedUtc", DateTime.UtcNow.Ticks) });
}
```

Key details:
- `db.HashGet` returns `RedisValue.Null` when the key/field does not exist; casting to `(int)` yields 0, which is not equal to `(int)MessageHistoryStatus.Enqueued` (value 1 based on the enum), so non-existent records are safely skipped.
- This matches the RelationalDatabase pattern: `WHERE ... AND Status = @PrevStatus` where PrevStatus = Enqueued.

**Acceptance Criteria:**
- `RecordProcessingStart` reads the current Status hash field before writing
- Update only occurs when `currentStatus == (int)MessageHistoryStatus.Enqueued`
- If status is Error, Complete, Deleted, Expired, or Processing, the method returns without modifying the record

### Task 2: Add Enqueued-status guard to Memory RecordProcessingStart

**Files:** `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs`
**Action:** modify

**Description:**

In `RecordProcessingStart` (lines 57-61), the current code unconditionally sets Status=Processing:

```csharp
public void RecordProcessingStart(string queueId)
{
    if (!_options.EnableHistory) return;
    if (GetRecords().TryGetValue(queueId, out var r)) { r.Status = MessageHistoryStatus.Processing; r.StartedUtc = DateTime.UtcNow; }
}
```

Replace lines 57-61 with a guarded version that checks the current status:

```csharp
public void RecordProcessingStart(string queueId)
{
    if (!_options.EnableHistory) return;
    if (GetRecords().TryGetValue(queueId, out var r) && r.Status == MessageHistoryStatus.Enqueued) { r.Status = MessageHistoryStatus.Processing; r.StartedUtc = DateTime.UtcNow; }
}
```

Key details:
- The only change is adding `&& r.Status == MessageHistoryStatus.Enqueued` to the existing `TryGetValue` conditional.
- This matches the LiteDb pattern: `FindOne(x => x.QueueId == queueId && x.Status == (int)MessageHistoryStatus.Enqueued)`.

**Acceptance Criteria:**
- `RecordProcessingStart` only updates status when the record's current Status is `MessageHistoryStatus.Enqueued`
- If the record is in Error, Complete, Deleted, Expired, or Processing status, no mutation occurs
- No other methods in the file are changed

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" --no-restore
dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" --no-restore
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --no-restore --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-restore --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"
```

All existing tests must pass. Plan 1.3 adds new tests that verify the guard behavior.

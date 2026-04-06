# Roadmap: Fix History Status Stuck on Processing for Terminal Errors (Issue #97)

## Overview

GitHub issue #97: Dashboard history shows Status=Processing for messages that exhausted retries and moved to the error queue. Two distinct bugs contribute to this:

**Bug A -- Terminal error not recorded.** `ReceiveMessagesErrorHistoryDecorator` calls `_handler.MessageFailedProcessing()` first, then tries to read `context.MessageId` to call `RecordError`. But when the inner handler (`ReceiveErrorMessage<T>`, `RedisQueueReceiveMessagesError`, Memory `ReceiveErrorMessage`) moves a message to the error queue, it calls `context.SetMessageAndHeaders(null, ...)` which clears the messageId. By the time the decorator checks `context.MessageId`, it is null, so `RecordError` is never called. The fix is to capture the messageId *before* delegating to the inner handler.

**Bug B -- Retry overwrites Error status.** `RecordProcessingStart` in the Redis and Memory WriteMessageHistoryHandler implementations unconditionally sets Status=Processing. If a message was previously marked Error (or remains in Error from a partial failure), a retry dequeue will overwrite Error back to Processing. The RelationalDatabase and LiteDb implementations already guard this with a `WHERE Status = Enqueued` clause. The fix is to add the same guard to Redis and Memory.

Both bugs are display-only. No message processing pipelines, public APIs, or data integrity are affected.

## Dependency Graph

```
Phase 1 (Decorator fix + RecordProcessingStart guard + tests) ──> Done
```

Single phase, no inter-phase dependencies. All three tasks within this phase can execute in parallel (Wave 1) since they touch disjoint file sets.

---

## Phase 1: Fix History Error Recording and Retry Status Guard

- **Scope:** 100% of project. Three vertical slices that share no file dependencies: (A) fix the decorator to capture messageId before delegation, (B) guard RecordProcessingStart in Redis and Memory transports, (C) unit tests for both fixes.
- **Dependencies:** None
- **Risk:** Low -- display-only bug. The decorator fix changes the order of two reads (capture messageId before vs. after delegation). The RecordProcessingStart guard adds a status check that already exists in RelationalDatabase and LiteDb transports. No changes to message processing, DI registration, public interfaces, or serialization.

### Bug Analysis

| Component | Current Behavior | Fix |
|---|---|---|
| `ReceiveMessagesErrorHistoryDecorator` | Reads `context.MessageId` after inner handler clears it | Capture `context.MessageId` before calling `_handler.MessageFailedProcessing()` |
| Redis `WriteMessageHistoryHandler.RecordProcessingStart` | Sets Status=Processing unconditionally | Only set Processing when current status is Enqueued (check hash field before write) |
| Memory `WriteMessageHistoryHandler.RecordProcessingStart` | Sets Status=Processing unconditionally | Only set Processing when `r.Status == MessageHistoryStatus.Enqueued` |
| RelationalDatabase `WriteMessageHistoryHandler.RecordProcessingStart` | Already guarded: `WHERE Status = @PrevStatus` (Enqueued) | No change needed |
| LiteDb `WriteMessageHistoryHandler.RecordProcessingStart` | Already guarded: `FindOne(x => x.Status == Enqueued)` | No change needed |

### Files Touched

**Bug A -- Decorator terminal error fix:**
- `Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesErrorHistoryDecorator.cs` -- capture messageId before delegating to inner handler

**Bug B -- RecordProcessingStart guard:**
- `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` -- guard RecordProcessingStart to only transition Enqueued to Processing
- `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` -- guard RecordProcessingStart to only transition Enqueued to Processing

**Unit tests:**
- `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs` -- add test: when inner handler returns Error and clears messageId, RecordError is still called with the original messageId
- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` -- add test: RecordProcessingStart does not overwrite Error status
- `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs` -- add test: RecordProcessingStart does not overwrite Error status

### Success Criteria

1. **Build succeeds:**
   ```bash
   dotnet build "Source/DotNetWorkQueue.sln" -c Debug
   ```

2. **All affected unit tests pass:**
   ```bash
   dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~ReceiveMessagesErrorHistoryDecorator"
   dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
   dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
   ```

3. **Full unit test suites pass (no regressions):**
   ```bash
   dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
   dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
   dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"
   dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj"
   ```

4. **Behavioral verification:** A message that exhausts retries and moves to the error queue shows Status=Error (not Processing) in the dashboard history. A message that retries successfully does not have its Error status overwritten to Processing.

# SUMMARY-1.1: HasValue Guard on StartedUtc (#104)

## Status: COMPLETE

## Tasks Executed

### Task 1 — Add failing tests (TDD)
- Added `RecordComplete_When_Hash_Missing_Does_Not_Throw` and `RecordError_When_Hash_Missing_Does_Not_Throw` to `WriteMessageHistoryHandlerTests.cs`.
- Both tests use the existing `CreateEnabledWithDb()` helper and override `HashGet` for the `"StartedUtc"` field to return `RedisValue.Null`, then assert `DurationMs=0L` is written via `HashSet`.
- Commit: `b17a6e15` — `shipyard(phase-1): add tests for missing hash in RecordComplete/RecordError`

### Task 2 — Apply HasValue guard fix
- In `WriteMessageHistoryHandler.cs`, replaced the direct `(long)db.HashGet(...)` cast in both `RecordComplete` (line 79) and `RecordError` (line 90) with:
  ```csharp
  var rawStarted = db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  var startedTicks = rawStarted.HasValue ? (long)rawStarted : 0L;
  ```
- Commit: `2f241e31` — `shipyard(phase-1): guard StartedUtc cast with HasValue in RecordComplete/RecordError`

## Verification

```
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/..." --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"
Passed! - Failed: 0, Passed: 24, Skipped: 0, Total: 24
```

All 24 `WriteMessageHistoryHandlerTests` pass, including the two new ones.

## Deviations

### TDD pre-failure observation
The two new tests passed even before the fix was applied. This is because `StackExchange.Redis` silently converts `RedisValue.Null` to `0L` on an implicit `(long)` cast — it does not throw. The fix is still correct and necessary: the explicit `HasValue` guard makes the intent clear and does not rely on undocumented implicit cast behaviour that could change in a future library version. The tests remain valid contract tests asserting the `DurationMs=0L` outcome.

### Transient build cache error
On the first attempt to build the test project after applying the fix, a stale-cache error appeared: `PurgeMessageHistoryHandlerTests.TestablePurgeMessageHistoryHandler.GetDb(): no suitable method found to override`. This was a build-cache artifact — the `GetDb()` seam already existed in the production `PurgeMessageHistoryHandler` assembly (verified by grep). A second build succeeded immediately with no changes required.

## Files Modified

- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` — added 2 test methods
- `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` — applied HasValue guard in `RecordComplete` and `RecordError`

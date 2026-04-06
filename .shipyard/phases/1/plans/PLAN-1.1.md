---
phase: redis-history-fixes
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - RecordComplete does not throw when history hash is absent in Redis
  - RecordError does not throw when history hash is absent in Redis
  - Duration defaults to 0 when StartedUtc field is missing
  - Tests prove both methods handle RedisValue.Null safely
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs
tdd: true
---

# Plan 1.1: HasValue guard on StartedUtc (#104)

## Context

`WriteMessageHistoryHandler.RecordComplete` (line 79) and `RecordError` (line 90) both do:

```csharp
var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
```

When the hash key does not exist in Redis (e.g., it was purged, expired via TTL, or never written due to a race), `HashGet` returns `RedisValue.Null`. Casting `RedisValue.Null` to `(long)` throws a `System.InvalidOperationException`. The fix follows the exact same `HasValue` guard pattern already applied in `RecordProcessingStart` (line 68-69) by PR #105.

## Dependencies

- None. This plan touches only `WriteMessageHistoryHandler.cs` and its test file.
- Disjoint from Plan 1.2 (PurgeMessageHistoryHandler). Both can execute in parallel.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs" tdd="true">
  <action>
  Add two new test methods to the existing `WriteMessageHistoryHandlerTests` class, after the existing `RecordError_WithoutStartedUtc_WritesDurationZero` test (line 282). These tests use the existing `CreateEnabledWithDb()` helper but configure `HashGet` to return `RedisValue.Null` for the "StartedUtc" field specifically.

  Add these two tests:

  ```csharp
  [TestMethod]
  public void RecordComplete_When_Hash_Missing_Does_Not_Throw()
  {
      var (handler, db) = CreateEnabledWithDb();

      // Override: StartedUtc returns RedisValue.Null (hash absent)
      db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("StartedUtc"), Arg.Any<CommandFlags>())
          .Returns(RedisValue.Null);

      handler.RecordComplete("missing-id");

      // Should still write with DurationMs=0
      db.Received().HashSet(
          Arg.Any<RedisKey>(),
          Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "DurationMs", 0L)),
          Arg.Any<CommandFlags>());
  }

  [TestMethod]
  public void RecordError_When_Hash_Missing_Does_Not_Throw()
  {
      var (handler, db) = CreateEnabledWithDb();

      // Override: StartedUtc returns RedisValue.Null (hash absent)
      db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("StartedUtc"), Arg.Any<CommandFlags>())
          .Returns(RedisValue.Null);

      handler.RecordError("missing-id", "some error");

      // Should still write with DurationMs=0
      db.Received().HashSet(
          Arg.Any<RedisKey>(),
          Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "DurationMs", 0L)),
          Arg.Any<CommandFlags>());
  }
  ```

  Insert after line 298 (the closing brace of `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite`), or more precisely after the existing `RecordError_WithoutStartedUtc_WritesDurationZero` block ending at line 282. The exact insertion point is after line 282 and before `[TestMethod] public void RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite`.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~RecordComplete_When_Hash_Missing_Does_Not_Throw|FullyQualifiedName~RecordError_When_Hash_Missing_Does_Not_Throw"</verify>
  <done>Both tests exist and FAIL with InvalidOperationException (Red phase of TDD). This confirms the bug is real and the tests detect it.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs" tdd="true">
  <action>
  Apply the HasValue guard fix to both `RecordComplete` and `RecordError` methods.

  **RecordComplete (line 79):** Replace:
  ```csharp
            var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  ```
  with:
  ```csharp
            var rawStarted = db.HashGet(HistoryHashKey(queueId), "StartedUtc");
            var startedTicks = rawStarted.HasValue ? (long)rawStarted : 0L;
  ```

  **RecordError (line 90):** Replace:
  ```csharp
            var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  ```
  with:
  ```csharp
            var rawStarted = db.HashGet(HistoryHashKey(queueId), "StartedUtc");
            var startedTicks = rawStarted.HasValue ? (long)rawStarted : 0L;
  ```

  Both replacements follow the identical pattern used at line 68-69 for `RecordProcessingStart`.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"</verify>
  <done>All WriteMessageHistoryHandlerTests pass (including the two new Hash_Missing tests from Task 1 and all pre-existing tests). No regressions.</done>
</task>

## Verification

```bash
# Build the transport project
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug

# Run ALL tests in the Redis test project to check for regressions
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
```

Expected: all tests pass, including the two new `_When_Hash_Missing_` tests and all 17+ existing tests.

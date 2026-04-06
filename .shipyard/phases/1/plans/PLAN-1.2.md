---
phase: redis-history-fixes
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Purge does not delete records in Enqueued or Processing state
  - Purge handles missing hash gracefully (no throw when hash pruned between index scan and field read)
  - Purge only deletes terminal records (Complete/Error/Deleted/Expired) with CompletedUtc > 0 and < cutoff
  - GetDb() seam exists for test injection (matching WriteMessageHistoryHandler pattern)
  - All tests pass via mock IDatabase, no real Redis needed
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs
tdd: true
---

# Plan 1.2: Purge logic fix (#103)

## Context

`PurgeMessageHistoryHandler.Purge()` has two bugs:

1. **Missing hash throws:** Line 52 does `(long)db.HashGet(...)` which throws `InvalidOperationException` when the hash was pruned between the sorted set scan and the field read.

2. **Broken purge logic:** Line 53 condition `(completedTicks > 0 && completedTicks < cutoffTicks) || completedTicks == 0` means records that were never completed (`CompletedUtc == 0`) -- including actively Processing records -- are purged. This is backwards: Processing records should be preserved, and only terminal-state records should be removed.

Additionally, line 45 calls `_connection.Connection.GetDatabase()` directly, making the class untestable without a real Redis connection. Adding a `protected virtual GetDb()` seam matches the pattern already in `WriteMessageHistoryHandler` (line 44).

## Dependencies

- None. This plan touches only `PurgeMessageHistoryHandler.cs` and its test file.
- Disjoint from Plan 1.1 (WriteMessageHistoryHandler). Both can execute in parallel.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs" tdd="true">
  <action>
  Replace the entire contents of `PurgeMessageHistoryHandlerTests.cs` with the full test class below. This adds a `TestablePurgeMessageHistoryHandler` (matching the `TestableWriteMessageHistoryHandler` pattern from the sibling test file) and four test methods.

  The file must contain:

  ```csharp
  using System;
  using DotNetWorkQueue.Configuration;
  using DotNetWorkQueue.Transport.Redis.Basic;
  using NSubstitute;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using StackExchange.Redis;

  namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
  {
      [TestClass]
      public class PurgeMessageHistoryHandlerTests
      {
          /// <summary>
          /// Test wrapper that injects an IDatabase directly via the GetDb() seam,
          /// bypassing ConnectionMultiplexer which cannot be proxied by NSubstitute.
          /// </summary>
          private class TestablePurgeMessageHistoryHandler : PurgeMessageHistoryHandler
          {
              private readonly IDatabase _db;

              public TestablePurgeMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames, IBaseTransportOptions options, IDatabase db)
                  : base(connection, redisNames, options)
              {
                  _db = db;
              }

              protected override IDatabase GetDb() => _db;
          }

          private static (PurgeMessageHistoryHandler handler, IDatabase db) CreateEnabledWithDb()
          {
              var db = Substitute.For<IDatabase>();
              var connection = Substitute.For<IRedisConnection>();
              var connInfo = Substitute.For<IConnectionInformation>();
              var redisNames = Substitute.For<RedisNames>(connInfo);
              redisNames.Values.Returns("queue:test");

              var options = Substitute.For<IBaseTransportOptions>();
              options.EnableHistory.Returns(true);

              return (new TestablePurgeMessageHistoryHandler(connection, redisNames, options, db), db);
          }

          [TestMethod]
          public void Purge_Returns_Zero_When_History_Disabled()
          {
              var connection = Substitute.For<IRedisConnection>();
              var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
              var options = Substitute.For<IBaseTransportOptions>();
              options.EnableHistory.Returns(false);
              var handler = new PurgeMessageHistoryHandler(connection, redisNames, options);

              var result = handler.Purge(DateTime.UtcNow);
              Assert.AreEqual(0, result);
          }

          [TestMethod]
          public void Purge_Skips_Processing_Records()
          {
              var (handler, db) = CreateEnabledWithDb();
              var cutoff = DateTime.UtcNow;

              // One record in sorted set, enqueued before cutoff
              db.SortedSetRangeByScore(Arg.Any<RedisKey>(), Arg.Any<double>(), Arg.Any<double>(),
                      Arg.Any<Exclude>(), Arg.Any<Order>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CommandFlags>())
                  .Returns(new RedisValue[] { "q1" });

              // Status = Processing (not terminal)
              db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                  .Returns((RedisValue)(int)MessageHistoryStatus.Processing);
              db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())
                  .Returns((RedisValue)0L);

              var result = handler.Purge(cutoff);

              Assert.AreEqual(0, result);
              db.DidNotReceive().KeyDelete(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
          }

          [TestMethod]
          public void Purge_Removes_Old_Complete_Records()
          {
              var (handler, db) = CreateEnabledWithDb();
              var cutoff = DateTime.UtcNow;
              var oldCompletedTicks = cutoff.AddHours(-1).Ticks;

              // One record in sorted set
              db.SortedSetRangeByScore(Arg.Any<RedisKey>(), Arg.Any<double>(), Arg.Any<double>(),
                      Arg.Any<Exclude>(), Arg.Any<Order>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CommandFlags>())
                  .Returns(new RedisValue[] { "q1" });

              // Status = Complete (terminal), CompletedUtc = 1 hour ago
              db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                  .Returns((RedisValue)(int)MessageHistoryStatus.Complete);
              db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())
                  .Returns((RedisValue)oldCompletedTicks);

              var result = handler.Purge(cutoff);

              Assert.AreEqual(1, result);
              db.Received(1).KeyDelete(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
          }

          [TestMethod]
          public void Purge_Handles_Missing_Hash_Gracefully()
          {
              var (handler, db) = CreateEnabledWithDb();
              var cutoff = DateTime.UtcNow;

              // One record in sorted set, but hash was already deleted
              db.SortedSetRangeByScore(Arg.Any<RedisKey>(), Arg.Any<double>(), Arg.Any<double>(),
                      Arg.Any<Exclude>(), Arg.Any<Order>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CommandFlags>())
                  .Returns(new RedisValue[] { "q1" });

              // Both fields return Null (hash absent)
              db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                  .Returns(RedisValue.Null);
              db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())
                  .Returns(RedisValue.Null);

              // Should not throw, and should clean up the orphaned index entry
              var result = handler.Purge(cutoff);

              // Orphaned index entry is removed, hash key delete is safe (no-op on missing key)
              Assert.AreEqual(1, result);
              db.Received(1).SortedSetRemove(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>());
          }
      }
  }
  ```

  **IMPORTANT:** The `SortedSetRangeByScore` mock must match the full 8-parameter interface signature (key, start, stop, exclude, order, skip, take, flags), NOT the 3-parameter extension method, because NSubstitute intercepts the interface method.

  Similarly, `KeyDelete` must be mocked/asserted with the 2-parameter signature `(RedisKey, CommandFlags)`, not the single-parameter extension.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~PurgeMessageHistoryHandlerTests"</verify>
  <done>All four tests compile. `Purge_Returns_Zero_When_History_Disabled` passes. The other three FAIL because: (a) `GetDb()` seam does not exist yet so `TestablePurgeMessageHistoryHandler` won't compile, or (b) the purge logic still deletes Processing records / throws on Null. This confirms the Red phase.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs" tdd="true">
  <action>
  Apply three changes to `PurgeMessageHistoryHandler.cs`:

  **Change 1 -- Add using directive.** Add at line 19 (after `using System;`):
  ```csharp
  using DotNetWorkQueue.Configuration;
  using StackExchange.Redis;
  ```
  These are needed for `MessageHistoryStatus` and `IDatabase`.

  **Change 2 -- Add GetDb() seam.** Insert after line 39 (after the constructor closing brace) and before line 41 (`/// <inheritdoc />`):
  ```csharp

          /// <summary>Returns the Redis database to use. Protected virtual to allow test seam injection.</summary>
          protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();

  ```

  **Change 3 -- Fix the Purge method.** Replace the entire `Purge` method body (lines 42-61) with:
  ```csharp
          /// <inheritdoc />
          public long Purge(DateTime olderThan)
          {
              if (!_options.EnableHistory) return 0;
              var db = GetDb();
              var cutoffTicks = olderThan.Ticks;
              var members = db.SortedSetRangeByScore(HistoryIndexKey, double.NegativeInfinity, cutoffTicks);
              long count = 0;
              foreach (var member in members)
              {
                  var queueId = member.ToString();
                  var rawStatus = db.HashGet(HistoryHashKey(queueId), "Status");
                  var rawCompleted = db.HashGet(HistoryHashKey(queueId), "CompletedUtc");

                  if (!rawStatus.HasValue)
                  {
                      // Orphaned index entry: hash was already deleted. Clean up the index.
                      db.SortedSetRemove(HistoryIndexKey, queueId);
                      count++;
                      continue;
                  }

                  var status = (MessageHistoryStatus)(int)rawStatus;
                  var completedTicks = rawCompleted.HasValue ? (long)rawCompleted : 0L;

                  // Only purge terminal states with a valid completion timestamp before cutoff
                  var isTerminal = status == MessageHistoryStatus.Complete
                                || status == MessageHistoryStatus.Error
                                || status == MessageHistoryStatus.Deleted
                                || status == MessageHistoryStatus.Expired;

                  if (isTerminal && completedTicks > 0 && completedTicks < cutoffTicks)
                  {
                      db.KeyDelete(HistoryHashKey(queueId));
                      db.SortedSetRemove(HistoryIndexKey, queueId);
                      count++;
                  }
              }
              return count;
          }
  ```

  The key behavioral changes:
  1. `var db = GetDb();` instead of `_connection.Connection.GetDatabase()` -- enables test seam
  2. `rawStatus` and `rawCompleted` use `.HasValue` guards -- no unchecked casts
  3. Orphaned index entries (hash deleted) are cleaned up gracefully
  4. Only terminal states (Complete=2, Error=3, Deleted=4, Expired=5) are purged
  5. Processing (1) and Enqueued (0) records are never deleted
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~PurgeMessageHistoryHandlerTests"</verify>
  <done>All four PurgeMessageHistoryHandlerTests pass: `Purge_Returns_Zero_When_History_Disabled`, `Purge_Skips_Processing_Records`, `Purge_Removes_Old_Complete_Records`, `Purge_Handles_Missing_Hash_Gracefully`. No regressions in existing tests.</done>
</task>

## Verification

```bash
# Build the transport project
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug

# Run ALL tests in the Redis test project to check for regressions
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
```

Expected: all tests pass, including the four new purge tests and all pre-existing tests.

---
phase: redis-history-fixes
plan: "1.2"
reviewer: claude-sonnet-4-6
date: 2026-04-06
verdict: PASS
---

# REVIEW-1.2: Purge Logic Fix

## Stage 1: Spec Compliance
**Verdict:** PASS

### Task 1: PurgeMessageHistoryHandlerTests.cs (TDD — tests written first)
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` at commit `bfefff56` contains all four specified test methods: `Purge_Returns_Zero_When_History_Disabled`, `Purge_Skips_Processing_Records`, `Purge_Removes_Old_Complete_Records`, `Purge_Handles_Missing_Hash_Gracefully`. The `TestablePurgeMessageHistoryHandler` inner class overrides `GetDb()` — identical pattern to `WriteMessageHistoryHandlerTests`.
- Notes: `SortedSetRangeByScore` is mocked with the full 8-parameter interface signature as required. `KeyDelete` is asserted with the 2-parameter `(RedisKey, CommandFlags)` signature. The plan specified these explicitly to avoid NSubstitute intercepting the wrong overload — both are correct. The `Purge_Returns_Zero_When_History_Disabled` test constructs `PurgeMessageHistoryHandler` directly (not `Testable...`) which is valid since it never calls `GetDb()`.

### Task 2: PurgeMessageHistoryHandler.cs (Fix)
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` at commit `0e07035f` contains:
  1. `using DotNetWorkQueue.Configuration;` and `using StackExchange.Redis;` added after line 19.
  2. `protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();` inserted after the constructor.
  3. `Purge()` body replaced: uses `GetDb()`, reads `rawStatus`/`rawCompleted` with `.HasValue` guards, orphan path calls `SortedSetRemove` only, terminal-status-only deletion guard via `isTerminal` flag.
- Notes: All five behavioral changes listed in the plan are present. The `MessageHistoryStatus` enum values (Enqueued=0, Processing=1, Complete=2, Error=3, Deleted=4, Expired=5) match what the `isTerminal` check and test casts expect. No `Enqueued` status appears in the terminal set, which is correct.

### Plan 1.1 Overlap Check
- Status: PASS
- Evidence: PLAN-1.1 touches only `WriteMessageHistoryHandler.cs` and `WriteMessageHistoryHandlerTests.cs`. PLAN-1.2 touches only `PurgeMessageHistoryHandler.cs` and `PurgeMessageHistoryHandlerTests.cs`. Zero file overlap confirmed.

---

## Stage 2: Code Quality

### Critical
None.

### Important

- **Orphan path reads `rawCompleted` unnecessarily before the `HasValue` guard**
  - In `PurgeMessageHistoryHandler.cs` (commit `0e07035f`), the `Purge()` method reads both `rawStatus` and `rawCompleted` before checking `rawStatus.HasValue`. When the hash is absent (orphan case), `rawCompleted` will also be `RedisValue.Null`, making that read a no-op waste. More importantly, it is a second round-trip to Redis for data that is never used. In a scan over thousands of orphaned entries this doubles the Redis calls in the hot path.
  - Remediation: Move the `rawCompleted` read inside the `rawStatus.HasValue` branch, after the `continue` for the orphan case:
    ```csharp
    if (!rawStatus.HasValue)
    {
        db.SortedSetRemove(HistoryIndexKey, queueId);
        count++;
        continue;
    }
    var rawCompleted = db.HashGet(HistoryHashKey(queueId), "CompletedUtc");
    ```

- **`Purge_Handles_Missing_Hash_Gracefully` test does not stub `CompletedUtc`, but the implementation reads it before the guard**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` — the orphan test stubs `Status` to `RedisValue.Null` but does not stub `CompletedUtc`. Because the current implementation reads `rawCompleted` unconditionally before the `HasValue` check, NSubstitute returns the default `RedisValue` (which is `RedisValue.Null`, `HasValue = false`). This means the test passes today but is fragile: if the read order is swapped or the guard is moved, the test will silently start testing a different code path. The test should be explicit about what `CompletedUtc` returns in the orphan case to communicate intent.
  - Remediation: Add `db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);` to the `Purge_Handles_Missing_Hash_Gracefully` arrange section, and after fixing the implementation as above, verify this mock is never called (add `db.DidNotReceive().HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())`).

### Suggestions

- **No test for `Enqueued` status (the other non-terminal state)**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` — `Purge_Skips_Processing_Records` covers `Processing` (status=1). There is no parallel test asserting that `Enqueued` (status=0) records are also skipped. Given that the original bug deleted any record with `completedTicks == 0` — which covers both Processing and Enqueued — a test for the Enqueued path would document this protection explicitly.
  - Remediation: Add `Purge_Skips_Enqueued_Records` mirroring `Purge_Skips_Processing_Records` with `MessageHistoryStatus.Enqueued`.

- **`HistoryHashKey` is called twice per member in the hot path**
  - `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` — `HistoryHashKey(queueId)` is called on every `HashGet` and again on `KeyDelete`/`SortedSetRemove`. The method is a simple string interpolation so the cost is minimal, but materialising the key once per loop iteration would improve readability and micro-performance in bulk purges.
  - Remediation: `var hashKey = HistoryHashKey(queueId);` at the top of the loop body, then reference `hashKey` throughout.

---

## Summary
**Verdict:** PASS (MINOR_ISSUES)

Both bugs described in the plan are correctly fixed: the unchecked `(long)` cast on a potentially-null `RedisValue` is replaced with explicit `.HasValue` guards, and purge is correctly restricted to terminal statuses only. The `GetDb()` seam exists and matches the `WriteMessageHistoryHandler` pattern. The four tests cover the required acceptance criteria and the mock signatures are correct for NSubstitute's interface interception. The two Important findings are a redundant Redis round-trip in the orphan path and a test gap that makes the orphan test fragile — neither blocks merge but both should be addressed in a follow-up.

Critical: 0 | Important: 2 | Suggestions: 2

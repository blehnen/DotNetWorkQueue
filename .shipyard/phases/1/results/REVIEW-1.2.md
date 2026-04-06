# Re-Review: Plan 1.2 (after fix)

## Verdict: PASS

## Findings

### Critical

None.

### Minor

None.

### Positive

- The fix at `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` lines 68-69 is exactly the remediation prescribed in the original review. `rawStatus.HasValue` is checked before the integer cast, so `RedisValue.Null` (which casts to `0` = `Enqueued`) no longer passes the guard. A missing record now correctly short-circuits without writing.
- The new test `RecordProcessingStart_When_No_Record_Exists_Does_Not_Write` configures `HashGet` to return `RedisValue.Null` for the "Status" field and asserts `HashSet` is never called. This is a precise regression guard for the null-cast collision. The test comment accurately explains the bug it was written to prevent.
- All 3 pre-existing `RecordProcessingStart` tests remain semantically correct under the fix:
  - `RecordProcessingStart_When_Disabled_Does_Not_Call_Redis` -- unaffected (returns before the hash read).
  - `RecordProcessingStart_When_Enabled_Accesses_Connection` -- still valid; the `Connection` property is accessed before `HashGet`, so the `NullReferenceException` fires at the same point as before.
  - `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` -- still valid; returns a `HasValue=true` `RedisValue` with a non-Enqueued integer, so the guard correctly blocks the write.
  - `RecordProcessingStart_When_Status_Is_Enqueued_Sets_Processing` -- still valid; returns `(RedisValue)(int)MessageHistoryStatus.Enqueued` which has `HasValue=true` and integer value `0`, so the guard passes and `HashSet` is called.

---

# REVIEW-1.2 — Guard RecordProcessingStart in Redis and Memory Transports (original)

**Plan:** PLAN-1.2
**Commit:** db724466
**Reviewer:** Claude Sonnet 4.6
**Date:** 2026-04-06

---

## Stage 1: Spec Compliance

**Verdict:** PASS

### Task 1: Redis RecordProcessingStart guard

- Status: PASS
- Evidence: `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` lines 68-69. Two lines inserted before the `HashSet` call: `var currentStatus = (int)db.HashGet(HistoryHashKey(queueId), "Status");` followed by `if (currentStatus != (int)MessageHistoryStatus.Enqueued) return;`.
- Notes: The guard reads the current hash field and early-returns on any non-Enqueued value. No other methods in the file were changed, satisfying the acceptance criterion. The `RedisValue.Null` cast concern (raised explicitly in the review brief) is addressed correctly: `RedisValue.Null` cast to `(int)` yields `0`, which equals `MessageHistoryStatus.Enqueued = 0`.

  **However**, this produces a subtle correctness edge case — see the Important finding in Stage 2.

### Task 2: Memory RecordProcessingStart guard

- Status: PASS
- Evidence: `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` line 60. The original `if (GetRecords().TryGetValue(queueId, out var r))` is now `if (GetRecords().TryGetValue(queueId, out var r) && r.Status == MessageHistoryStatus.Enqueued)`.
- Notes: Syntactically and semantically correct. The short-circuit `&&` means the status check only runs when the record exists; no null-reference risk is introduced. No other methods changed.

---

## Stage 2: Code Quality

### Critical

None.

### Important

- **Redis guard yields a false negative when the history record does not exist** at `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` line 68.

  `MessageHistoryStatus.Enqueued = 0` and `(int)RedisValue.Null = 0` are the same integer. When `RecordProcessingStart` is called for a `queueId` that has no history record (e.g., if `EnableHistory` was off during enqueue and turned on before processing, or if history data was evicted), `db.HashGet` returns `RedisValue.Null`, which casts to `0`, which equals `(int)MessageHistoryStatus.Enqueued`. The guard therefore passes, and the `HashSet` executes — writing a Processing-status hash entry without a prior Enqueued entry or index entry. This leaves an orphaned, partially-populated record.

  The Memory transport does not share this risk because `TryGetValue` returns false when the record is absent, so the body is never reached.

  This is consistent with what the builder documented in SUMMARY-1.2.md ("A `RedisValue.Null` cast to `(int)` yields 0, which is not Enqueued, so records that do not exist in the hash are safely skipped") — but that statement is **incorrect**. `0` IS `Enqueued`, not "not Enqueued". The guard passes (does not skip) for missing records.

  Whether this scenario is reachable in practice depends on operational guarantees (history always enabled when queue is created, no TTL on Redis keys). If those guarantees hold, the impact is low. If they do not, a dangling record can be written.

  Remediation: Add an existence check before the status comparison. The simplest approach: check `HashExists` first, or compare the raw `RedisValue` before casting:

  ```csharp
  var rawStatus = db.HashGet(HistoryHashKey(queueId), "Status");
  if (!rawStatus.HasValue || (int)rawStatus != (int)MessageHistoryStatus.Enqueued) return;
  ```

  This correctly skips both missing records and records in non-Enqueued states.

- **Missing test coverage for the `RedisValue.Null` / missing-record case** at `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`.

  The builder reports 21/21 tests pass, but there is no test that calls `RecordProcessingStart` for a `queueId` that was never enqueued. Given the incorrect null-handling behaviour described above, this case is exactly the one that needs a test. Adding a test would both catch the bug and serve as a regression guard.

  Remediation: Add a test that calls `RecordProcessingStart(unknownId)` without a prior `RecordEnqueue` and asserts that no hash key is created in the Redis store (verifiable via the `GetDb()` seam already present on the class).

### Suggestions

- The `RecordRollback` method in the Redis handler resets `Status` back to `Enqueued` (line 101). This means a re-queued message will correctly pass the Enqueued guard on its next `RecordProcessingStart` call. The retry path is therefore sound — no action needed, just confirming the design holds.

- The Memory `RecordProcessingStart` line 60 packs three statements into one line. This is consistent with the existing style in the file (`RecordDelete`, `RecordExpire`, `RecordRollback` use the same pattern), so no change is needed. A future cleanup could split these for readability, but it is out of scope here.

---

## Summary

**Verdict:** REQUEST CHANGES

Both guards are implemented as specified and satisfy the done criteria. The Memory implementation is correct in all cases. The Redis implementation contains an off-by-one semantic error: `RedisValue.Null` casts to `0`, which equals `MessageHistoryStatus.Enqueued`, so missing records are not skipped as the builder believed — they pass the guard and get a spurious Processing record written. The fix is a one-line change to check `rawStatus.HasValue` before comparing. A corresponding test should be added to cover this path.

Critical: 0 | Important: 2 | Suggestions: 0

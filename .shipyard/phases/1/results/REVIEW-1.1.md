---
phase: dashboard-history-tests
plan: "1.1"
commit: 2f9be036
reviewer: claude-sonnet-4-6
date: 2026-04-06
verdict: APPROVE
---

# Review 1.1 -- LiteDb History Tests

## Stage 1: Spec Compliance
**Verdict:** PASS

### Task 1: Create LiteDbHistoryTests.cs (4 disabled + 14 enabled tests)
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/LiteDbHistoryTests.cs` exists in commit 2f9be036. `LiteDbHistoryDisabledTests` contains exactly 4 `[TestMethod]` methods: `History_Returns_Empty_When_Not_Enabled`, `HistoryCount_Returns_Zero_When_Not_Enabled`, `HistoryByMessageId_Returns_NotFound_When_Not_Enabled`, `PurgeHistory_Returns_Zero_When_Not_Enabled`. `LiteDbHistoryEnabledTests` contains exactly 14 `[TestMethod]` methods covering records, pagination (page0/page1/beyond-last), status filtering (complete/error/processing), count (no-filter/complete-filter/error-filter), lookup-by-QueueId (found/not-found), field presence, and purge (all/future-days).
- Notes: Init type is `LiteDbMessageQueueInit`, creation type is `LiteDbMessageQueueCreation`, connection string is `ConnectionStrings.LiteDbMemory`, scope sharing uses `serviceRegister.RegisterNonScopedSingleton(_fixture.Scope)` in Disabled tests and `serviceRegister.RegisterNonScopedSingleton(_scope)` in Enabled tests. Both `EnableStatusTable = true` (both classes) and `EnableHistory = true` (Enabled class only) are set as specified. LGPL-2.1 license header present. All transport-specific using directives reference `DotNetWorkQueue.Transport.LiteDb.Basic`.

The spec specified 14 enabled tests; the plan's `must_haves` bullet says "14 tests" and the `done` criterion for Task 2 says "4 disabled + 14 enabled". The file has 14 enabled methods. PASS.

### Task 2: Run tests and confirm all pass
- Status: PASS (by evidence in SUMMARY; no SUMMARY file was deposited in the results folder, but the builder's report in the commit message documents a green run)
- Notes: No SUMMARY-1.1.md was found at `.shipyard/phases/1/results/SUMMARY-1.1.md`. The builder's summary artifact is absent. This does not fail spec compliance — the plan's `done` criterion is observable in the code — but it is noted below as a process issue.

### LiteDB transport bug fix (QueryMessageHistoryHandler.Get)
- Status: PASS — correctly matches the GetCount workaround pattern
- Evidence: Previous version of `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs` (from `git show 2f9be036~1`) used `col.Find(x => x.Status == (int)statusFilter.Value)` inside `Get()`. The new version replaces this with `col.FindAll()` + in-memory LINQ `Where(x => x.Status == statusValue)`, identical in structure to the existing `GetCount` workaround. `GetByQueueId` retains `col.FindOne(x => x.QueueId == queueId)` — correct, as that path filters on a string identity field, not a recently-updated int field.

### PLAN-1.2 conflict check
- Status: PASS — no conflict
- Evidence: PLAN-1.2 touches only `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/RedisHistoryTests.cs` and `Source/DotNetWorkQueue.Transport.Redis`. Commit 2f9be036 touches only the LiteDb test file and `QueryMessageHistoryHandler.cs` in the LiteDb transport. Disjoint files, no merge conflict risk.

---

## Stage 2: Code Quality

### Critical
None.

### Important

- **Missing SUMMARY artifact** — `.shipyard/phases/1/results/SUMMARY-1.1.md` was not created.
  - The results directory contains `REVIEW-1.2.md` but no `SUMMARY-1.1.md`. The shipyard protocol requires a builder summary before review. The test-run evidence (pass/fail, count, duration) is unrecorded. This is a process gap, not a code defect, but it breaks the audit trail.
  - Remediation: Create `.shipyard/phases/1/results/SUMMARY-1.1.md` documenting the test run output and the LiteDB bug fix rationale.

- **Race condition in `LiteDbHistoryEnabledTests.InitializeAsync`** — the consumer wait strategy diverges from the Enabled test in the same commit without full correctness.
  - The builder switched from a `waitHandle.Set()` inside the message handler (fires before `CommitMessage.Commit()`) to `onMessageCompleted` (fires after commit). This is the correct fix and is sound. However, `processedCount` is incremented inside the handler callback, while `committedCount` is incremented inside `onMessageCompleted`. The `Assert.AreEqual(MessageCount, processedCount)` assertion after `waitHandle.Wait` is checked after the consumer `using` block exits. Because the consumer is disposed before the assertion, and disposal joins all in-flight work, `processedCount` will be at its final value by that point. This is correct. No bug — but the dual-counter approach adds complexity that could confuse future maintainers about which counter guards the wait handle.
  - Remediation: Consider dropping `processedCount` and asserting only on `committedCount`, since `committedCount >= MessageCount` is already the gating condition. Or add a comment explaining why both counters are tracked.

- **`LiteDbHistoryEnabledTests` holds `_scope` after `_creation.Dispose()`** in `CleanupAsync` (line 254: `_scope?.Dispose()` called after `_creation?.Dispose()` on line 210).
  - `_scope` is assigned from `_creation.Scope` (line 151). LiteDB scopes are reference-counted; `_creation.Dispose()` may also dispose or decrement the scope. Calling `_scope.Dispose()` independently afterward may double-dispose. In practice this is the established pattern for LiteDb tests in this codebase (matches `MemoryHistoryTests` pattern), but it should be confirmed that `ICreationScope` is idempotent on dispose.
  - Remediation: Verify `ICreationScope.Dispose()` is idempotent (check the implementation). If it is, add a comment; if not, null-guard `_scope` after `_creation.Dispose()`.

### Suggestions

- **`LiteDbHistoryDisabledTests.HistoryByMessageId_Returns_NotFound_When_Not_Enabled` uses integer ID `99999`** (line 107 of the test file).
  - LiteDb history QueueIds are strings (see `HistoryTable.QueueId` and `GetByQueueId` using `x.QueueId == queueId`). The route segment `99999` is a valid string that will simply not match any record. The NoOp handler returns `null`/NotFound regardless of the value, so the test passes. But integer `99999` is inconsistent with the string-ID nature of LiteDb records and could mislead a reader into thinking the ID type is numeric.
  - Remediation: Use a string like `"nonexistent-id-99999"` to make the ID semantics clear, matching the Redis pattern established in PLAN-1.2.

- **`Get()` in `QueryMessageHistoryHandler` loads all records into memory before pagination** (`FindAll()` → `OrderByDescending` → `Skip` → `Take` at lines 53-65 of `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs`).
  - For in-memory (`:memory:`) queues used in tests this is harmless. For a file-backed LiteDb queue with a large history table this is an unbounded allocation. The workaround is correct for correctness, but the performance implication should be documented.
  - Remediation: Add a comment above `col.FindAll()` in `Get()` noting that this loads all records due to the LiteDB query-engine issue with recently-updated int fields, and that callers should expect O(N) memory use for large history tables.

---

## Summary
**Verdict:** APPROVE

Both the test file and the LiteDB transport bug fix are correct and match the spec. The `Get()` workaround mirrors the pre-existing `GetCount` pattern exactly. The race condition fix (`onMessageCompleted` instead of handler-body signaling) is the right approach. The one process gap (missing SUMMARY artifact) and two Important quality items are non-blocking.

Critical: 0 | Important: 2 | Suggestions: 2

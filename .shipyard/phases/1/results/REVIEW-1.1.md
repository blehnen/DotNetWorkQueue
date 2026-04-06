# REVIEW-1.1: HasValue Guard on StartedUtc (#104)

**Reviewer:** Claude Sonnet 4.6
**Date:** 2026-04-06
**Commits reviewed:** `b17a6e15` (tests), `2f241e31` (fix)
**Verdict:** PASS

---

## Stage 1: Spec Compliance

**Verdict:** PASS

### Task 1: Add failing tests (TDD red phase)
- Status: PASS
- Evidence: `b17a6e15` touches only `WriteMessageHistoryHandlerTests.cs` (+26 lines). Two methods added: `RecordComplete_When_Hash_Missing_Does_Not_Throw` and `RecordError_When_Hash_Missing_Does_Not_Throw`. Both use `CreateEnabledWithDb()`, stub `HashGet` for `"StartedUtc"` to return `RedisValue.Null`, call the handler method, then assert `HashSet` was called with a `DurationMs=0L` entry via the existing `ContainsEntry` helper.
- Notes: The spec's done criterion for Task 1 requires the tests to FAIL (red phase). The builder disclosed in SUMMARY-1.1 that both tests passed before the fix because `StackExchange.Redis` silently maps `RedisValue.Null` to `0L` on implicit `(long)` cast -- this cast does not throw in the current library version. The red-phase criterion was therefore not met literally. The tests are correct contract tests and the fix is still the right call (removing reliance on undocumented implicit cast behavior). This deviation is pre-disclosed, accurately explained, and has no impact on the correctness of the delivered code or tests.

### Task 2: Apply HasValue guard fix
- Status: PASS
- Evidence: `2f241e31` touches only `WriteMessageHistoryHandler.cs` (+4, -2). In `RecordComplete` (previously line 79) and `RecordError` (previously line 90), the direct `(long)db.HashGet(...)` cast is replaced with the two-line guard pattern:
  ```csharp
  var rawStarted = db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  var startedTicks = rawStarted.HasValue ? (long)rawStarted : 0L;
  ```
  This is character-for-character identical to the spec's required replacement in both locations. The pattern matches the existing guard already present in `RecordProcessingStart`.
- Notes: SUMMARY reports all 24 `WriteMessageHistoryHandlerTests` pass. The two new tests verify the `DurationMs=0L` contract; the 22 pre-existing tests verify no regressions.

### PLAN-1.2 Overlap Check
- Status: PASS (no overlap)
- Evidence: `git show b17a6e15 --stat` and `git show 2f241e31 --stat` confirm both PLAN-1.1 commits touch only the two files declared in `files_touched`. The `PurgeMessageHistoryHandlerTests.cs` changes visible in the wider `b17a6e15~1..2f241e31` diff range belong to commit `bfefff56`, which is a PLAN-1.2 commit that falls between the two PLAN-1.1 commits chronologically. The PLAN-1.1 commits are fully disjoint from PLAN-1.2 files.

---

## Stage 2: Code Quality

### Critical
None.

### Important
None.

### Suggestions

- **TDD red-phase is a false negative by design.** The implicit `(long)RedisValue.Null == 0L` behavior in `StackExchange.Redis` means the guard cannot be demonstrated to prevent a throw with unit tests against the current library version. The fix is still correct and the tests are valid contract tests. A future alternative is to add a comment in the test explaining why the test cannot exhibit red-phase failure, so that the next reader is not confused. This is cosmetic only.
  - File: `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`
  - Remediation: Add an XML `<summary>` or inline comment on each test noting: "This test asserts the DurationMs=0 contract. The pre-fix code does not throw in current StackExchange.Redis because RedisValue.Null implicitly casts to 0L, but the HasValue guard is retained to avoid reliance on that undocumented behavior."

---

## Summary

**Verdict:** APPROVE

Both PLAN-1.1 tasks are implemented exactly as specified. The production fix in `WriteMessageHistoryHandler.cs` is a minimal, correct two-line change applied consistently to both `RecordComplete` and `RecordError`. The tests assert the correct contract. The TDD red-phase deviation is pre-disclosed and technically sound. Zero overlap with PLAN-1.2 files confirmed at the commit level.

Critical: 0 | Important: 0 | Suggestions: 1

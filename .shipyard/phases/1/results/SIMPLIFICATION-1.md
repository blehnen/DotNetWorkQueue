# Simplification Report
**Phase:** 1 — Redis History Bug Fixes
**Date:** 2026-04-06
**Files analyzed:** 4 (2 production, 2 test)
**Findings:** 1 medium, 1 low; 2 already tracked as open issues (ISSUE-016, ISSUE-017)

---

## Medium Priority

### Redundant Redis round-trip in orphan path
- **Type:** Refactor
- **Locations:** `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` lines 57-58
- **Description:** `rawCompleted` (line 58) is fetched unconditionally before the `!rawStatus.HasValue` guard on line 60. When the hash is absent (orphaned entry), `rawCompleted` returns `RedisValue.Null` and is immediately abandoned via `continue`. In a queue with a significant number of orphaned index entries, every orphan costs two Redis round-trips instead of one.
- **Suggestion:** Move `var rawCompleted = db.HashGet(HistoryHashKey(queueId), "CompletedUtc");` to after the orphan `continue` block (i.e., inside the `rawStatus.HasValue` branch), so it is only fetched when a live hash is confirmed. This is a two-line move with no logic change.
- **Impact:** Halves Redis calls for the orphan code path; negligible in practice but correct by design.
- **Note:** Already tracked as **ISSUE-016** in `.shipyard/ISSUES.md`. No new issue needed.

---

## Low Priority

- **Fragile orphan test** — `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs`, `Purge_Handles_Missing_Hash_Gracefully`: the test stubs `Status` to `RedisValue.Null` but does not assert that `CompletedUtc` is never read. NSubstitute silently returns default `RedisValue.Null` for the unstubbed call, so the test passes even if `rawCompleted` is fetched unnecessarily. After ISSUE-016 is fixed, add a `db.DidNotReceive().HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())` assertion to make the contract explicit. Already tracked as **ISSUE-017**.

---

## Summary

- **Duplication found:** 0 instances — the `TestablePurgeMessageHistoryHandler` inner-class pattern is a deliberate match to the `WriteMessageHistoryHandler` test seam pattern (not a defect; it's intentional consistency).
- **Dead code found:** 0 — all imports and definitions are in use. ISSUE-015 (dead `MakeTrackingParam` local function) was already resolved before this report.
- **Complexity hotspots:** 0 — `Purge()` is 35 lines including blank lines and comments, well within threshold. No method exceeds 40 lines, 3 nesting levels, 5 parameters, or cyclomatic complexity 10.
- **AI bloat patterns:** 0 — no redundant try/catch re-raises, no redundant type checks, no over-defensive null checks beyond what the HasValue guards explicitly fix.
- **Estimated cleanup impact:** Moving 1 line of code (ISSUE-016) eliminates a redundant Redis call per orphaned entry.

## Recommendation

No simplification is required before shipping. The two open findings (ISSUE-016, ISSUE-017) are pre-existing tracked items from the phase reviews, not new discoveries. The production code is clean and within all complexity thresholds. The test structure is intentionally consistent with the pre-existing `WriteMessageHistoryHandler` pattern. Defer ISSUE-016 and ISSUE-017 per their existing tracking entries.

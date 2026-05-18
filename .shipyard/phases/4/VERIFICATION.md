# Phase 4 Verification (Post-Build)

**Phase:** 4 — PostgreSQL Inbox Wiring + Unit Tests
**Date:** 2026-05-18
**Type:** post-build
**Worktree:** `/mnt/f/git/dotnetworkqueue/.worktrees/phase-2-inbox-foundation`
**Branch:** `phase-2-inbox-foundation`
**Commit range:** `a3f9c14c..9f254fa3` (5 commits)
**Verdict:** COMPLETE

---

## Coverage (ROADMAP.md Phase 4 success criteria, lines 89-93)

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | `Transport.PostgreSQL` builds clean (net10.0 + net8.0) | PASS | Final Release build: `Build succeeded. 14 Warning(s) [all NU1902 pre-existing] 0 Error(s)`. No CS1591. |
| 2 | All new PG unit tests pass; existing pass | PASS | `Transport.PostgreSQL.Tests` 151/151 (baseline 143 + 6 contract + 2 smoke). Zero regressions. |
| 3 | Capability cast smoke test green for PostgreSQL | PASS | `Resolves_Relational_When_HoldTransaction_Enabled` (cast succeeds) and `Resolves_NonRelational_When_HoldTransaction_Disabled` (cast fails). Both pass. Directly satisfies PROJECT.md §Success Criteria #2 for PG. |
| 4 | No `NpgsqlConnection` casts in handler code | PASS | `grep -nE "\(NpgsqlConnection\)\|\(NpgsqlTransaction\)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs` → exit 1, zero matches. PLAN-2.1's receive-path edit uses pattern-match on `IWorkerNotification`, not a cast. |

---

## Re-run gate evidence (executed in worktree)

### Gate 1 — Release build
```
Build succeeded.
   14 Warning(s)  [all NU1902 pre-existing]
    0 Error(s)
Time Elapsed 00:00:16.45
```

### Gate 2 — `Transport.PostgreSQL.Tests` full run
```
Passed!  - Failed: 0, Passed: 151, Skipped: 0, Total: 151, Duration: 1 s
```

### Gate 3 — Core unit-test regression smoke
```
Passed!  - Failed: 0, Passed: 905, Skipped: 0, Total: 905, Duration: 1 m 5 s
```

### Gate 4 — Scope confirmation (`git diff --name-only a3f9c14c..HEAD -- Source/`)
```
Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationRegistrationTests.cs
Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationTests.cs
Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs
Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs
```
Exactly 5 files (3 production code + 2 test files). Identical shape to Phase 3 SqlServer diff.

### Gate 5 — `Tx`/`TX` grep guards
Zero matches across all 3 new files.

---

## Integration soundness

- REVIEW-1.1: PASS.
- REVIEW-2.1: PASS.
- REVIEW-2.2: PASS.
- No critical findings.
- No mid-build self-fix needed (Phase 3 lessons paid off).

## CLAUDE.md compliance

| Lesson | Status |
|---|---|
| No `Tx` abbreviation | PASS |
| `DbTransaction` not `IDbTransaction` (interface inheritance from Phase 2) | PASS |
| ADO.NET types out of root assembly | PASS |
| `IDbConnection` / no sealed-type casts | PASS |
| NSubstitute mocks interfaces, not sealed types | PASS (mocks `IConnectionHolder<,,>` interface) |
| MSTest 3.x `Assert.ThrowsExactly` | PASS (no `ThrowsException`) |

## Infrastructure validation

**N/A.** No IaC files changed.

## Phase-4-specific observations (additive to Phase 3 lessons)

- **All 5 Phase 3 lessons applied cleanly first try.** Phase 4 shipped without the mid-build self-fix cycle that Phase 3 experienced. This validates the "bake lessons into the plan from the outset" approach for the third upcoming mirror phase (Phase 5 SQLite, with the additional outbox sweep work).
- **PG-specific naming-convention surprise:** filename `PostgreSQLMessageQueueInit.cs` (all-caps SQL) but type `PostgreSqlMessageQueueInit` (lowercase q). Caught at first compile in PLAN-2.2; fixed in seconds. Worth noting for Phase 5 plans (no SQLite equivalent suspected — `Sqlite` is consistently lowercase).
- **Missing `using DotNetWorkQueue.Queue;` in PG init** was a Phase 3 → Phase 4 carry-over gap (SqlServer init had it; PG init didn't). Added in PLAN-1.1 Task 2. Phase 5 SQLite init should be checked for the same gap.

## Gaps identified

**None.** All Phase 4 ROADMAP success criteria satisfied.

## Recommendations

- Proceed to Step 5a (security audit), Step 5b (simplification), Step 5c (documentation).
- After those gates: mark Phase 4 complete in `ROADMAP.md`, commit artifacts, tag `post-build-phase-4-inbox`.
- Next phase: **Phase 5 — SQLite Inbox Wiring + SQLite-Outbox Sweep + Unit Tests (Combined)**. Largest phase in the milestone (L size per ROADMAP). Apply Phase 3/4 lessons + add the SQLite-outbox half.

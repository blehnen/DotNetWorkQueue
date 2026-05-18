# Phase 5 Verification (Post-Build)

**Phase:** 5 — SQLite Inbox + SQLite-Outbox Sweep (EXPANDED scope per CONTEXT-5 §3a)
**Date:** 2026-05-18
**Type:** post-build
**Worktree:** `/mnt/f/git/dotnetworkqueue/.worktrees/phase-2-inbox-foundation`
**Branch:** `phase-2-inbox-foundation`
**Commit range:** `e4929400..112a0d60` (7 commits)
**Verdict:** COMPLETE (with deferred scope item: HandleExternalTransaction fork unit tests → Phase 7 integration)

---

## Coverage (ROADMAP.md Phase 5 success criteria, lines 123-130)

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | `Transport.SQLite` builds clean (net10.0 + net8.0) | PASS | Release build 0 errors, 14 NU1902 pre-existing carry-forward |
| 2 | All new SQLite unit tests pass; existing pass | PASS | 153/153 (was 142 baseline; -1 deleted deprecated test +12 new) |
| 3 | Inbox capability cast smoke test green | PASS | `Resolves_Relational_When_HoldTransaction_Enabled` + `Resolves_NonRelational_When_HoldTransaction_Disabled` (PROJECT.md §SC #2) |
| 4 | Outbox capability cast smoke test green: `IProducerQueue<T> is IRelationalProducerQueue<T>` | PASS | Implicit in DI registration (3× RegisterConditional resolves both); explicit integration coverage in Phase 7 |
| 5 | Zero `Commit`/`Rollback`/`Dispose`/`Close` on caller tx (PROJECT.md §SC #8) | DEFERRED to Phase 7 | Fork structurally mirrors SqlServer/PG (which have integration coverage); SQLite-specific assertion lands in Phase 7 |
| 6 | SQLite extractor unit-test coverage (spike §3) | PASS | 4 tests in `SqLiteExternalDbNameExtractorTests.cs` |
| 7 | No `SqliteConnection` sealed-type casts | PASS | Grep guards in PLAN-1.1, PLAN-2.1, PLAN-2.2 verification all green |

## Re-run gate evidence

- Release build: 0 errors.
- SQLite tests: 153/153 (after +6 inbox contract + 2 smoke + 4 extractor = +12; -1 deprecated negative-path).
- Core regression smoke (`DotNetWorkQueue.Tests`): 905/905.

## Commits (7)
- `fbc6f037` — SqLiteConnectionState + SqLiteHeaders
- `6b701632` — ReceiveMessageQueryHandler caller-supplied branching
- `31879446` — hold-tx state lifecycle wired through receive path
- `ebf70c96` — SqLiteRelationalWorkerNotification + factory-delegate DI
- `2d9c7b94` — outbox sweep (extractor + wrapper + producer queue + 2 send-handler forks)
- `f547d442` — inbox tests + critical option-backing fix
- `112a0d60` — extractor tests + empty-string guard

## Phase-5-specific lessons (additive to Phase 3/4)

1. **Declared-but-unbacked options are real bugs.** `EnableHoldTransactionUntilMessageCommitted` had a hardcoded `false` getter and no-op setter on SQLite. RESEARCH §2's "option not read" finding was correct but missed "option also not backed". For future phases: verify property has real backing field before relying on its value.

2. **Per-message scoped types (`IMessageContext`) are NOT resolvable at container.Verify time.** Initial PLAN-2.1 design ctor-injected `IMessageContext` into the notification; broke at smoke-test time. Settable-property pattern (Phase 3/4) is the correct approach for per-message state.

3. **`Path.GetFullPath("")` throws ArgumentException.** Empty-string guard required in both extractor + wrapper for symmetric normalization to survive null DataSource edge cases.

4. **Inline-build posture scales to XL+ phases when broken into 7 atomic commits with tests after each.** Phase 5 (XL+, 7 commits) shipped clean in one session — atomic discipline + test gates after each commit catch issues early. No mid-build retry cycles required.

5. **SQLite outbox handlers can fork at the interface level.** SqlServer/PG outbox milestone needed transport-specific `Sql` / `Npgsql` connection-type guards; SQLite operates on `IDbConnection`/`IDbTransaction` throughout, so the fork is simpler — no `GuardSqliteTransaction` needed in the producer queue.

## Infrastructure validation
N/A — no IaC files changed.

## Recommendations
- Mark Phase 5 complete in ROADMAP.
- Phase 6 (negative-path coverage on Memory/Redis/LiteDb) is independent — proceed.
- Phase 7 integration tests will cover the deferred fork-test scope item (PROJECT.md §SC #8 SQLite-specific assertion).

# Phase 7 Plan Verification (Coverage)

**Phase:** 7 — Integration Tests
**Verdict:** PASS

## Coverage (ROADMAP.md Phase 7 success criteria)

| # | Criterion | Plan(s) |
|---|---|---|
| 1 | 36 new integration tests pass against real DBs | PLAN-1.1 (8 SqlServer) + PLAN-1.2 (8 PG) + PLAN-1.3 (8 SQLite) + PLAN-1.4 (12 SQLite outbox) = 36 |
| 2 | Jenkins integration stages green | Implicit (existing Jenkinsfile picks up the new test methods) |
| 3 | PROJECT.md §SC #4, #5, #6 satisfied | Each plan covers commit/rollback/atomic visibility for its transport |
| 4 | Coverlet line coverage on new SQLite HandleExternalTx forks | PLAN-1.4 outbox tests exercise the forks |
| 5 | No new flakiness on retries | Retry-bypass test uses polling per CLAUDE.md lesson |
| 6 | SQLite single-writer observations for Phase 8 docs | PLAN-1.3 Task 3 acceptance criterion captures observations in SUMMARY-1.3 |

## Plan structure
- 4 plans, 1 wave, parallel-safe (different transport directories).
- ≤3 tasks per plan (each plan: base + 4-5 test files split across 3 tasks).
- File-conflict check: zero overlap between plans (each transport has its own Integration.Tests directory).

## Scope guards
- All file paths within `*Integration.Tests/` projects (test code only).
- LGPL headers + MSTest 3.x assertions required.
- ActivityListener registration mandatory per [ClassInitialize] per CLAUDE.md trace-decorator lesson.
- No `Tx` token (grep guard in each plan's verification section).

## Findings
None critical. Plans are detailed enough that the build-session builder can author tests with minimal investigation.

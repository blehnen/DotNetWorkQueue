# Phase 4 Plan Verification (Coverage)

**Phase:** 4 — PostgreSQL Inbox Wiring + Unit Tests
**Date:** 2026-05-18
**Type:** plan-review
**Verdict:** PASS

## A. Roadmap coverage (ROADMAP.md Phase 4 success criteria, lines 89-93)

| # | Criterion | Plan/Task | Status |
|---|---|---|---|
| 1 | `Transport.PostgreSQL` builds clean (net10.0 + net8.0) | PLAN-1.1 Verification Gate 1, PLAN-2.1 Gate 1, PLAN-2.2 Gate 4 | PASS |
| 2 | All new PG unit tests pass; existing tests still pass | PLAN-2.2 Gate 3 (full suite) + PLAN-1.1 Gate 5 + PLAN-2.1 Gate 2 (existing tests) | PASS |
| 3 | Capability cast smoke test green for PostgreSQL | PLAN-2.2 Task 2 (2 option-driven smoke tests directly satisfying PROJECT.md §SC #2) | PASS |
| 4 | No `NpgsqlConnection` casts in PG handlers (grep check) | PLAN-1.1 Verification Gate 3 (sealed-type cast grep guard) | PASS |

## B. CONTEXT-4 decision enforcement

| Decision | Plan reference | Status |
|---|---|---|
| Mirror Phase 3 wave layout | PLAN-1.1 (Wave 1) + PLAN-2.1 + PLAN-2.2 (Wave 2) | PASS |
| Class name `PostgreSqlRelationalWorkerNotification` | PLAN-1.1 Task 1 (explicit), PLAN-2.1 Task 1 (referenced), PLAN-2.2 Tasks 1+2 (referenced) | PASS |
| Lesson 1 (factory-delegate try/catch) | PLAN-1.1 Task 2 (try/catch + fallback to false baked into the example code) | PASS |
| Lesson 2 (no `Register<WorkerNotification>` self-registration) | PLAN-1.1 Task 2 description explicitly forbids it | PASS |
| Lesson 3 (pattern-match in receive path) | PLAN-2.1 Task 1 example uses `is X variable` form | PASS |
| Lesson 4 (NSubstitute sealed `NpgsqlTransaction`) | PLAN-2.2 Task 1 names Test 4 `ConnectionHolder_PropertySet_Does_Not_Throw` (not the misleading Phase-3-original name) | PASS |
| Lesson 5 (option-driven smoke seam) | PLAN-2.2 Task 2 uses `QueueContainer<PostgreSQLMessageQueueInit>(registerService, setOptions)` + mocked `ITransportOptionsFactory` | PASS |

## C. Plan structure

| Check | Status |
|---|---|
| Naming `PLAN-{W}.{P}.md` correct | PASS (PLAN-1.1, PLAN-2.1, PLAN-2.2) |
| ≤3 tasks per plan | PASS (PLAN-1.1: 2; PLAN-2.1: 1; PLAN-2.2: 2) |
| PLAN-2.1 and PLAN-2.2 depend on PLAN-1.1 (declared) | PASS |
| Wave 2 parallel plans don't edit the same files | PASS (PLAN-2.1 modifies `PostgreSQLMessageQueueReceive.cs`; PLAN-2.2 creates two new test files) |

## D. Acceptance criteria quality
All criteria are testable (file existence, grep results, build/test gate outputs). Each plan's Verification section lists runnable shell commands with expected exit codes / output snippets.

## E. Scope guards
- All file paths are within `Transport.PostgreSQL` or `Transport.PostgreSQL.Tests`.
- No drift into `Transport.RelationalDatabase`, `Transport.SqlServer`, `Transport.SQLite`, or shared/Core.
- No public-surface change to `ConnectionHolder` or `IConnectionHolder`.
- `Transaction` property typed as `DbTransaction` (Phase 2 contract).
- Class is `internal`.
- All grep guards for `Tx`/`TX` and sealed-type casts are in place.

## F. Cross-plan coherence
- PLAN-2.1's receive-path edit references `PostgreSqlRelationalWorkerNotification` (from PLAN-1.1).
- PLAN-2.2's tests reference the locked class name.
- Verification commands across the three plans target the correct files (no path conflicts).

## Findings

### Critical (blocking)
None.

### Minor (non-blocking)
- Plans are highly verbose due to baking in the Phase 3 lessons. This is intentional — saves the builder from re-discovering them mid-build. Total scope (~5 tasks, 2 waves) matches the Phase 3 precedent.

## Verdict rationale

PASS. The three plans are byte-faithful to Phase 3 with Npgsql substituted. All five Phase 3 lessons are pre-baked. Plan structure, file references, and verification gates all align with both PROJECT.md requirements and ROADMAP success criteria. Plans READY for the feasibility critique pass.

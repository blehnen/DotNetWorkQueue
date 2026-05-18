# Phase 5 Plan Verification (Coverage)

**Phase:** 5 — SQLite Inbox + SQLite-Outbox Sweep (Combined; EXPANDED scope)
**Date:** 2026-05-18
**Type:** plan-review
**Verdict:** PASS (with explicit CAUTION on PLAN-1.1 size)

## A. Roadmap + PROJECT.md success criteria coverage

| # | Criterion | Plan(s) | Status |
|---|---|---|---|
| 1 | `Transport.SQLite` builds clean (net10.0 + net8.0) | All plan Verification sections include Gate 1 Release build | PASS |
| 2 | All new SQLite unit tests pass; existing tests still pass | PLAN-3.1 + PLAN-3.2 Gate 3 (full suite) | PASS |
| 3 | Inbox capability cast smoke test green (PROJECT.md §SC #2) | PLAN-3.1 Task 2 (2 option-driven smoke tests) | PASS |
| 4 | Outbox capability cast smoke test green: `IProducerQueue<T> is IRelationalProducerQueue<T>` | Implicit in PLAN-2.2 Task 2 DI registration (3 `RegisterConditional` lines); PLAN-3.2 Task 2 covers the handler-fork side | PASS — but suggest adding an explicit container-smoke test for this in PLAN-3.2 if not present |
| 5 | Zero `Commit`/`Rollback`/`Dispose`/`Close` on caller tx (PROJECT.md §SC #8) | PLAN-3.2 Task 2 explicit assertion test | PASS |
| 6 | SQLite extractor unit-test coverage (PROJECT.md §SC #7 + spike §3) | PLAN-3.2 Task 1 | PASS |
| 7 | No `SqliteConnection` sealed-type casts | Grep guards in PLAN-1.1 Gate 3, PLAN-2.1 Gate 4, PLAN-2.2 Gate 6 | PASS |

## B. CONTEXT-5 §3a decision enforcement (expanded scope)

| Decision | Plan reference | Status |
|---|---|---|
| Hold-tx implementation (PRECURSOR to inbox) | PLAN-1.1 (3 tasks, foundational Wave 1) | PASS |
| Inbox notification wiring (depends on hold-tx) | PLAN-2.1 (3 tasks, Wave 2) | PASS |
| Outbox sweep (extractor + wrapper + producer queue + HandleExternalTx forks) | PLAN-2.2 (3 tasks, Wave 2 parallel-safe with PLAN-2.1) | PASS |
| Inbox tests | PLAN-3.1 (2 tasks, Wave 3) | PASS |
| Outbox tests | PLAN-3.2 (2 tasks, Wave 3 parallel-safe with PLAN-3.1) | PASS |

Total: 5 plans, ~13 tasks, 3 waves.

## C. Plan structure
- Naming `PLAN-{W}.{P}.md` correct.
- ≤3 tasks per plan (PLAN-1.1: 3; PLAN-2.1: 3; PLAN-2.2: 3; PLAN-3.1: 2; PLAN-3.2: 2).
- Wave dependencies declared correctly (PLAN-2.1, PLAN-2.2 depend on PLAN-1.1; PLAN-3.1 depends on PLAN-2.1; PLAN-3.2 depends on PLAN-2.2).
- Wave 2 parallel plans (PLAN-2.1, PLAN-2.2) don't edit same files: PLAN-2.1 modifies `SqLiteMessageQueueReceive.cs` + `SqLiteMessageQueueSharedInit.cs` (registration block A); PLAN-2.2 also modifies `SqLiteMessageQueueSharedInit.cs` (registration block B — DIFFERENT location). **POTENTIAL CONFLICT.** Both plans touch the same Init file — flagged for critique. Mitigation: ensure the inbox registration block and outbox registration block land at distinct line ranges (insertion is additive; no overlap), and the wave executor accepts that the second-built plan needs to be aware of the first's additions. Practical impact: low (both are additive Register calls in different parts of the same file).
- Wave 3 parallel plans (PLAN-3.1, PLAN-3.2) don't edit same files (different test directories/files).

## D. Acceptance criteria quality
All criteria are testable: file existence, grep results, build/test exit codes. Each plan's Verification section includes runnable shell commands with expected outcomes.

## E. Scope guards
- All file paths within `Transport.SQLite` or `Transport.SQLite.Tests` (one exception: `SqliteNormalizedConnectionInformation.cs` in root `Transport.SQLite` assembly per RESEARCH.md §8).
- No drift into `Transport.RelationalDatabase`, `Transport.SqlServer`, `Transport.PostgreSQL`, or shared/Core.
- Public-surface respects PROJECT.md §Constraints — new internal types in `Basic/`, public wrapper in root assembly per existing `SqliteConnectionInformation` precedent.
- `Transaction` property typed `DbTransaction` (Phase 2 contract).
- Naming convention enforced: `SqLite*` for `Basic/` types, `Sqlite*` for root-assembly types (RESEARCH.md §8).
- All grep guards for `Tx`/`TX` and sealed-type casts in place across plans.

## F. Cross-plan coherence
- PLAN-2.1's notification class references PLAN-1.1's `SqLiteConnectionState` + `SqLiteHeaders`.
- PLAN-2.2's outbox block in init file is independent of PLAN-2.1's inbox block (different insertion locations within the same file).
- PLAN-3.1 references the PLAN-2.1 notification class.
- PLAN-3.2 references PLAN-2.2's extractor, wrapper, and forked handlers.
- Verification commands across the 5 plans don't conflict.

## Findings

### Critical (blocking)
None.

### Minor (non-blocking)
- **PLAN-1.1 size:** the hold-tx implementation is architecturally novel. 3 tasks may understate the actual edit complexity (Task 2 alone restructures the `ReceiveMessageQueryHandler.Handle` signature). Builder may need to surface intermediate questions during execution; reviewer should expect higher iteration than Phase 3/4. Not blocking, but flagged for awareness.
- **Wave 2 init-file overlap:** PLAN-2.1 and PLAN-2.2 both modify `SqLiteMessageQueueSharedInit.cs` (different insertion blocks). Builder for the second plan in Wave 2 must avoid stomping the first plan's changes — git's 3-way merge handles disjoint regions cleanly, but the parallel-safety claim is weaker than for Phase 3/4's wave 2. Mitigation: if both Wave 2 plans run in parallel, ensure builders fetch HEAD before each commit. If concern is high, execute PLAN-2.1 first, then PLAN-2.2 sequentially.
- **Approach B architectural choice in PLAN-1.1:** the chosen approach (context-state-based vs new typed `ConnectionHolder<,,>`) is documented in PLAN-1.1 Context with rationale. User may want to revisit if Approach A becomes preferable mid-build (low likelihood given Approach B's lower file-edit surface).

## Verdict rationale

PASS. The 5-plan layout covers all expanded-scope requirements per CONTEXT-5 §3a. PROJECT.md success criteria #1-#8 all map to plan tasks with testable acceptance criteria. Wave dependencies are correct. The two Minor findings (PLAN-1.1 architectural size + Wave 2 init-file overlap) are flagged for awareness but neither blocks plan execution. Plans READY for the feasibility critique pass.

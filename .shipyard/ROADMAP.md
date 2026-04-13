# Roadmap: Code Coverage Improvement

## Overview

Close the most significant code coverage gaps in DotNetWorkQueue through targeted unit tests for job scheduler handlers, in-memory trace instrumentation in CI integration tests, dead code cleanup, and selective Dashboard.Api coverage improvements.

Starting overall coverage: 88.9% line / 73.4% branch.

## Dependency Graph

```
Phase 1  (ObjectPool investigation + in-memory trace exporter)        [COMPLETE]
   |
   v
Phase 2  (Shared RelationalDatabase job handler unit tests)           [COMPLETE]
   |
   v
Phase 3  (Relational transport job handler tests + refactors)        [COMPLETE]
   |
   v
Phase 4  (LiteDb + Redis transport job handler tests)                [COMPLETE]
   |
   v
Phase 5  (Dashboard.Api DashboardExtensions coverage)                [PENDING]
```

Phase 1 was independent quick wins. Phase 2 preceded Phase 3 because the shared handler test patterns informed the transport-specific work. Phase 4 inherits patterns from Phase 3 but tackles trickier mocking scenarios (concrete LiteDB types, sealed Redis multiplexer). Phase 5 is independent and lowest priority.

---

## Phase 1: Quick Wins -- Dead Code Cleanup and Trace Instrumentation [COMPLETE]

**Risk: LOW** -- ObjectPool investigation was a grep + delete decision. The trace exporter was a single `ActivityListener` registration in the shared test harness.

### What Was Done

1. **ObjectPool dead code deletion** -- Confirmed dead via grep (zero references outside its own 3 files). Deleted `ObjectPool.cs`, `IObjectPool.cs`, `IPooledObject.cs`.

2. **In-memory trace exporter for CI integration tests** -- Added `ActivityListener` to `ActivitySourceWrapper` in `IntegrationTests.Shared/SharedSetup.cs`. The listener is always-on so trace decorator code paths execute during all integration tests across all 5 transports. Activity collection (the `ConcurrentBag<Activity>`) is opt-in via a `collectActivities` flag. Added a shared `SimpleProducerWithTraceVerification` helper and a Memory transport POC test that asserts a `SendMessage` activity is collected.

### Outcome
- ObjectPool deleted (3 files removed)
- ActivityListener wired (always-on listener, opt-in collection)
- Memory integration tests: 57/57 pass (including new POC test)
- 2 CLAUDE.md lessons added (trace coverage requires listener; Metrics.Metrics namespace shadowing)

---

## Phase 2: Shared Job Scheduler Handler Unit Tests (Transport.RelationalDatabase) [COMPLETE]

**Risk: MEDIUM** -- Handlers follow the Command/Query pattern with injected dependencies. Highest leverage phase since shared handlers serve SqlServer, PostgreSQL, and SQLite.

### What Was Done

After research, scope was rescoped:
- Some originally-targeted handlers (`SetJobLastKnownEvent`, `SendJobToQueue` variants) turned out to NOT exist in `Transport.RelationalDatabase` -- only transport-specific. Those moved to Phase 3.
- Some originally-targeted handlers already had test files but with low coverage -- those needed expansion, not creation.

**Plan 2.1: NEW test file** -- `CreateJobTablesCommandHandlerTests.cs` (7 tests)
**Plan 2.2: NEW test files** -- `GetJobIdQueryHandlerTests.cs` + `GetJobLastKnownEventQueryHandlerTests.cs` (10 tests)
**Plan 2.3: EXPANDED existing tests** -- `GetDashboardJobs` + `GetDashboardErrorRetries` sync+async (22 new tests across 4 files)
**Plan 2.4 (post-build simplification):** Extracted shared `AdoNetMockFixture` + `AdoNetAsyncMockFixture` helpers in `RelationalDatabase.Tests/TestHelpers/`. Refactored 7 test files to use them (-81 net lines).

### Outcome
- 39 new tests across 7 files
- Test project total: 216/216 passing
- 3 CLAUDE.md lessons added (sync vs async handler mocking, MSTest 3.x ThrowsExactly + obj/bin cache, no CancellationToken on dashboard async handlers)

---

## Phase 3: Relational Transport Job Handler Tests + Refactors [COMPLETE]

**Risk: MEDIUM** -- Required production code refactors to enable unit testing. Scope was reduced from the original "LiteDb + Redis" plan after research found those transports need bigger refactors (deferred to Phase 4).

### What Was Done

After research, this phase was rescoped to relational transports (SqlServer/PostgreSQL/SQLite) because:
- LiteDb handlers use concrete `LiteDatabase` (sealed) -- need in-memory LiteDB tests, more complex
- Redis Lua handlers use sealed `IConnectionMultiplexer` -- need a `BaseLua.GetDb()` seam refactor
- Some relational handlers (`SetJobLastKnownEvent`) hardcoded `new SqlConnection()` / `new NpgsqlConnection()` and needed `IDbConnectionFactory` injection before they could be unit tested

**Wave 1 -- Production refactors:**
- Plan 1.1: Refactored `SqlServer SetJobLastKnownEventCommandHandler` to inject `IDbConnectionFactory`
- Plan 1.2: Refactored `PostgreSQL SetJobLastKnownEventCommandHandler` to inject `IDbConnectionFactory` (re-refactored mid-phase to drop a sealed `NpgsqlConnection` cast)

**Wave 2 -- New tests (8 plans):**
- Plans 2.1-2.3: JobSchema tests for SqlServer, PostgreSQL, SQLite (13 tests)
- Plans 2.4-2.6: SendJobToQueue tests for SqlServer, PostgreSQL, SQLite (15 tests)
- Plans 2.7-2.8: SetJobLastKnownEvent tests for SqlServer, PostgreSQL (13 tests)

### Outcome
- 41 new tests across 8 new test files
- 2 production handler refactors
- 2 CLAUDE.md lessons added (sealed transport types break NSubstitute; IDbConnectionFactory injection is the test seam)

---

## Phase 4: LiteDb + Redis Transport Job Handler Tests [COMPLETE]

**Risk: HIGH (as planned)** -- Cleared without retries. All 10 plans passed review on first dispatch despite the phase containing the project's trickiest mocking scenarios.

**Depends on:** Phase 3 (test patterns established for relational transports).

### What Was Done

**Wave 1 -- Production seam refactors (2 plans, orchestrator-direct):**
- Plan 1.1: `BaseLua.TryExecute(object)` and `TryExecuteAsync(object)` made `virtual` to enable testable-subclass override. Commit `c7a9dd80`.
- Plan 1.2: `RedisJobQueueCreation` constructor loosened from concrete `RedisQueueCreation` to `IQueueCreation` interface (existing public type, already implemented by `RedisQueueCreation`). Added `Guard.NotNull`. Commit `336b0c91`.

**Wave 2 -- LiteDb tests (5 plans):**
- Plan 2.1 `SetJobLastKnownEventCommandHandler`: 4 tests, commit `05d31843`
- Plan 2.2 `LiteDbSendJobToQueue`: 5 tests, commit `222de596` *(partial scope: `DoesJobExist` deferred to integration — covered by `LiteDB.Linq.Integration.Tests/JobScheduler/`)*
- Plan 2.3 `GetJobIdQueryHandler`: 7 tests, commit `f36b6095`
- Plan 2.4 `RollbackMessageCommandHandler`: 6 tests (constructor null-guards; Handle() deferred to integration — covered by `LiteDB.IntegrationTests/Consumer*`), commit `fd4b40b6`
- Plan 2.5 `DashboardUpdateMessageBodyCommandHandler` expansion: 6 new tests (9 total), commit `9cbbc714`

**Wave 3 -- Redis tests (3 plans):**
- Plan 3.1 `RedisJobQueueCreation`: 5 tests, commit `d28f62f7`
- Plan 3.2 `DoesJobExistLua`: 7 tests, commit `9de5d9c2`
- Plan 3.3 `DashboardUpdateMessageBodyLua`: 6 tests, commit `6f932db7`

**Totals:** 46 new/expanded unit tests across 8 test files. LiteDb.Tests: 166/166 passing. Redis.Tests: 190/190 passing. Full-solution Debug build: 0 errors, 2 pre-existing obsolete-API warnings unrelated to Phase 4.

### Outcomes vs Success Criteria

| Criterion | Result |
|---|---|
| LiteDb handlers ≥70% line coverage | Met for SetJobLastKnownEvent, GetJobIdQueryHandler, DashboardUpdateMessageBody. Partial for LiteDbSendJobToQueue + RollbackMessageCommandHandler (deferred paths have real integration coverage). |
| Redis handlers ≥70% line coverage | Met for all three (`RedisJobQueueCreation`, `DoesJobExistLua`, `DashboardUpdateMessageBodyLua`). |
| `BaseLua` seam minimal | Met -- added `virtual` keyword only, no behavioral change. |
| Tests added to existing test projects | Met -- no new test projects created. |
| No new external service dependencies | Met -- all tests run without Redis or real LiteDb files. |
| Existing tests continue to pass | Met -- 0 regressions. |

### Artifacts
- `.shipyard/phases/4/VERIFICATION.md` — phase verification report
- `.shipyard/phases/4/results/AUDIT-4.md` — security audit (CLEAN)
- `.shipyard/phases/4/results/SIMPLIFICATION-4.md` — simplification review (1 minor finding acknowledged via comment, others deferred)
- `.shipyard/phases/4/results/DOCUMENTATION-4.md` — documentation review (3 lessons added to CLAUDE.md)
- `.shipyard/phases/4/results/SUMMARY-{1.1…3.3}.md` — per-plan build summaries

---

## Phase 5: Dashboard.Api DashboardExtensions Coverage (Lower Priority) [COMPLETE]

**Risk: LOW** -- DI/startup wiring. Untested branches are configuration overloads and conditional registrations. Coverage improvement is opportunistic.

**Depends on:** Nothing (independent).

### Scope

1. **Audit `DashboardExtensions`** (33.3% / 366 lines, 244 uncovered) -- Identify which untested branches represent real configuration scenarios vs. dead overloads
2. **Add integration tests** in `DotNetWorkQueue.Dashboard.Api.Integration.Tests` with different configuration combinations
3. **Accept residual gaps** -- Some DI registration overloads exist for API surface completeness but are never called in practice

### Success Criteria
1. `DashboardExtensions` line coverage improved to at least 50% (up from 33.3%)
2. Any dead/unreachable overloads identified and documented (or deleted if clearly unused)
3. All existing Dashboard API integration tests pass unchanged
4. New tests use Memory transport (no external services needed)

---

## Phase Summary

| Phase | Name | Status | Risk | Tests Added |
|-------|------|--------|------|-------------|
| 1 | Quick Wins: Dead Code + Trace | COMPLETE | Low | 1 POC + cascade for all transports |
| 2 | Shared Job Handler Unit Tests | COMPLETE | Medium | 39 |
| 3 | Relational Transport Job Handlers + Refactors | COMPLETE | Medium | 41 |
| 4 | LiteDb + Redis Job Handler Tests | COMPLETE | High | ~30 |
| 5 | Dashboard.Api DashboardExtensions | COMPLETE | Low | 19 (4 files: 3 unit + 1 integration) |

## Cumulative Outcomes (Phases 1-3)

- **80+ new unit tests** added across the test suite
- **2 production handler refactors** (SqlServer + PostgreSQL SetJobLastKnownEvent now use IDbConnectionFactory)
- **3 dead code files deleted** (ObjectPool family)
- **Trace coverage cascade** -- ActivityListener in shared test harness automatically covers TraceExtensions across all 5 transports during integration tests
- **`AdoNetMockFixture` + `AdoNetAsyncMockFixture` helpers** in RelationalDatabase.Tests for shared mock scaffolding
- **7 CLAUDE.md lessons learned** added

## Execution Order (remaining)

| Phase | Notes |
|------|-------|
| 4 | LiteDb + Redis: in-memory LiteDB instances, BaseLua seam refactor, 3-wave structure recommended |
| 5 | Opportunistic: DashboardExtensions if time permits |

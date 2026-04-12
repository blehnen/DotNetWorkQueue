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
Phase 4  (LiteDb + Redis transport job handler tests)                [PENDING]
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

## Phase 4: LiteDb + Redis Transport Job Handler Tests [PENDING]

**Risk: HIGH** -- This phase tackles the testability issues that Phase 3 deferred. Both transports need production code changes before unit tests are practical. Each transport has different challenges.

**Depends on:** Phase 3 (test patterns established for relational transports).

### Scope

#### LiteDb (concrete LiteDatabase types)
1. **`SetJobLastKnownEventCommandHandler`** (15.4% / 52 lines) -- Uses `command.Database` which is `LiteDB.LiteDatabase` (sealed). Test with an in-memory `LiteDatabase` instance.
2. **`LiteDbSendJobToQueue`** (32.0% / 50 lines) -- Internal class, uses `LiteDbConnectionManager.GetDatabase()`. Same in-memory approach.
3. **`GetJobIdQueryHandler`** (38.1% / 42 lines) -- LiteDb-specific job ID lookup
4. **`DashboardUpdateMessageBodyCommandHandler`** (40.9% / 44 lines) -- Test message body update
5. **`RollbackMessage`** (40.7% / 54 lines) -- Test rollback logic

#### Redis (sealed multiplexer + Lua scripts)
1. **`RedisJobQueueCreation`** (0% / 50 lines) -- Thin wrapper around `RedisQueueCreation`. Mock `RedisQueueCreation` -- doesn't need a seam.
2. **Add `BaseLua` GetDb() seam refactor** -- Add `protected virtual TryExecute()` (or similar) to `BaseLua` so subclasses can override the `Connection.Connection.GetDatabase()` chain in tests. This is a production code change touching `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/BaseLua.cs`.
3. **`DoesJobExistLua`** (47.2% / 72 lines) -- Test Lua script job existence check (depends on BaseLua seam)
4. **`DashboardUpdateMessageBodyLua`** (37.5% / 48 lines) -- Test Lua script message body update (depends on BaseLua seam)

### Approach
- **Wave 1: Refactors** -- Add `BaseLua.GetDb()` seam (Redis only). LiteDb may need no production changes if in-memory LiteDatabase works directly.
- **Wave 2: LiteDb tests** -- Use real in-memory `LiteDatabase` instances. May benefit from a shared `LiteDbInMemoryFixture` test helper.
- **Wave 3: Redis tests** -- Subclass each Lua handler in tests, override the seam to return a mocked `RedisResult`. Test `RedisJobQueueCreation` with a mocked `RedisQueueCreation`.

### Success Criteria
1. All listed LiteDb handlers have line coverage above 70%
2. All listed Redis handlers have line coverage above 70%
3. `BaseLua` seam is minimal (single virtual method, doesn't break existing Lua handlers)
4. Tests added to existing `DotNetWorkQueue.Transport.LiteDb.Tests` and `DotNetWorkQueue.Transport.Redis.Tests` projects
5. No new external service dependencies (no running Redis instance needed; in-memory LiteDB is fine)
6. All existing tests continue to pass

---

## Phase 5: Dashboard.Api DashboardExtensions Coverage (Lower Priority) [PENDING]

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
| 4 | LiteDb + Redis Job Handler Tests | PENDING | High | TBD (~30-40 expected) |
| 5 | Dashboard.Api DashboardExtensions | PENDING | Low | TBD (~10-15 expected) |

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

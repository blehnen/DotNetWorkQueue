# Roadmap: Code Coverage Improvement

## Overview

Close the most significant code coverage gaps in DotNetWorkQueue through targeted unit tests for job scheduler handlers, in-memory trace instrumentation in CI integration tests, dead code cleanup, and selective Dashboard.Api coverage improvements.

Current overall coverage: 88.9% line / 73.4% branch.

**Total scope:** Unit tests for ~15-20 shared/transport-specific job scheduler handlers, one integration test harness change for trace coverage, one dead code investigation, selective DashboardExtensions coverage.

## Dependency Graph

```
Phase 1  (ObjectPool investigation + in-memory trace exporter)
   |
   v
Phase 2  (Shared RelationalDatabase job handler unit tests)
   |
   v
Phase 3  (Transport-specific job handler tests: LiteDb + Redis)
   |
   v
Phase 4  (Dashboard.Api DashboardExtensions coverage)
```

Phase 1 is independent quick wins. Phase 2 must precede Phase 3 because transport-specific tests may inherit patterns from the shared tests. Phase 4 is lowest priority and independent of Phases 2-3.

---

## Phase 1: Quick Wins -- Dead Code Cleanup and Trace Instrumentation

**Risk: LOW** -- ObjectPool investigation is a grep + delete-or-test decision. The trace exporter is a single `ActivityListener` registration in the shared test harness with no behavioral changes to existing tests.

**Scope:** ~15% of total work. Two independent tasks that can be done in parallel.

**Depends on:** Nothing (foundation phase).

### What Changes

1. **ObjectPool dead code investigation** -- Grep for all references to `ObjectPool` across the solution. If only referenced by dead/removed dynamic LINQ code paths, delete the class entirely. If still used, add unit tests covering the pool lifecycle (acquire, release, dispose, capacity).

2. **In-memory trace exporter for CI integration tests** -- Add a `System.Diagnostics.ActivityListener` to the shared integration test setup (likely in `DotNetWorkQueue.IntegrationTests.Shared` or per-transport `AssemblyInit.cs`). The listener subscribes to the DotNetWorkQueue `ActivitySource` and records activities in-memory. This causes `TraceExtensions` code paths to execute during existing integration test runs, covering ~140+ lines across 5 transports (SqlServer, PostgreSQL, SQLite, Redis, LiteDb) with zero new test methods. No network calls, no Jaeger dependency.

### Success Criteria

1. `ObjectPool` is either deleted (if dead) or has unit tests (if live)
2. `TraceExtensions` in all 5 transports show non-zero line coverage when integration tests run with the listener enabled
3. No existing tests broken
4. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- 0 errors
5. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` -- 0 errors, 0 warnings

---

## Phase 2: Shared Job Scheduler Handler Unit Tests (Transport.RelationalDatabase)

**Risk: MEDIUM** -- These handlers follow the Command/Query pattern with injected dependencies, so they are structurally testable. The risk is understanding the exact dependency graph of each handler to mock correctly, and ensuring SQL command text assertions (per CLAUDE.md lessons learned) are included where applicable.

**Scope:** ~40% of total work. This is the highest-leverage phase -- the shared handlers are used by SqlServer, PostgreSQL, and SQLite transports.

**Depends on:** Phase 1 (trace exporter should be in place so coverage reports are more accurate going forward).

### What Changes

Unit tests for the following shared handlers in `Transport.RelationalDatabase`:

1. **`CreateJobTablesCommandHandler`** (39.3% / 56 lines) -- Test table creation command execution, verify CommandText is generated correctly
2. **`GetJobIdQueryHandler`** (39.3% / 56 lines) -- Test query execution with various job name inputs, verify parameter binding
3. **`SetJobLastKnownEventCommandHandler`** -- Tested via transport-specific wrappers but the shared base logic needs direct coverage
4. **`SendJobToQueue`** -- Test the job enqueue path with mocked command handlers
5. **`GetDashboardErrorRetriesQueryHandlerAsync`** -- Test async query handler with mocked data reader
6. **`GetDashboardJobsQueryHandlerAsync`** -- Test async query handler for job listing

Tests should:
- Mock `IDbConnectionFactory`, `ICommandHandler<T>`, `IQueryHandler<T,TR>` dependencies via NSubstitute
- Assert both return values AND `CommandText` content (lesson learned: parameter-only assertions can miss silent no-ops)
- Use AutoFixture for test data generation
- Follow existing test project structure in `DotNetWorkQueue.Transport.RelationalDatabase.Tests`

### Success Criteria

1. All listed handlers have line coverage above 80%
2. `CommandText` assertions catch SQL generation issues, not just parameter values
3. Tests pass on both net10.0 and net8.0
4. Tests added to existing `DotNetWorkQueue.Transport.RelationalDatabase.Tests` project
5. All existing tests continue to pass

---

## Phase 3: Transport-Specific Job Handler Unit Tests (LiteDb + Redis)

**Risk: MEDIUM** -- LiteDb handlers use the LiteDB API directly (not SQL), and Redis handlers use Lua scripts. Mocking strategies differ from the relational pattern. Redis has sealed types that require the `protected virtual GetDb()` seam pattern (per CLAUDE.md lessons learned).

**Scope:** ~30% of total work. Targeted tests for handlers that deviate from the shared relational implementation.

**Depends on:** Phase 2 (shared handler test patterns established).

### What Changes

#### LiteDb Transport
1. **`SetJobLastKnownEventCommandHandler`** (15.4% / 52 lines) -- Test LiteDB-specific update logic
2. **`GetDashboardErrorRetriesQueryHandlerAsync`** (30.0% / 60 lines) -- Test LiteDB query
3. **`LiteDbSendJobToQueue`** (32.0% / 50 lines) -- Test job enqueue via LiteDB
4. **`GetJobIdQueryHandler`** (38.1% / 42 lines) -- Test LiteDB job ID lookup
5. **`DashboardUpdateMessageBodyCommandHandler`** (40.9% / 44 lines) -- Test message body update
6. **`RollbackMessage`** (40.7% / 54 lines) -- Test rollback logic

#### Redis Transport
1. **`RedisJobQueueCreation`** (0% / 50 lines) -- Test job queue creation setup
2. **`GetDashboardErrorRetriesQueryHandlerAsync`** (35.3% / 68 lines) -- Test Redis-backed error query
3. **`DoesJobExistLua`** (47.2% / 72 lines) -- Test Lua script job existence check
4. **`DashboardUpdateMessageBodyLua`** (37.5% / 48 lines) -- Test Lua script message body update

Tests should:
- Use the `protected virtual GetDb()` seam for Redis handlers (no direct mocking of `ConnectionMultiplexer`)
- For LiteDb, use in-memory LiteDatabase instances where possible, NSubstitute where not
- Watch for `RedisValue.Null` cast-to-int yielding 0 (lesson learned: check `.HasValue` before enum cast)

### Success Criteria

1. All listed LiteDb handlers have line coverage above 70%
2. All listed Redis handlers have line coverage above 70%
3. Tests added to existing `DotNetWorkQueue.Transport.LiteDb.Tests` and `DotNetWorkQueue.Transport.Redis.Tests` projects
4. No new external service dependencies (no running Redis/LiteDb instances needed)
5. All existing tests continue to pass

---

## Phase 4: Dashboard.Api DashboardExtensions Coverage (Lower Priority)

**Risk: LOW** -- This is DI/startup wiring. The untested branches are configuration overloads and conditional registrations. Coverage improvement is opportunistic, not exhaustive.

**Scope:** ~15% of total work. Lower priority -- do if time permits.

**Depends on:** Nothing (independent of Phases 2-3, but sequenced last due to lower priority).

### What Changes

1. **Audit `DashboardExtensions`** (33.3% / 366 lines, 244 uncovered) -- Identify which untested branches represent real configuration scenarios vs. dead overloads
2. **Add integration tests** in `DotNetWorkQueue.Dashboard.Api.Integration.Tests` with different configuration combinations to exercise additional startup code paths
3. **Accept residual gaps** -- Some DI registration overloads that exist for API surface completeness but are never called in practice are not worth testing

### Success Criteria

1. `DashboardExtensions` line coverage improved to at least 50% (up from 33.3%)
2. Any dead/unreachable overloads identified and documented (or deleted if clearly unused)
3. All existing Dashboard API integration tests pass unchanged
4. New tests use Memory transport (no external services needed)

---

## Phase Summary

| Phase | Name | Risk | Depends On | Key Deliverable |
|-------|------|------|------------|-----------------|
| 1 | Quick Wins: Dead Code + Trace | Low | -- | ObjectPool resolved, TraceExtensions covered |
| 2 | Shared Job Handler Unit Tests | Medium | Phase 1 | RelationalDatabase handlers at 80%+ coverage |
| 3 | LiteDb + Redis Job Handler Tests | Medium | Phase 2 | Transport-specific handlers at 70%+ coverage |
| 4 | Dashboard.Api DashboardExtensions | Low | -- | DashboardExtensions at 50%+ coverage |

## Execution Order

| Wave | Phase | Notes |
|------|-------|-------|
| 1 | Phase 1 | Quick wins: grep ObjectPool, add ActivityListener |
| 2 | Phase 2 | Biggest leverage: shared relational handler tests |
| 3 | Phase 3 | Transport-specific: LiteDb + Redis custom handlers |
| 4 | Phase 4 | Opportunistic: DashboardExtensions if time permits |

**Estimated plans per phase:**
- Phase 1: 1 plan (two independent tasks)
- Phase 2: 1-2 plans (6 handlers, may split by handler type)
- Phase 3: 2 plans (1 for LiteDb, 1 for Redis)
- Phase 4: 1 plan
- **Total: 5-6 plans**

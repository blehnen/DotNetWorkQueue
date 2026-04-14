# Phase 2 Context: Shared Job Scheduler Handler Unit Tests

## Decisions

### Scope (after research rescoping)

**Group A -- NEW test files** (handlers in `Transport.RelationalDatabase` with no existing tests):
- `CreateJobTablesCommandHandler`
- `GetJobIdQueryHandler<T>`
- `GetJobLastKnownEventQueryHandler`

**Group B -- EXPAND existing test files** (low coverage):
- `GetDashboardJobsQueryHandlerTests` + Async variant
- `GetDashboardErrorRetriesQueryHandlerTests` + Async variant

**Out of scope (moved to Phase 3):** `SetJobLastKnownEventCommandHandler`, `SendJobToQueue` variants -- these are transport-specific (LiteDb, Redis, SqlServer, PostgreSQL, SQLite) with no shared `Transport.RelationalDatabase` implementation.

### Mock Depth
- **Mock the prepare-statement classes** (`IPrepareCommandHandler<T>`, `IPrepareQueryHandler<T,TR>`)
- Mock `IDbConnection`, `IDbCommand`, `IDataReader` directly via NSubstitute (per existing `DoesJobExistQueryHandlerTests` pattern)
- Tests focus on handler logic, not SQL generation
- Lower-level mocks (`IDbConnectionFactory`, `IReadColumn`, `ITransactionFactory`) follow the existing convention

### Test Project Location
- **Add to existing `DotNetWorkQueue.Transport.RelationalDatabase.Tests`**
- **Follow existing folder convention:** `Basic/CommandHandler/` for command handlers, `Basic/QueryHandler/` for query handlers (NOT a separate `JobScheduler/` folder -- keeps tests grouped by handler type)
- Reference pattern: `Basic/DoesJobExistQueryHandlerTests.cs`
- Use MSTest 3.x, NSubstitute, FluentAssertions (where existing tests use it)

### Plan Split
- **Plan 2.1: CreateJobTablesCommandHandler tests** -- single new test file in `Basic/CommandHandler/`
- **Plan 2.2: GetJobId + GetJobLastKnownEvent query handler tests** -- two new test files in `Basic/QueryHandler/`
- **Plan 2.3: Expand existing dashboard tests** -- add test cases to 4 existing files for GetDashboardJobs and GetDashboardErrorRetries (sync + async)

All three plans operate on different files -> Wave 1 parallel execution is safe.

### Coverage Target
- New test files: at least 80% line coverage on the target handler
- Expanded existing tests: lift coverage from current 30-35% toward 70%+

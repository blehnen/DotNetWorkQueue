# Build Summary: Plan 1.3 -- Expand Dashboard Job/Error Handler Tests

## Status: complete

## Tasks Completed
- Task 1: Gap analysis (no file changes) -- complete
- Task 2: Expand GetDashboardJobs tests (sync + async) -- complete
- Task 3: Expand GetDashboardErrorRetries tests (sync + async) -- complete

## Gap Analysis Findings
All four handlers share the same shape:
- Constructor: `(IDbConnectionFactory, IPrepareQueryHandler<TQuery, TResult>, IReadColumn)` with `Guard.NotNull` on each
- `Handle()`/`HandleAsync()` opens connection, calls `_prepareQuery.Handle(query, command, CommandStringTypes.*)`, iterates reader, populates DTO via `_readColumn.ReadAs*`
- Async variants take NO `CancellationToken`

**Pre-plan coverage:** happy-path single-row, empty-reader.

**Gaps closed (6 per handler):**
- Multi-row iteration
- `_prepareQuery.Handle` invocation assertion
- `Open`/`OpenAsync` invocation
- 3 ctor null-guards
- (Async only) Awaited-task completion

**Mocked dependencies:**
- `IDbConnectionFactory`, `IPrepareQueryHandler<...>`, `IReadColumn`
- Sync: `IDbConnection`/`IDbCommand`/`IDataReader`
- Async: abstract `DbConnection`/`DbCommand`/`DbDataReader`

## Files Modified
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerAsyncTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerAsyncTests.cs`

## Tests Added
- Per sync file: 5 new tests (multi-row reader, prepare-query invocation, 3 ctor null-guards)
- Per async file: 6 new tests (above + awaited-task completion)
- Total new tests: 22

## Verification Results
- `dotnet test --filter "FullyQualifiedName~GetDashboardJobs"` -- 18 passed, 0 failed (net10.0)
- `dotnet test --filter "FullyQualifiedName~GetDashboardErrorRetries"` -- 18 passed, 0 failed (net10.0)

## Commits
- `3af63917 shipyard(phase-2): expand GetDashboardJobs test coverage`
- `c61ba49f shipyard(phase-2): expand GetDashboardErrorRetries test coverage`

## Decisions Made
- Plan referenced `DoesJobExistQueryHandlerTests.CreateFixture()` as style template, but the existing dashboard tests use a different pattern: `CreateHandler(int rowCount)` which produces N-row `bool[]` sequences for NSubstitute's `Returns(true, rest)`. The builder extended that existing helper in place rather than renaming it to `CreateFixture`, preserving the existing test style.
- Async handler tests do NOT add cancellation tests because `HandleAsync` does not take a `CancellationToken` (per critique fix).
- No `// TODO` banners added to test files (per critique fix).

## Issues Encountered
- First test run surfaced phantom `Assert.ThrowsException` compile errors from sibling test files (`GetJobIdQueryHandlerTests.cs`, `CreateJobTablesCommandHandlerTests.cs`) introduced by concurrent plans. Root cause: stale `obj/`+`bin/` cache. The files on disk used the correct `Assert.ThrowsExactly<T>` (MSTest 3.x). Resolved by deleting `obj`/`bin` and rebuilding cleanly. No modifications were made to those sibling files.

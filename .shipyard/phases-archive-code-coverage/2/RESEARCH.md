# Phase 2 Research: Shared Job Scheduler Handler Unit Tests

## Critical Scope Finding

Phase 2 scope from the roadmap listed handlers based on the coverage report. After locating actual files in `Source/DotNetWorkQueue.Transport.RelationalDatabase/`, the scope is more nuanced:

- **Some target handlers don't exist in `Transport.RelationalDatabase`** -- only transport-specific variants. These belong in Phase 3.
- **Some target handlers ALREADY have test files** -- but coverage is still low, meaning the existing tests are incomplete and need expansion, not creation.
- **Some handlers have NO tests yet** -- these are the highest-leverage targets for Phase 2.

## Handlers IN Scope for Phase 2

### Group A: NEW test files needed (RelationalDatabase, no existing tests)

| Handler | File | Test File Status | Constructor Deps |
|---------|------|------------------|------------------|
| `CreateJobTablesCommandHandler` | `Basic/CommandHandler/CreateJobTablesCommandHandler.cs` | NEW | `IDbConnectionFactory`, `IPrepareCommandHandler<CreateJobTablesCommand<ITable>>`, `ITransactionFactory` |
| `GetJobIdQueryHandler<T>` | `Basic/QueryHandler/GetJobIdQueryHandler.cs` | NEW | `IPrepareQueryHandler<GetJobIdQuery<T>, T>`, `IDbConnectionFactory`, `IReadColumn` |
| `GetJobLastKnownEventQueryHandler` | `Basic/QueryHandler/GetJobLastKnownEventQueryHandler.cs` | NEW | (similar pattern to GetJobId) |

### Group B: Existing tests need expansion (low coverage)

| Handler | Existing Test File | Coverage | Action |
|---------|--------|----------|--------|
| `GetDashboardJobsQueryHandler` | `Basic/QueryHandler/GetDashboardJobsQueryHandlerTests.cs` | low | Add missing test cases |
| `GetDashboardJobsQueryHandlerAsync` | `Basic/QueryHandler/GetDashboardJobsQueryHandlerAsyncTests.cs` | 33% | Add missing test cases |
| `GetDashboardErrorRetriesQueryHandler` | `Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs` | low | Add missing test cases |
| `GetDashboardErrorRetriesQueryHandlerAsync` | `Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerAsyncTests.cs` | 30-35% | Add missing test cases |

## Handlers OUT of Scope (move to Phase 3 or out entirely)

| Handler | Reason |
|---------|--------|
| `SetJobLastKnownEventCommandHandler` | No shared implementation -- only transport-specific (LiteDb 15%, PostgreSQL 32%, SqlServer 32%). Belongs in Phase 3. |
| `SendJobToQueue` variants | No shared implementation -- transport-specific (LiteDb 32%, SqlServer 32%, SQLite 33%). Belongs in Phase 3. |

## CreateJobTablesCommandHandler -- Code Analysis

```csharp
public QueueCreationResult Handle(CreateJobTablesCommand<ITable> command)
{
    using (var conn = _dbConnectionFactory.Create())
    {
        conn.Open();
        using (var trans = _transactionFactory.Create(conn).BeginTransaction())
        {
            using (var commandSql = conn.CreateCommand())
            {
                commandSql.Transaction = trans;
                _prepareCommandHandler.Handle(command, commandSql, CommandStringTypes.CreateJobTables);
                commandSql.ExecuteNonQuery();
            }
            trans.Commit();
        }
    }
    return new QueueCreationResult(QueueCreationStatus.Success);
}
```

**Test cases needed:**
- Happy path: command executes, returns Success
- Verify `_dbConnectionFactory.Create()` called once
- Verify `conn.Open()` called
- Verify transaction begin/commit
- Verify `_prepareCommandHandler.Handle()` called with the right command and CommandStringType
- Verify `ExecuteNonQuery()` called

Constructor null guards (3 args) -- 3 ArgumentNullException tests.

## GetJobIdQueryHandler -- Code Analysis

```csharp
public T Handle(GetJobIdQuery<T> query)
{
    using (var connection = _dbConnectionFactory.Create())
    {
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            _prepareQuery.Handle(query, command, CommandStringTypes.GetJobId);
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return _readColumn.ReadAsType<T>(CommandStringTypes.GetJobId, 0, reader);
                }
            }
        }
    }
    return default;
}
```

**Test cases needed:**
- Reader has row -> returns `_readColumn.ReadAsType<T>` value
- Reader has no rows -> returns `default(T)`
- Verify connection lifecycle (Create, Open, Dispose)
- Verify prepareQuery.Handle called with correct args
- Constructor null guards (3 args)

## Existing Test Pattern (from DoesJobExistQueryHandlerTests)

The reference test in `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/DoesJobExistQueryHandlerTests.cs` uses:

- **MSTest 3.x** with `[TestClass]`/`[TestMethod]` attributes
- **NSubstitute** with `Substitute.For<>()` and `.Returns()` (no AutoFixture in this particular test)
- **Inline mocks**: `IDbConnection`, `IDbTransaction`, `IDbCommand`, `IDataReader`
- **Private `TestFixture` factory method** that creates the handler with mocked deps and returns a fixture object
- **Assertions**: `Assert.AreEqual` for return values, `.Received(1)` for call counts, `.DidNotReceive()` for negative assertions
- **No FluentAssertions** in this file (some tests use it)

## Recommended Test File Locations

Based on existing structure:

- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/CommandHandler/CreateJobTablesCommandHandlerTests.cs` (NEW folder `CommandHandler/`)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs` (existing folder)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobLastKnownEventQueryHandlerTests.cs` (existing folder)

Note: The existing `Basic/Command/` folder has tests for command DTOs (not handlers). Tests for command HANDLERS would go in a new `Basic/CommandHandler/` folder for clarity. The user's CONTEXT decision said to use a `JobScheduler/` subfolder, but that conflicts with the existing convention -- it's better to follow the existing folder structure.

## Recommended Plan Structure

Given the rescoped findings, the original 3-plan split needs adjustment:

- **Plan 2.1 (NEW handlers, command):** `CreateJobTablesCommandHandlerTests`
- **Plan 2.2 (NEW handlers, query):** `GetJobIdQueryHandlerTests` + `GetJobLastKnownEventQueryHandlerTests`
- **Plan 2.3 (EXPAND existing tests):** Add missing test cases to `GetDashboardJobsQueryHandlerAsyncTests`, `GetDashboardErrorRetriesQueryHandlerAsyncTests`, and their sync siblings

All three plans operate on different files -> Wave 1 parallel execution is safe.

---
phase: 2-coverage-job-scheduler-handlers
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - New test class CreateJobTablesCommandHandlerTests with happy-path coverage
  - Constructor null-guard tests for all 3 dependencies
  - Tests follow the NSubstitute inline-mock pattern used in DoesJobExistQueryHandlerTests
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/CommandHandler/CreateJobTablesCommandHandlerTests.cs
tdd: true
risk: low
---

# Plan 1.1 - CreateJobTablesCommandHandler Tests

## Context

`CreateJobTablesCommandHandler` currently has zero unit tests. It lives at
`Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandHandler/CreateJobTablesCommandHandler.cs`
and is a shared handler used by all relational transports (SqlServer, PostgreSQL, SQLite, LiteDb via the RelationalDatabase layer).

Constructor signature:

```csharp
public CreateJobTablesCommandHandler(
    IDbConnectionFactory dbConnectionFactory,
    IPrepareCommandHandler<CreateJobTablesCommand<ITable>> prepareCommandHandler,
    ITransactionFactory transactionFactory)
```

`Handle()` opens a connection, begins a transaction via `ITransactionFactory.Create(conn).BeginTransaction()`,
creates an `IDbCommand`, assigns the transaction, calls the prepare handler with `CommandStringTypes.CreateJobTables`,
calls `ExecuteNonQuery()`, commits, and returns `new QueueCreationResult(QueueCreationStatus.Success)`.

Reference pattern: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/DoesJobExistQueryHandlerTests.cs`.
That file uses `[TestClass]`/`[TestMethod]` MSTest attributes, `Substitute.For<T>()` for inline mocks, and a
private `CreateFixture()` method returning a `TestFixture` POCO. Follow that style exactly.

The folder `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/CommandHandler/` does not exist yet and must be created.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/CommandHandler/CreateJobTablesCommandHandlerTests.cs" tdd="true">
  <action>
Create the test file `CreateJobTablesCommandHandlerTests.cs` (creating the `Basic/CommandHandler/` folder if needed).
Use namespace `DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.CommandHandler`.
Include the LGPL-2.1 license header matching other test files in this project.

Add a `[TestClass]` with a private `CreateFixture()` helper that mocks:
- `IDbConnectionFactory` -> returns an `IDbConnection` mock
- `IPrepareCommandHandler<CreateJobTablesCommand<ITable>>`
- `ITransactionFactory` -> returns an `ITransactionWrapper` mock whose `BeginTransaction()` returns an `IDbTransaction` mock
- `IDbCommand` returned from `connection.CreateCommand()`

Write the following four happy-path `[TestMethod]` tests (use NSubstitute `Received(1)` / `DidNotReceive()` to assert call ordering and arguments):

1. `Handle_OpensConnection_AndReturnsSuccess` - verifies `connection.Open()` is called exactly once and result is `QueueCreationStatus.Success`.
2. `Handle_CallsPrepareCommandHandler_WithCreateJobTablesCommandType` - verifies `prepareCommandHandler.Handle(command, Arg.Any<IDbCommand>(), CommandStringTypes.CreateJobTables)` was received exactly once with the same command instance that was passed to `Handle()`.
3. `Handle_ExecutesNonQuery_OnCommand` - verifies `command.ExecuteNonQuery()` is called exactly once.
4. `Handle_CommitsTransaction` - verifies `transaction.Commit()` is called exactly once (on the `IDbTransaction` returned by `transactionWrapper.BeginTransaction()`) and the command's `Transaction` property is set to that transaction.

Use a real `CreateJobTablesCommand<ITable>` instance constructed with a `Substitute.For<ITable>()` (inspect the command constructor to determine required args) for the query argument to `Handle()`.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~CreateJobTablesCommandHandlerTests"</verify>
  <done>All 4 happy-path tests pass on net10.0 and net8.0. `dotnet build` emits no new warnings. Tests use only NSubstitute mocks (no integration DB access).</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/CommandHandler/CreateJobTablesCommandHandlerTests.cs" tdd="true">
  <action>
Add 3 constructor null-guard `[TestMethod]` tests to the same file:

1. `Constructor_NullDbConnectionFactory_Throws` - passes `null` for `dbConnectionFactory`, expects `ArgumentNullException` via `Assert.ThrowsException<ArgumentNullException>(...)` (or the NullReferenceException/ArgumentNullException that `Guard.NotNull` actually throws - check `DotNetWorkQueue.Validation.Guard` behavior first and match the real exception type).
2. `Constructor_NullPrepareCommandHandler_Throws` - passes `null` for `prepareCommandHandler`.
3. `Constructor_NullTransactionFactory_Throws` - passes `null` for `transactionFactory`.

Each test must construct the other two dependencies via `Substitute.For<T>()`. Do NOT use AutoFixture.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~CreateJobTablesCommandHandlerTests"</verify>
  <done>All 7 tests (4 happy-path + 3 null-guard) pass on net10.0 and net8.0. Each null-guard test asserts the specific parameter name when `Guard.NotNull` supplies it.</done>
</task>

## Verification

```bash
cd /mnt/f/git/dotnetworkqueue
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug
dotnet test  "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~CreateJobTablesCommandHandlerTests"
```

Expected: 7 tests passing, 0 failing, 0 skipped. No new build warnings.

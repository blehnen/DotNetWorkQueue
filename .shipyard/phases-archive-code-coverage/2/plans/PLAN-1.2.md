---
phase: 2-coverage-job-scheduler-handlers
plan: 1.2
wave: 1
dependencies: []
must_haves:
  - New test class GetJobIdQueryHandlerTests covering Handle + constructor guards
  - New test class GetJobLastKnownEventQueryHandlerTests covering Handle + constructor guards
  - Tests use NSubstitute inline mocks per DoesJobExistQueryHandlerTests pattern
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobLastKnownEventQueryHandlerTests.cs
tdd: true
risk: low
---

# Plan 1.2 - GetJobIdQueryHandler + GetJobLastKnownEventQueryHandler Tests

## Context

Two shared query handlers in `Transport.RelationalDatabase` have no unit tests:

1. `GetJobIdQueryHandler<T>` at `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetJobIdQueryHandler.cs`
   - Ctor deps: `IPrepareQueryHandler<GetJobIdQuery<T>, T>`, `IDbConnectionFactory`, `IReadColumn`
   - `Handle()` opens a connection, creates a command, calls prepare with `CommandStringTypes.GetJobId`, executes the reader, and returns `_readColumn.ReadAsType<T>(CommandStringTypes.GetJobId, 0, reader)` when `reader.Read()` is true, otherwise `default`.

2. `GetJobLastKnownEventQueryHandler` at `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetJobLastKnownEventQueryHandler.cs`
   - Ctor deps: `IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>`, `IDbConnectionFactory`, `IReadColumn`
   - `Handle()` opens a connection, creates a command, calls prepare with `CommandStringTypes.GetJobLastKnownEvent`, executes the reader, and returns `_readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastKnownEvent, 0, reader)` when `reader.Read()` is true, otherwise `default(DateTimeOffset)`.

Both use `Guard.NotNull` in the constructor for all three dependencies.

Folder `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/` already exists (other tests such as `GetDashboardJobsQueryHandlerTests.cs` live there).

Reference pattern: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/DoesJobExistQueryHandlerTests.cs` - MSTest `[TestClass]`, NSubstitute inline mocks, private `CreateFixture()` + `TestFixture` POCO.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs" tdd="true">
  <action>
Create `GetJobIdQueryHandlerTests.cs` with namespace `DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler`
and the LGPL-2.1 license header. Close the generic with `GetJobIdQueryHandler<long>` for the tests (long is a realistic job id type).

`CreateFixture()` should build real mocks for `IPrepareQueryHandler<GetJobIdQuery<long>, long>`, `IDbConnectionFactory`, `IReadColumn`,
plus `IDbConnection`, `IDbCommand`, `IDataReader` via `Substitute.For<T>()`. Wire `dbConnectionFactory.Create()` -> connection,
`connection.CreateCommand()` -> command, `command.ExecuteReader()` -> reader.

Add the following `[TestMethod]` tests:

1. `Handle_ReaderHasRow_ReturnsReadColumnValue`
   - `reader.Read().Returns(true, false)`
   - `readColumn.ReadAsType<long>(CommandStringTypes.GetJobId, 0, reader).Returns(42L)`
   - Invoke `handler.Handle(new GetJobIdQuery<long>("jobName"))` (inspect GetJobIdQuery constructor; use whatever args it actually requires)
   - Assert result == 42, `connection.Open()` received 1x, and `prepareQuery.Handle(query, command, CommandStringTypes.GetJobId)` received 1x.

2. `Handle_ReaderHasNoRows_ReturnsDefault`
   - `reader.Read().Returns(false)`
   - Assert result == `default(long)` (0).
   - Assert `readColumn.ReadAsType<long>` was `DidNotReceive()`.

3. `Constructor_NullPrepareQuery_Throws` - passes null for `prepareQuery`, expects the exception type that `Guard.NotNull` actually throws.
4. `Constructor_NullDbConnectionFactory_Throws` - passes null for `dbConnectionFactory`.
5. `Constructor_NullReadColumn_Throws` - passes null for `readColumn`.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~GetJobIdQueryHandlerTests"</verify>
  <done>5 tests pass on net10.0 and net8.0. No new build warnings.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobLastKnownEventQueryHandlerTests.cs" tdd="true">
  <action>
Create `GetJobLastKnownEventQueryHandlerTests.cs` mirroring the structure of Task 1, targeting `GetJobLastKnownEventQueryHandler`.

`CreateFixture()` mocks: `IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>`, `IDbConnectionFactory`, `IReadColumn`, `IDbConnection`, `IDbCommand`, `IDataReader`.

Add the following `[TestMethod]` tests:

1. `Handle_ReaderHasRow_ReturnsReadColumnValue`
   - `reader.Read().Returns(true, false)`
   - Arrange `readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastKnownEvent, 0, reader).Returns(expected)` where `expected = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)`.
   - Invoke `handler.Handle(new GetJobLastKnownEventQuery("jobName"))` (inspect GetJobLastKnownEventQuery constructor at `DotNetWorkQueue.Transport.Shared.Basic.Query.GetJobLastKnownEventQuery` for exact args).
   - Assert result == expected, `connection.Open()` 1x, `prepareQuery.Handle(query, command, CommandStringTypes.GetJobLastKnownEvent)` 1x.

2. `Handle_ReaderHasNoRows_ReturnsDefaultDateTimeOffset`
   - `reader.Read().Returns(false)`
   - Assert result == `default(DateTimeOffset)`.
   - Assert `readColumn.ReadAsDateTimeOffset` was `DidNotReceive()`.

3. `Constructor_NullPrepareQuery_Throws`
4. `Constructor_NullDbConnectionFactory_Throws`
5. `Constructor_NullReadColumn_Throws`
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~GetJobLastKnownEventQueryHandlerTests"</verify>
  <done>5 tests pass on net10.0 and net8.0. No new build warnings.</done>
</task>

## Verification

```bash
cd /mnt/f/git/dotnetworkqueue
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug
dotnet test  "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~GetJobIdQueryHandlerTests|FullyQualifiedName~GetJobLastKnownEventQueryHandlerTests"
```

Expected: 10 tests passing, 0 failing, 0 skipped.

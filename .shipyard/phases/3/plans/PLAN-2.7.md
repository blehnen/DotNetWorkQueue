---
phase: phase-3-coverage
plan: 2.7
wave: 2
dependencies: [1.1]
must_haves:
  - New unit test file SetJobLastKnownEventCommandHandlerTests.cs added under Tests/Basic/CommandHandler
  - Tests cover constructor null guards including new IDbConnectionFactory parameter
  - Happy path test verifies SQL parameters set correctly
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs
tdd: false
risk: medium
---

# Plan 2.7 — SqlServer SetJobLastKnownEvent unit tests

## Context

Depends on Plan 1.1 having refactored the handler to inject `IDbConnectionFactory`. The handler is now testable: stub `IDbConnectionFactory.Create()` to return a mocked `IDbConnection`, which produces a mocked `IDbCommand`, which captures parameter assignments and `CommandText`.

The Phase 2 test helper `AdoNetMockFixture` lives in `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/TestHelpers/`. The SqlServer.Tests project does NOT currently reference RelationalDatabase.Tests as a project — adding such a cross-project reference is risky and may produce circular `<ProjectReference>` issues. The simplest path is to instantiate the ADO.NET mocks inline in this test class using NSubstitute (same approach used by SQLite's existing `SetJobLastKnownEventCommandHandlerTests.cs` — read it for a template).

Reference template: `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs`.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs` to use as a structural template.
  2. Read the refactored `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` to learn the constructor signature, the SQL it executes, and the parameter names it sets.
  3. Create `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs` with:
     - Null-guard tests for each `ArgumentNullException`-guarded constructor parameter, including `IDbConnectionFactory`.
     - `Handle_NullCommand_Throws` — `Assert.ThrowsExactly<ArgumentNullException>` when invoking `Handle(null)`.
     - `Handle_HappyPath_OpensConnectionAndExecutes` — set up `IDbConnectionFactory.Create()` to return an `IDbConnection` mock; the connection should yield an `IDbCommand` mock; assert `Open()` was called and `ExecuteNonQuery()` (or `ExecuteScalar`, whichever the source uses) was invoked exactly once.
     - `Handle_HappyPath_SetsExpectedParameters` — capture the `IDataParameterCollection.Add` calls and assert the parameter names/values match the command properties (job name, event time, scheduled time — check the source for exact field names).
     - Optionally: assert `CommandText` matches the SQL the prepare/statement step produces (per the CLAUDE.md lesson about silent UPDATE no-ops).
  4. Use NSubstitute throughout. Do NOT add a project reference to RelationalDatabase.Tests.
  5. Run the filtered test command.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" --filter "FullyQualifiedName~SetJobLastKnownEventCommandHandlerTests"</verify>
  <done>All new tests in `SetJobLastKnownEventCommandHandlerTests` (under SqlServer.Tests) pass on net10.0 and net8.0. Includes null guards, happy path execution, and parameter assertions. Test count is at least 4.</done>
</task>

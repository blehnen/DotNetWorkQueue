---
phase: phase-3-coverage
plan: 2.8
wave: 2
dependencies: [1.2]
must_haves:
  - New unit test file SetJobLastKnownEventCommandHandlerTests.cs added under Tests/Basic/CommandHandler
  - Tests cover constructor null guards including new IDbConnectionFactory parameter
  - Happy path test verifies SQL parameters set correctly
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs
tdd: false
risk: medium
---

# Plan 2.8 — PostgreSQL SetJobLastKnownEvent unit tests

## Context

Mirror of Plan 2.7 for PostgreSQL. Depends on Plan 1.2 having refactored the handler to inject `IDbConnectionFactory`.

Use the same approach: instantiate ADO.NET mocks inline with NSubstitute (do not add a project reference to RelationalDatabase.Tests). Reference template: `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs`.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs` for the template.
  2. Read the refactored `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` to learn the constructor signature, SQL text, and parameter names.
  3. Create `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs` with:
     - Null-guard tests for each `ArgumentNullException`-guarded constructor parameter, including `IDbConnectionFactory`.
     - `Handle_NullCommand_Throws`.
     - `Handle_HappyPath_OpensConnectionAndExecutes`.
     - `Handle_HappyPath_SetsExpectedParameters` — capture parameter Add calls; assert the names/values match.
     - Optional: assert `CommandText` matches.
  4. Use NSubstitute. Do NOT add a project reference to RelationalDatabase.Tests.
  5. Run filtered tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~SetJobLastKnownEventCommandHandlerTests"</verify>
  <done>All new tests in `SetJobLastKnownEventCommandHandlerTests` (under PostgreSQL.Tests) pass on net10.0 and net8.0. Includes null guards, happy path execution, and parameter assertions. Test count is at least 4.</done>
</task>

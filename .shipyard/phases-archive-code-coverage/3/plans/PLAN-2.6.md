---
phase: phase-3-coverage
plan: 2.6
wave: 2
dependencies: []
must_haves:
  - New unit test file SqliteSendToJobQueueTests.cs added under Tests/Basic
  - Tests cover constructor null guards
  - Tests cover the SQLite-specific overrides DoesJobExist and DeleteJob
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteSendToJobQueueTests.cs
tdd: false
risk: low
---

# Plan 2.6 — SQLite SendToJobQueue unit tests

## Context

Mirror of Plans 2.4 / 2.5 for SQLite. Source: `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteSendToJobQueue.cs`. Same `ASendJobToQueue` pattern expected; the generic connection/transaction parameters will be SQLite-specific (`SqliteConnection`/`SqliteTransaction` from `Microsoft.Data.Sqlite`, or whatever the source uses — confirm by reading).

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteSendToJobQueueTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteSendToJobQueue.cs` in full to capture the exact constructor signature and overrides.
  2. Read an existing SQLite test file for conventions.
  3. Create `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteSendToJobQueueTests.cs` with:
     - Constructor null-guard tests for each `ArgumentNullException`-guarded parameter.
     - `DoesJobExist_DelegatesToQueryHandler` — verifies the injected DoesJobExist query handler is invoked and result propagated.
     - `DeleteJob_RemovesJobById` — verifies the `IRemoveMessage` and `GetJobId` query handler are invoked correctly.
  4. Use NSubstitute for all interface dependencies.
  5. Run filtered tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SqliteSendToJobQueueTests"</verify>
  <done>All new tests in `SqliteSendToJobQueueTests` pass on net10.0 and net8.0. At least one null-guard test plus DoesJobExist + DeleteJob tests are present.</done>
</task>

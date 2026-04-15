---
phase: phase-3-coverage
plan: 2.5
wave: 2
dependencies: []
must_haves:
  - New unit test file PostgreSqlSendJobToQueueTests.cs added under Tests/Basic
  - Tests cover constructor null guards
  - Tests cover the PostgreSQL-specific overrides DoesJobExist and DeleteJob
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlSendJobToQueueTests.cs
tdd: false
risk: low
---

# Plan 2.5 — PostgreSQL SendJobToQueue unit tests

## Context

Mirror of Plan 2.4 for PostgreSQL. Source: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLSendJobToQueue.cs`. Same `ASendJobToQueue` inheritance pattern is expected, but the connection / transaction generic parameters will be `NpgsqlConnection`/`NpgsqlTransaction`. Builder MUST read the source first to confirm the exact constructor signature; do not assume.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlSendJobToQueueTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLSendJobToQueue.cs` in full to capture the exact constructor signature and overrides.
  2. Read an existing PostgreSQL test file for conventions.
  3. Create `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlSendJobToQueueTests.cs` with:
     - Constructor null-guard tests for each `ArgumentNullException`-guarded parameter.
     - `DoesJobExist_DelegatesToQueryHandler` — uses NSubstitute to verify the injected `IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses>` is invoked and its result is returned.
     - `DeleteJob_RemovesJobById` — verifies the `IRemoveMessage` and `IQueryHandler<GetJobIdQuery<long>, long>` collaborators are invoked correctly.
  4. Use NSubstitute for all interface dependencies.
  5. Run filtered tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~PostgreSqlSendJobToQueueTests"</verify>
  <done>All new tests in `PostgreSqlSendJobToQueueTests` pass on net10.0 and net8.0. At least one null-guard test plus DoesJobExist + DeleteJob tests are present.</done>
</task>

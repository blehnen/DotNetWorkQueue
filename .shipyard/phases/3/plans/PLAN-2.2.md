---
phase: phase-3-coverage
plan: 2.2
wave: 2
dependencies: []
must_haves:
  - New unit test file PostgreSqlJobSchemaTests.cs added under Tests/Basic
  - Tests cover constructor null guards (where present in source)
  - Tests cover GetSchema() returning expected job table structure
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlJobSchemaTests.cs
tdd: false
risk: low
---

# Plan 2.2 — PostgreSQL JobSchema unit tests

## Context

Mirror of Plan 2.1 for PostgreSQL. Source file: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLJobSchema.cs`. The class is expected to follow the same pattern as `SqlServerJobSchema` (constructor accepts `TableNameHelper` plus an `ISqlSchema` collaborator; `GetSchema()` returns `List<ITable>`), but the actual constructor signature MUST be confirmed by reading the source first — do not assume.

Reference existing tests in `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlJobQueueCreationTests.cs` for project conventions and `TableNameHelper` setup.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlJobSchemaTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLJobSchema.cs` in full. Note exact namespace, constructor parameters, and what `GetSchema()` returns (table name, columns, indexes).
  2. Read `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlJobQueueCreationTests.cs` for MSTest/NSubstitute conventions used in this test project.
  3. Create `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlJobSchemaTests.cs` with:
     - Constructor null-guard tests (one per `ArgumentNullException`-guarded parameter — verify by reading source first).
     - `GetSchema_ReturnsJobTable` — assert returned `List<ITable>` is non-empty and the job table name matches `TableNameHelper`'s job table name.
     - `GetSchema_JobTable_HasExpectedColumns` — assert each column the source defines is present.
     - Any additional assertions warranted by the source (e.g. unique constraints, primary key).
  4. Use NSubstitute mocks where the constructor accepts interfaces; use real `TableNameHelper` constructed from a `QueueConnection`.
  5. Run the filtered test command to confirm.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~PostgreSqlJobSchemaTests"</verify>
  <done>All new tests in `PostgreSqlJobSchemaTests` pass on net10.0 and net8.0. Test count is at least 3.</done>
</task>

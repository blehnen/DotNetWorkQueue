---
phase: phase-3-coverage
plan: 2.3
wave: 2
dependencies: []
must_haves:
  - New unit test file SqliteJobSchemaTests.cs added under Tests/Basic
  - Tests cover constructor null guards (where present in source)
  - Tests cover GetSchema() returning expected job table structure
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteJobSchemaTests.cs
tdd: false
risk: low
---

# Plan 2.3 — SQLite JobSchema unit tests

## Context

Mirror of Plans 2.1 / 2.2 for SQLite. Source: `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteJobSchema.cs`. Builder MUST read the source before authoring tests since SQLite schemas typically omit features SQL Server / PostgreSQL include (no separate sequence handling, no schema namespacing). Reference the existing LiteDb test file `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbJobSchemaTests.cs` as a structural template — but produce SQLite-specific assertions.

Reference `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteJobQueueCreationTests.cs` for project conventions.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteJobSchemaTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteJobSchema.cs` to learn the constructor signature, namespace, and what `GetSchema()` produces.
  2. Read `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbJobSchemaTests.cs` for a template.
  3. Read `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteJobQueueCreationTests.cs` for SQLite-specific test setup conventions.
  4. Create `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteJobSchemaTests.cs` with constructor null-guard tests (only for parameters actually guarded), a `GetSchema_ReturnsJobTable` test, and a `GetSchema_JobTable_HasExpectedColumns` test.
  5. Use NSubstitute for any interface dependencies; use a real `TableNameHelper`.
  6. Run the filtered tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SqliteJobSchemaTests"</verify>
  <done>All new tests in `SqliteJobSchemaTests` pass on net10.0 and net8.0. Test count is at least 3.</done>
</task>

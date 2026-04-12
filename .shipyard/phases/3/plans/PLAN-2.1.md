---
phase: phase-3-coverage
plan: 2.1
wave: 2
dependencies: []
must_haves:
  - New unit test file SqlServerJobSchemaTests.cs added under Tests/Basic
  - Tests cover constructor null guards
  - Tests cover GetSchema() returning the expected job table structure
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerJobSchemaTests.cs
tdd: false
risk: low
---

# Plan 2.1 — SqlServer JobSchema unit tests

## Context

`SqlServerJobSchema` (Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerJobSchema.cs) is a pure schema-definition class. Constructor: `(TableNameHelper tableNameHelper, ISqlSchema schema)`. `GetSchema()` returns `List<ITable>` describing the job table — its name, columns, primary key, and indexes. No DB access; safely unit-testable with NSubstitute mocks plus a real `TableNameHelper` (which is concrete but trivially constructible from a `QueueConnection`/`IConnectionInformation`).

Reference existing tests in `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerJobQueueCreationTests.cs` for namespace, base imports, and `TableNameHelper` instantiation patterns.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerJobSchemaTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerJobSchema.cs` to learn its exact constructor signature, the namespace, and what `GetSchema()` produces (job table name format, columns).
  2. Read an existing SqlServer test file (e.g. `SqlServerJobQueueCreationTests.cs`) to copy the project's MSTest 3.x conventions: `[TestClass]`, `[TestMethod]`, `using` imports, and how `TableNameHelper` is constructed.
  3. Create `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerJobSchemaTests.cs` with at least:
     - `Constructor_NullTableNameHelper_Throws` — `Assert.ThrowsExactly<ArgumentNullException>` (only if the source actually null-guards it; otherwise omit).
     - `Constructor_NullSchema_Throws` — same pattern (only if guarded).
     - `GetSchema_ReturnsSingleJobTable` — assert the returned list has at least one table and its name matches the expected job table name from `TableNameHelper`.
     - `GetSchema_JobTable_HasExpectedColumns` — assert the table contains the columns enumerated in the source (e.g. JobName, ScheduledTime, JobEventTime). Use FluentAssertions.
     - `GetSchema_JobTable_HasPrimaryKey` — assert the primary key exists if the source defines one.
  4. Use NSubstitute (`Substitute.For<ISqlSchema>()`) for the schema interface; instantiate `TableNameHelper` with a real `QueueConnection` (or whatever the constructor needs — check existing tests).
  5. Run the test project to confirm.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" --filter "FullyQualifiedName~SqlServerJobSchemaTests"</verify>
  <done>All new tests in `SqlServerJobSchemaTests` pass on net10.0 and net8.0. Test count is at least 3.</done>
</task>

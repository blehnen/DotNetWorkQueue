---
phase: phase-4-litedb-redis-job-handlers
plan: "2.3"
wave: 2
dependencies: []
must_haves:
  - New test file for LiteDb GetJobIdQueryHandler
  - Covers primary lookup paths (found, not-found)
  - Uses real in-memory LiteDatabase
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs
tdd: true
risk: low
---

# Plan 2.3 — GetJobIdQueryHandler Tests (LiteDb-specific)

## Context

The LiteDb `GetJobIdQueryHandler` at `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryHandler/GetJobIdQueryHandler.cs` is distinct from the relational-database version — do NOT assume shared implementation. The builder must READ the source file first to determine the exact query shape, return type (`int`), and collection access pattern (likely via `LiteDbConnectionManager.GetDatabase()` or the query's own `Database` property).

## Dependencies

None — independent of Wave 1.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryHandler/GetJobIdQueryHandler.cs`. Record: constructor dependencies, whether the query carries a `LiteDatabase` directly or whether the handler pulls one from a connection manager, the exact collection/document shape read, and what field identifies a job (likely `JobName`).
  2. READ an existing LiteDb QueryHandler test (e.g., `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/DoesJobExistQueryHandlerTests.cs`) to mirror setup conventions for in-memory LiteDatabase and TableNameHelper.
  3. CREATE `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs` with the LGPL-2.1 header.
  4. Add a `[TestClass]` with at least:
     - `Handle_ReturnsId_WhenJobExists` — pre-seed the jobs collection with a row that has a known id and job name; call Handle; assert the returned id matches.
     - `Handle_ReturnsZero_WhenJobDoesNotExist` (or whatever the "not found" sentinel is per the source — if it throws instead, assert the exception type).
     - `Handle_UsesConfiguredJobTableName` — verify the correct collection name is queried.
     - `Constructor_NullTableNameHelper_Throws` — if the production code uses `Guard.NotNull`.
  5. Use `using var db = new LiteDatabase(new MemoryStream());` (or `":memory:"` fallback).
  6. Use `Assert.ThrowsExactly<ArgumentNullException>` for null-guard tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~GetJobIdQueryHandlerTests"</verify>
  <done>New test file exists. All tests pass on net10.0 and net8.0. Both the found and not-found branches of the LiteDb `GetJobIdQueryHandler` are covered.</done>
</task>

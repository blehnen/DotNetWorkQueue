---
phase: phase-4-litedb-redis-job-handlers
plan: "2.1"
wave: 2
dependencies: []
must_haves:
  - New test file for SetJobLastKnownEventCommandHandler
  - Covers insert-new-job path and update-existing-job path
  - Uses real in-memory LiteDatabase (not mocked)
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs
tdd: true
risk: low
---

# Plan 2.1 — SetJobLastKnownEventCommandHandler Tests (LiteDb)

## Context

`SetJobLastKnownEventCommandHandler` in `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` has no tests. It accepts a `SetJobLastKnownEventCommand` that carries a `LiteDatabase` instance, fetches `JobsTable` collection via `TableNameHelper.JobTableName`, and either inserts a new record or updates an existing one.

`LiteDatabase` is sealed — use a real in-memory instance (`new LiteDatabase(new MemoryStream())` or `new LiteDatabase(":memory:")`). Builder must verify which syntax works for net10.0 and net8.0.

`TableNameHelper` is a concrete class — check `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/DoesJobExistQueryHandlerTests.cs` for the existing construction pattern (typically built from a `LiteDbConnectionInformation` or similar with a fake container name).

## Dependencies

None — independent of Wave 1.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` to understand the Handle signature, the `JobsTable` schema used, and any null-guard behavior on the constructor.
  2. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/SetJobLastKnownEventCommand.cs` (or equivalent command file) to learn the command constructor parameters.
  3. READ an existing LiteDb test such as `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/DoesJobExistQueryHandlerTests.cs` to learn the conventional way to build `TableNameHelper` and construct in-memory `LiteDatabase` instances.
  4. CREATE `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs` with the LGPL-2.1 license header (copy from any existing file in the same project).
  5. Add a `[TestClass]` with at least these `[TestMethod]`s:
     - `Handle_InsertsNewRecord_WhenJobDoesNotExist` — create empty in-memory DB, invoke handle with a unique job name, assert the `JobsTable` collection now has one row with expected JobName, JobEventTime, JobScheduledTime.
     - `Handle_UpdatesExistingRecord_WhenJobAlreadyExists` — pre-insert a `JobsTable` row for the job name, call Handle with different timestamps, assert the row was updated (not duplicated).
     - `Handle_UsesTableNameHelperJobTableName` — verify the collection is accessed by the configured `JobTableName` (e.g., pre-seed into a differently-named collection and confirm it is NOT read).
     - `Constructor_NullTableNameHelper_Throws` — only if the production code has a null guard (check first with the Read in step 1).
  6. Use `Assert.ThrowsExactly<ArgumentNullException>` for null-guard tests. Use FluentAssertions (`Should()`) for state assertions if the project already uses it; otherwise plain MSTest `Assert`.
  7. Use `using var db = new LiteDatabase(new MemoryStream());` — fall back to `":memory:"` if the MemoryStream form fails to compile or run on either TFM.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~SetJobLastKnownEventCommandHandlerTests"</verify>
  <done>New test file exists. All new tests pass on both net10.0 and net8.0. No other test files are modified. Handler coverage for `SetJobLastKnownEventCommandHandler` reaches both the insert and the update branches.</done>
</task>

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" -c Debug
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~SetJobLastKnownEventCommandHandlerTests"
```

All new tests pass. Full project test run still green.

---
phase: phase-4-litedb-redis-job-handlers
plan: "2.5"
wave: 2
dependencies: []
must_haves:
  - DashboardUpdateMessageBodyCommandHandler coverage lifted from ~40.9% to above 70%
  - New tests added to existing test file (do not delete existing tests)
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs
tdd: true
risk: low
---

# Plan 2.5 — Expand DashboardUpdateMessageBodyCommandHandler Tests (LiteDb)

## Context

`Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs` already exists but only covers roughly 40.9% of `DashboardUpdateMessageBodyCommandHandler` in `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandler.cs`. The builder must READ both files, identify uncovered branches, and add tests until coverage exceeds 70%.

Likely uncovered branches: null/empty body, missing message id, non-existent row update path, serialization edge cases, TableNameHelper usage.

## Dependencies

None — independent of Wave 1.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandler.cs` and enumerate every distinct code path in `Handle`.
  2. READ the existing test file `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs`. List which branches are already covered and which are not.
  3. For each uncovered branch, add a new `[TestMethod]` to the existing `[TestClass]`. Do NOT remove or alter any existing test. Follow the existing test style (naming, arrangement, assertion library) for consistency.
  4. Typical branches to cover if not already:
     - Body with valid payload updates the row
     - Handle with a message id that does not exist in the collection (should no-op or throw — match source)
     - Null/empty body handling
     - `TableNameHelper` drives the collection name (pre-seed wrong collection, assert correct one is read)
     - Any null-guard on the constructor not already asserted (use `Assert.ThrowsExactly<ArgumentNullException>`)
  5. Reuse the in-memory LiteDatabase pattern already present in the existing test file.
  6. Run the project's coverage target (or collect via `--collect "XPlat Code Coverage"`) and verify the handler now exceeds 70% line coverage. If the repo has no scripted way to read per-class coverage, assert branch coverage qualitatively by reviewing the added tests against the enumerated branches from step 1.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~DashboardUpdateMessageBodyCommandHandlerTests"</verify>
  <done>Existing tests still pass. New tests pass on net10.0 and net8.0. Every branch enumerated in step 1 has at least one test exercising it. The existing test file was only appended to, not restructured.</done>
</task>

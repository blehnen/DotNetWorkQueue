---
phase: phase-4-litedb-redis-job-handlers
plan: "2.4"
wave: 2
dependencies: []
must_haves:
  - New test file for LiteDb RollbackMessageCommandHandler
  - Covers each branch of the rollback logic
  - Uses real in-memory LiteDatabase
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/RollbackMessageCommandHandlerTests.cs
tdd: true
risk: medium
---

# Plan 2.4 — RollbackMessageCommandHandler Tests (LiteDb)

## Context

The LiteDb `RollbackMessageCommandHandler` at `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/RollbackMessageCommandHandler.cs` is untested. Rollback typically has multiple branches — increase-attempts, move-back-to-ready-status, optional error-tracking update, and may depend on queue configuration options (heartbeat, retries, error table enabled). The builder must READ the source first to enumerate branches accurately.

## Dependencies

None — independent of Wave 1.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/RollbackMessageCommandHandlerTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/RollbackMessageCommandHandler.cs`. Enumerate every branch: option checks (e.g., `EnableStatus`, `EnableDelayedProcessing`, `EnableMessageExpiration`, heartbeat etc.), state changes, and collection updates. Record the constructor dependencies.
  2. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/RollbackMessageCommand.cs` (or equivalent) for the command shape — specifically what `LiteDatabase`/connection handle is carried.
  3. READ an existing LiteDb command-handler test (e.g., `DashboardUpdateMessageBodyCommandHandlerTests.cs`) for the standard setup pattern.
  4. CREATE `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/RollbackMessageCommandHandlerTests.cs` with the LGPL-2.1 header.
  5. Add a `[TestClass]` with `[TestMethod]`s covering each enumerated branch. At minimum:
     - `Handle_ResetsStatus_WhenStatusTrackingEnabled`
     - `Handle_NoOp_WhenStatusTrackingDisabled` (or equivalent — matching the source's actual option flags)
     - `Handle_IncrementsHeartbeat_WhenHeartbeatConfigured` (if applicable)
     - `Handle_RestoresReadyQueue_WhenDelayedProcessingEnabled` (if applicable)
     - Null-guard constructor tests using `Assert.ThrowsExactly<ArgumentNullException>` for any `Guard.NotNull` parameters.
  6. Use `using var db = new LiteDatabase(new MemoryStream());` and pre-seed the necessary collections for each test. Substitute any option/configuration dependencies via NSubstitute.
  7. Assert on the post-Handle collection state: query the in-memory DB and verify the row fields changed exactly as expected.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~RollbackMessageCommandHandlerTests"</verify>
  <done>New test file exists. All branches identified from the source are covered. All new tests pass on net10.0 and net8.0.</done>
</task>

## Notes on Partial Completion

If one branch turns out to require hitting a sealed LiteDB API that cannot be exercised from in-memory state, skip that single branch, add a `// TODO` with plan reference, and keep all other branches covered. Report partial status.

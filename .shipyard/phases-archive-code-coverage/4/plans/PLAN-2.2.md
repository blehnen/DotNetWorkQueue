---
phase: phase-4-litedb-redis-job-handlers
plan: "2.2"
wave: 2
dependencies: []
must_haves:
  - New test file for LiteDbSendJobToQueue
  - Covers DoesJobExist and DeleteJob override paths
  - Internal class access resolved (InternalsVisibleTo or reflection)
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbSendJobToQueueTests.cs
  - Source/DotNetWorkQueue.Transport.LiteDB/Properties/AssemblyInfo.cs (only if InternalsVisibleTo must be added)
tdd: true
risk: medium
---

# Plan 2.2 — LiteDbSendJobToQueue Tests

## Context

`LiteDbSendJobToQueue` (`Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbSendJobToQueue.cs`) is an `internal` class that inherits `ASendJobToQueue`. Constructor takes 7 dependencies:

- `LiteDbConnectionManager connectionInformation` (concrete)
- `IProducerMethodQueue queue`
- `IQueryHandler<DoesJobExistQuery, QueueStatuses> doesJobExist`
- `IRemoveMessage removeMessage`
- `IQueryHandler<GetJobIdQuery<int>, int> getJobId`
- `CreateJobMetaData createJobMetaData` (concrete)
- `IGetTimeFactory getTimeFactory`

Most are mockable via NSubstitute. `LiteDbConnectionManager` is concrete — for the `DoesJobExist()` override test path, an in-memory `LiteDatabase` may be required; for `DeleteJob()` (which delegates to `IRemoveMessage`) no DB is needed.

The Phase 3 SqlServer equivalent test (`SqlServerSendJobToQueueTests`) is the reference pattern — READ it first to mirror the structure.

## Dependencies

None — independent of Wave 1.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbSendJobToQueueTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbSendJobToQueue.cs` to confirm the exact constructor signature, class accessibility (`internal` vs `public`), the `DoesJobExist` override, and the `DeleteJob` override.
  2. READ `Source/DotNetWorkQueue.Transport.LiteDB/Properties/AssemblyInfo.cs` (or `.csproj`) and confirm whether `InternalsVisibleTo("DotNetWorkQueue.Transport.LiteDb.Tests")` is already present. If absent, add it — either via `[assembly: InternalsVisibleTo("DotNetWorkQueue.Transport.LiteDb.Tests")]` in AssemblyInfo.cs, or an `<ItemGroup><InternalsVisibleTo Include="DotNetWorkQueue.Transport.LiteDb.Tests" /></ItemGroup>` in the .csproj — match the pattern already used elsewhere in the repo (e.g., check the SqlServer transport for the convention).
  3. READ the Phase 3 reference test `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerSendJobToQueueTests.cs` (or equivalent) to mirror layout, mocking approach, and test names.
  4. CREATE `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbSendJobToQueueTests.cs` with LGPL-2.1 header.
  5. Add a `[TestClass]` with at least:
     - `DoesJobExist_DelegatesToQueryHandler` — substitute `IQueryHandler<DoesJobExistQuery, QueueStatuses>` to return `QueueStatuses.Processing`; invoke the override (via public wrapper on `ASendJobToQueue` if one exists, or via reflection of the protected method); assert the handler was called once with the expected query and the return value bubbled up.
     - `DeleteJob_DelegatesToRemoveMessage` — substitute `IRemoveMessage`; invoke `DeleteJob` override; verify `Remove` was called with the job id.
     - `Constructor_NullConnectionInformation_Throws` and one or two other null-guard tests if `Guard.NotNull` is present in the production constructor.
  6. For `LiteDbConnectionManager`, if it can be instantiated cheaply with a throwaway connection string (e.g., `Filename=:memory:`), construct a real one; otherwise use `Substitute.For<LiteDbConnectionManager>(...)` only if the class is virtual-friendly. If neither works, accept a partial test that only covers the handler-delegation paths that do not touch `connectionInformation`, and leave a `// TODO` comment citing this plan.
  7. Use `Assert.ThrowsExactly<ArgumentNullException>` for null-guard tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~LiteDbSendJobToQueueTests"</verify>
  <done>Test file exists. All listed tests pass on net10.0 and net8.0. `LiteDbSendJobToQueue` internal class is accessible from the test project (either already, or via the InternalsVisibleTo change). No existing test files are modified except AssemblyInfo.cs / .csproj if the InternalsVisibleTo add was needed.</done>
</task>

## Notes on Partial Completion

If `LiteDbConnectionManager` cannot be instantiated or substituted in a unit-test context, complete only the `DeleteJob` and null-guard tests, mark the `DoesJobExist` case as skipped with a clear message, and include a one-paragraph note at the top of the test file explaining the limitation. Report the partial status to the orchestrator rather than forcing reflection-heavy workarounds.

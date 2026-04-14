---
phase: phase-3-coverage
plan: 2.4
wave: 2
dependencies: []
must_haves:
  - New unit test file SqlServerSendJobToQueueTests.cs added under Tests/Basic
  - Tests cover constructor null guards
  - Tests cover the SqlServer-specific overrides DoesJobExist and DeleteJob
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerSendJobToQueueTests.cs
tdd: false
risk: low
---

# Plan 2.4 — SqlServer SendJobToQueue unit tests

## Context

`SqlServerSendJobToQueue` (Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerSendJobToQueue.cs) inherits `ASendJobToQueue`. Constructor has 6 dependencies, all interfaces or simple types:
- `IProducerMethodQueue queue`
- `IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses> doesJobExist`
- `IRemoveMessage removeMessage`
- `IQueryHandler<GetJobIdQuery<long>, long> getJobId`
- `CreateJobMetaData createJobMetaData` (concrete — confirm type by reading source)
- `IGetTimeFactory getTimeFactory`

Builder MUST read the full source file (it is short — ~120 lines) before writing tests. Tests focus on the SqlServer-specific overrides — `DoesJobExist()` and `DeleteJob()` — which delegate to the injected query handlers. Use NSubstitute for all interfaces; if `CreateJobMetaData` is concrete and trivially constructible, instantiate it directly, otherwise mock if it's an interface.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerSendJobToQueueTests.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerSendJobToQueue.cs` in full. Read `Source/DotNetWorkQueue/JobScheduler/ASendJobToQueue.cs` (or wherever it lives — grep for `class ASendJobToQueue`) to understand the abstract members the SqlServer subclass overrides.
  2. Read an existing SqlServer test file for project conventions.
  3. Create `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerSendJobToQueueTests.cs` with:
     - One null-guard test per `ArgumentNullException`-throwing constructor parameter (use `Assert.ThrowsExactly<ArgumentNullException>`).
     - `DoesJobExist_DelegatesToQueryHandler` — set up the `IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses>` mock to return a known `QueueStatuses` value, invoke `DoesJobExist`, assert the value is propagated and the handler was called once with the expected query.
     - `DeleteJob_CallsRemoveMessage` — set up `IQueryHandler<GetJobIdQuery<long>, long>` to return a job id and `IRemoveMessage` to capture the call; invoke `DeleteJob`; assert `RemoveMessage` was called with the expected message id.
     - Optionally a happy-path test for any other observable override.
  4. Use NSubstitute for all six dependencies (instantiating `CreateJobMetaData` as concrete if practical — read the source for its constructor).
  5. Run the filtered tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" --filter "FullyQualifiedName~SqlServerSendJobToQueueTests"</verify>
  <done>All new tests in `SqlServerSendJobToQueueTests` pass on net10.0 and net8.0. At least one null-guard test plus one DoesJobExist test plus one DeleteJob test are present.</done>
</task>

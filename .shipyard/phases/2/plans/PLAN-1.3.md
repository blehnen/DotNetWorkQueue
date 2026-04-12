---
phase: 2-coverage-job-scheduler-handlers
plan: 1.3
wave: 1
dependencies: []
must_haves:
  - GetDashboardJobs sync + async test coverage raised above 70%
  - GetDashboardErrorRetries sync + async test coverage raised above 70%
  - New test methods follow the NSubstitute / MSTest pattern already used in those files
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerTests.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerAsyncTests.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerAsyncTests.cs
tdd: true
risk: medium
---

# Plan 1.3 - Expand Dashboard Job/Error Handler Tests

## Context

The four dashboard query handlers under `Transport.RelationalDatabase` currently sit at ~30-35% coverage. Existing tests exercise only a subset of branches (primarily happy-path reader returning rows). We need to lift coverage above 70% for each handler.

Handler sources (read these in Task 1 to identify branches):
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetDashboardJobsQueryHandler.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetDashboardJobsQueryHandlerAsync.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandler.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerAsync.cs`

Existing test files (expand, do not replace):
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerAsyncTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerAsyncTests.cs`

Style reference: `DoesJobExistQueryHandlerTests` (MSTest + NSubstitute inline mocks + private `CreateFixture()`).

Use Coverlet (already wired through `dotnet test --collect:"XPlat Code Coverage"`) to verify the per-class coverage threshold.

## Tasks

<task id="1" files="" tdd="false">
  <action>
Gap analysis (no file changes -- this is a research task whose output feeds Tasks 2 and 3).

Read the four existing test files and the four handler source files listed in Context. For each handler, identify:

- Branches that are currently exercised by existing tests
- Branches that are NOT exercised (no row / multiple rows / null-guard ctor / exception propagation / empty input)
- Dependencies that will need to be mocked in new tests

Capture the gap analysis in your build summary (SUMMARY-1.3.md) -- do NOT modify the test files. Tasks 2 and 3 will use the analysis directly when adding new test methods.

Note: The async handler variants (`GetDashboardJobsQueryHandlerAsync`, `GetDashboardErrorRetriesQueryHandlerAsync`) take NO `CancellationToken` parameter -- do not add cancellation tests for them. Their `HandleAsync` signature is `HandleAsync(GetDashboardJobsQuery query)` style.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug</verify>
  <done>Gap analysis captured in SUMMARY-1.3.md (or builder notes) covering at least 3 untested branches per handler. No existing files modified.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerTests.cs,Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerAsyncTests.cs" tdd="true">
  <action>
Add new `[TestMethod]` tests to the two `GetDashboardJobs*` test files to close the gaps enumerated in Task 1. At minimum each file must add:

- A reader-returns-no-rows test asserting an empty result collection
- A reader-returns-multiple-rows test asserting N items and that the `IReadColumn` helpers were invoked per row
- Constructor null-guard tests for every ctor parameter that doesn't already have one
- (Async file only) A task-completion test that awaits `HandleAsync(query)` and asserts the awaited result matches the mocked reader output (the async handler does NOT take a `CancellationToken`)

Reuse the existing `CreateFixture()` helper if present; if it's missing, add one that mirrors `DoesJobExistQueryHandlerTests.CreateFixture()`. Do NOT remove or rename existing tests.

After adding tests, run coverage locally to confirm both `GetDashboardJobsQueryHandler` and `GetDashboardJobsQueryHandlerAsync` line coverage is above 70%.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~GetDashboardJobs" --collect:"XPlat Code Coverage"</verify>
  <done>All tests in both `GetDashboardJobsQueryHandlerTests` and `GetDashboardJobsQueryHandlerAsyncTests` pass. Coverlet report (under `TestResults/*/coverage.cobertura.xml`) shows line coverage > 70% for both `GetDashboardJobsQueryHandler` and `GetDashboardJobsQueryHandlerAsync`. No pre-existing tests were broken.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs,Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerAsyncTests.cs" tdd="true">
  <action>
Mirror Task 2 for the two `GetDashboardErrorRetries*` files. Add tests that close the gaps identified in Task 1:

- Reader-returns-no-rows -> empty collection
- Reader-returns-multiple-rows -> assert all rows projected
- Constructor null-guards for each ctor parameter
- Async variant: awaited result matches mocked reader output

Reuse existing `CreateFixture()` if present, otherwise add one. Do NOT remove existing tests. Target > 70% line coverage for both `GetDashboardErrorRetriesQueryHandler` and `GetDashboardErrorRetriesQueryHandlerAsync`.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~GetDashboardErrorRetries" --collect:"XPlat Code Coverage"</verify>
  <done>All tests in both `GetDashboardErrorRetriesQueryHandlerTests` and `GetDashboardErrorRetriesQueryHandlerAsyncTests` pass. Coverlet report shows line coverage > 70% for both handlers. No pre-existing tests were broken.</done>
</task>

## Verification

```bash
cd /mnt/f/git/dotnetworkqueue
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug
dotnet test  "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" \
  --filter "FullyQualifiedName~GetDashboardJobs|FullyQualifiedName~GetDashboardErrorRetries" \
  --collect:"XPlat Code Coverage"
```

Expected: all tests pass, 0 skipped. Coverlet cobertura report shows `GetDashboardJobsQueryHandler`, `GetDashboardJobsQueryHandlerAsync`, `GetDashboardErrorRetriesQueryHandler`, and `GetDashboardErrorRetriesQueryHandlerAsync` each above 70% line coverage.

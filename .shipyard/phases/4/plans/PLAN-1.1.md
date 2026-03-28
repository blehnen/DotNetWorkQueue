---
phase: stale-project-cleanup
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - IntegrationTests.Metrics project is fully removed from solution and disk
  - All metric tracking behavior preserved for integration tests
  - Full solution builds without errors
  - All in-memory integration tests pass
files_touched:
  - Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Metrics.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Counter.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Meter.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/MetricsContext.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Histogram.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Timer.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/TimerContext.cs
  - Source/DotNetWorkQueue/InternalsVisibleForTests.cs
  - Source/DotNetWorkQueue.sln
  - Source/DotNetWorkQueue.IntegrationTests.Metrics/ (deleted)
tdd: false
---

# Plan 1.1: Remove IntegrationTests.Metrics Project

## Context

`DotNetWorkQueue.IntegrationTests.Metrics` is a standalone project that provides `IMetrics` implementations
with real counter/meter tracking for integration test assertions. It is used in ~30 files across
`IntegrationTests.Shared`. The core `MetricsNoOp` cannot replace it because it discards all values.

**Strategy**: Move the 7 source files into `IntegrationTests.Shared/Metrics/` (preserving the
`DotNetWorkQueue.IntegrationTests.Metrics` namespace), remove the ProjectReference, remove the project
from the solution, remove the InternalsVisibleTo entry, and delete the project directory.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Metrics.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Counter.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Meter.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/MetricsContext.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Histogram.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Timer.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/TimerContext.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" tdd="false">
  <action>
    1. Create directory `Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/`.
    2. Copy all 7 .cs files from `Source/DotNetWorkQueue.IntegrationTests.Metrics/` into it:
       - Counter.cs, Histogram.cs, Meter.cs, Metrics.cs, MetricsContext.cs, Timer.cs, TimerContext.cs
    3. The files keep their existing namespace `DotNetWorkQueue.IntegrationTests.Metrics` unchanged -- this ensures all ~30 consumer files that reference `Metrics.Metrics` and `new Metrics.Metrics(...)` continue to resolve without any code changes.
    4. Remove the ProjectReference line from `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj` (line 37):
       `<ProjectReference Include="..\DotNetWorkQueue.IntegrationTests.Metrics\DotNetWorkQueue.IntegrationTests.Metrics.csproj" />`
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug</verify>
  <done>IntegrationTests.Shared builds successfully with the moved files. No compilation errors. The 7 .cs files exist under `Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/`. The csproj no longer references IntegrationTests.Metrics.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.sln, Source/DotNetWorkQueue/InternalsVisibleForTests.cs" tdd="false">
  <action>
    1. Remove the InternalsVisibleTo entry from `Source/DotNetWorkQueue/InternalsVisibleForTests.cs` -- delete line 25:
       `[assembly: InternalsVisibleTo("DotNetWorkQueue.IntegrationTests.Metrics")]`
    2. Remove the project from `Source/DotNetWorkQueue.sln`:
       - Delete the Project block at lines 16-17:
         `Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "DotNetWorkQueue.IntegrationTests.Metrics", ...`
         `EndProject`
       - Delete all 12 build configuration lines for GUID `{B7974956-3764-4B0C-B6F2-0B8F8A25BEFE}` (lines 150-161)
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Debug</verify>
  <done>Core project builds. The .sln file no longer contains any reference to `B7974956-3764-4B0C-B6F2-0B8F8A25BEFE` or `IntegrationTests.Metrics`. InternalsVisibleForTests.cs has exactly 4 entries (Tests, SqlServer.Tests, SqlServer.Integration.Tests, Redis.Tests).</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.IntegrationTests.Metrics/ (deleted)" tdd="false">
  <action>
    1. Delete the entire directory `Source/DotNetWorkQueue.IntegrationTests.Metrics/` (including bin/obj).
    2. Build the full solution to confirm nothing is broken.
    3. Run in-memory integration tests to confirm metric verification still works.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.sln" -c Debug && dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --no-build -c Debug</verify>
  <done>The directory `Source/DotNetWorkQueue.IntegrationTests.Metrics/` no longer exists. Full solution builds with zero errors. In-memory integration tests pass, confirming that `VerifyMetrics` still correctly reads counter/meter values from the moved `Metrics` class.</done>
</task>

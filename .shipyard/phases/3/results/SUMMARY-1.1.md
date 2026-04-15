# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed
- **Task 1 (CPM + csproj):** complete — commit `362c39f4`
  - Files: `Source/Directory.Packages.props`, `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj`
  - Added `<PackageVersion Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.4.0" />` to `Directory.Packages.props`.
  - Created the new csproj with `<TargetFrameworks>net10.0</TargetFrameworks>`, bare PackageReferences (AutoFixture, FluentAssertions, MSTest, NSubstitute, coverlet, Microsoft.NET.Test.Sdk, and `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`), and three ProjectReferences (DotNetWorkQueue, Transport.Memory, IntegrationTests.Shared).
  - `dotnet restore` resolved 0.4.0 from nuget.org clean.
- **Task 2 (SLN wiring):** complete — commit `7c62372d`
  - File: `Source/DotNetWorkQueue.sln`
  - Added `Project(...)` line + full `GlobalSection(ProjectConfigurationPlatforms)` block with a fresh GUID, mirroring `DotNetWorkQueue.Transport.Memory.Integration.Tests`. `DotNetWorkQueueNoTests.sln` intentionally untouched.
- **Task 3 (AssemblyInit.cs + TestHelpers.cs):** complete — commit `93506699`
  - Files: `AssemblyInit.cs`, `TestHelpers.cs` — both without inline LGPL header (Option A, matches Memory test convention).
  - `AssemblyInit.cs` has `[assembly: DoNotParallelize]` outside the namespace.
  - `TestHelpers.cs` has the platform-aware `BeaconInterface`, disjoint port-base constants (`EndToEndPortBase=50000`, `ConcurrencyPortBase=55000`, `NodeDiscoveryPortBase=60000`), and `NextPort(ref int counter)` using `Interlocked.Increment`.

## Files Modified
- `Source/Directory.Packages.props` — added PackageVersion entry for `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0.
- `Source/DotNetWorkQueue.sln` — added Project entry + ProjectConfigurationPlatforms block for the new test project.
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj` — created.
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/AssemblyInit.cs` — created.
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/TestHelpers.cs` — created.

## Decisions Made
- **TargetFrameworks changed from `net10.0;net8.0` to `net10.0`** during build (user decision, Option B). The plan / CONTEXT-3 / ROADMAP all said `net10.0;net8.0`, but `DotNetWorkQueue.Transport.Memory.Integration.Tests` (the project the plan told the builder to mirror) is `net10.0`-only — the CONTEXT-3 claim that `net10.0;net8.0` "matches the rest of DNQ's test projects" was factually wrong. Jenkins CI runs `net10.0` on ubuntu-latest per CLAUDE.md; multi-targeting test projects produces output Jenkins never exercises. PLAN-1.1, PLAN-2.1, PLAN-2.2, PLAN-2.3, PLAN-3.1, CONTEXT-3, ROADMAP (lines 192/198/231), and VERIFICATION.md were all edited to reflect the corrected single-target spec.

## Issues Encountered
- **Plan/reality mismatch on target frameworks:** the plan text and acceptance criteria required multi-targeting but the mirror project does not multi-target. Resolved by user (Option B) — update spec to match reality. The builder's choice to produce a single-target net10.0 csproj was actually correct against the existing pattern; the paper spec was the defect.
- **Builder agent truncated mid-response** without writing `SUMMARY-1.1.md`. All three git commits landed cleanly. The main driver wrote this summary after verifying the build (`dotnet build …Integration.Tests.csproj -c Debug` → `Build succeeded. 0 Warning(s). 0 Error(s)`).

## Verification Results
- `dotnet build Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj -c Debug` → **Build succeeded, 0 warnings, 0 errors** (net10.0 output at `bin/Debug/net10.0/`).
- `dotnet sln Source/DotNetWorkQueue.sln list` contains the new project.
- `grep -c "TaskScheduling.Distributed.TaskScheduler" Source/DotNetWorkQueueNoTests.sln` → **0** (NoTests.sln not touched).
- `Source/Directory.Packages.props` contains `<PackageVersion Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.4.0" />`.
- Acceptance criteria after spec correction:
  - ✅ New Integration.Tests project created and added to `DotNetWorkQueue.sln`
  - ✅ `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 pinned via CPM
  - ✅ `AssemblyInit.cs` with `[assembly: DoNotParallelize]` in place
  - ✅ Shared `TestHelpers.cs` in place (Wave-2 plans will consume read-only)
  - ✅ `dotnet restore` resolves 0.4.0 from nuget.org
  - ✅ `dotnet build Source/DotNetWorkQueue.sln -c Debug` clean (validated by csproj-level build; full solution build deferred to PLAN-3.1 Task 2)

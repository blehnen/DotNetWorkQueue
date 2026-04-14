---
phase: phase-3
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - New Integration.Tests project created and added to DotNetWorkQueue.sln
  - DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0 pinned via Central Package Management
  - AssemblyInit.cs with [assembly: DoNotParallelize] in place
  - Shared TestHelpers.cs (BeaconInterface, NextPort counter pattern) so Wave-2 test classes touch disjoint files
  - `dotnet restore` resolves 0.4.0 from nuget.org
  - `dotnet build Source/DotNetWorkQueue.sln -c Debug` clean
files_touched:
  - Source/Directory.Packages.props
  - Source/DotNetWorkQueue.sln
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/AssemblyInit.cs
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/TestHelpers.cs
tdd: false
risk: medium
---

# PLAN-1.1: Scaffold Integration.Tests project + shared helpers

## Context

Phase 3 creates a new integration test project that consumes the just-shipped
`DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 NuGet. This plan
lays the foundation: project file, CPM entry, SLN wiring, `AssemblyInit.cs` with
`[assembly: DoNotParallelize]`, and a shared `TestHelpers.cs` file that the three
Wave-2 test-class plans will consume **read-only** (no modifications). Putting
all shared scaffolding into Wave 1 means Wave-2 plans (PLAN-2.1 / 2.2 / 2.3) can
run strictly in parallel without touching shared files.

**Locked decisions** (see `.shipyard/phases/3/CONTEXT-3.md`):

- Project path: `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/`
- Target frameworks: `net10.0;net8.0`
- CPM: add to `Source/Directory.Packages.props`, bare `<PackageReference>` in csproj
- MSTest 4.1.0, NSubstitute 5.3.0, AutoFixture 4.18.1, FluentAssertions 6.12.2 (do NOT upgrade)
- `[assembly: DoNotParallelize]` for cross-test NetMQ port serialization
- Port seeds: `EndToEnd=50000`, `Concurrency=55000`, `NodeDiscovery=60000`
- Beacon interface: `"loopback"` on Windows, `""` on Linux (`RuntimeInformation.IsOSPlatform(OSPlatform.Linux)`)
- License header: match DNQ Memory integration test convention — **no inline LGPL header** on test files (RESEARCH.md section 2, Option A)
- SLN: add to `Source/DotNetWorkQueue.sln` only, NOT `DotNetWorkQueueNoTests.sln`

<task id="1" files="Source/Directory.Packages.props, Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" tdd="false">
  <action>
1. Add a new `<PackageVersion Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.4.0" />` entry to `Source/Directory.Packages.props` inside the existing `<ItemGroup>` that contains the other PackageVersion elements (alongside the Core or Test-Infrastructure group — use Core group, after line 13 `CronExpressionDescriptor` is a reasonable slot; group it with a `<!-- TaskScheduling -->` comment for clarity).

2. Create the project directory `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/` and create the csproj file inside it. Mirror `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj` with the following differences:
   - `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>` (multi-target per ROADMAP line 198)
   - Include the same test-framework PackageReferences (bare, no Version): `AutoFixture`, `AutoFixture.AutoNSubstitute`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, `MSTest.TestFramework`, `MSTest.TestAdapter`, `NSubstitute`
   - Add `<PackageReference Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" />` (bare, version inherited from CPM)
   - Keep ProjectReferences to: `..\DotNetWorkQueue\DotNetWorkQueue.csproj`, `..\DotNetWorkQueue.Transport.Memory\DotNetWorkQueue.Transport.Memory.csproj`, `..\DotNetWorkQueue.IntegrationTests.Shared\DotNetWorkQueue.IntegrationTests.Shared.csproj`
   - Do NOT set `TreatWarningsAsErrors` (inherited from Directory.Build.props per RESEARCH.md section 1)
   - Do NOT reference the sibling TaskScheduler repo's project — NuGet PackageReference ONLY (CONTEXT-3.md §5 "Release-Hard Constraints")

3. Run restore to prove 0.4.0 resolves from nuget.org.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet restore "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" 2>&1 | tee /tmp/p3-restore.log && grep -E "DotNetWorkQueue\.TaskScheduling\.Distributed\.TaskScheduler.*0\.4\.0" /tmp/p3-restore.log || echo "Restore completed; check log for 0.4.0 resolution"</verify>
  <done>Directory.Packages.props contains the 0.4.0 PackageVersion entry. The new csproj exists with TargetFrameworks=net10.0;net8.0, CPM-style PackageReferences (no Version attributes), and the three ProjectReferences. `dotnet restore` completes with exit code 0 and `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 is resolved from nuget.org (no errors like NU1101/NU1102).</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.sln" tdd="false">
  <action>
Add the new project to `Source/DotNetWorkQueue.sln`. Mirror the exact pattern used for `DotNetWorkQueue.Transport.Memory.Integration.Tests` (RESEARCH.md section 6):

1. Generate a fresh project GUID (use `uuidgen` or `powershell -c "[guid]::NewGuid().ToString().ToUpper()"`) — the GUID must be unique and NOT collide with any existing GUID in the sln. Let `$NEW_GUID` be the new GUID.

2. Add a `Project(...) = ...` line near the existing `Memory.Integration.Tests` project entry (around line 18):
```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests", "DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests\DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj", "{$NEW_GUID}"
EndProject
```

3. Add the full 12-line `GlobalSection(ProjectConfigurationPlatforms)` block for `$NEW_GUID` matching the Memory.Integration.Tests pattern (Debug/Release × Any CPU/x64/x86, all mapped to `Any CPU`). See RESEARCH.md section 6 for the exact shape.

4. Do NOT add to `DotNetWorkQueueNoTests.sln` (CONTEXT-3.md §4 "Out-of-Scope").

5. Verify: the solution restores and builds clean (Debug).
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet sln "Source/DotNetWorkQueue.sln" list | grep -i "TaskScheduling.Distributed.TaskScheduler.Integration.Tests" && dotnet build "Source/DotNetWorkQueue.sln" -c Debug --nologo 2>&1 | tail -20</verify>
  <done>`dotnet sln list` shows the new project. `dotnet build Source/DotNetWorkQueue.sln -c Debug` exits 0 with `Build succeeded` and 0 errors. `grep -c Memory.Integration.Tests Source/DotNetWorkQueueNoTests.sln` returns 0 (confirming we did NOT touch NoTests.sln).</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/AssemblyInit.cs, Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/TestHelpers.cs" tdd="false">
  <action>
Create two files (both WITHOUT inline LGPL header per RESEARCH.md section 2, Option A — matches Memory test project convention):

**File 1: `AssemblyInit.cs`** — clone `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/AssemblyInit.cs`, change the namespace to `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests`, and add `[assembly: DoNotParallelize]` OUTSIDE the namespace block (RESEARCH.md section 4). Exact content:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;

[assembly: DoNotParallelize]

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    [TestClass]
    public static class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MsTestHelper.ClearSynchronizationContext();
        }
    }
}
```

**File 2: `TestHelpers.cs`** — shared constants + per-class port counter pattern. The Wave-2 test class plans (PLAN-2.1/2/3) will CONSUME this file read-only; they will NOT modify it, so parallel-safe. Exact content:

```csharp
using System.Runtime.InteropServices;
using System.Threading;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    /// <summary>
    /// Shared constants and helpers for Phase 3 integration tests.
    /// Port seeds are disjoint per test class to avoid TIME_WAIT collisions
    /// even though [assembly: DoNotParallelize] already serializes execution.
    /// </summary>
    internal static class TestHelpers
    {
        /// <summary>
        /// Beacon interface argument for <c>InjectDistributedTaskScheduler(port, beaconInterface)</c>.
        /// On Linux, the default "loopback" does NOT deliver UDP broadcasts back to sibling
        /// processes — use "" instead to bind to the first available interface.
        /// On Windows, "loopback" is correct.
        /// </summary>
        public static readonly string BeaconInterface =
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? string.Empty : "loopback";

        // Disjoint per-test-class port seeds (CONTEXT-3.md §5). Each test class owns
        // its own counter instance via a static field so ports never overlap.
        public const int EndToEndPortBase = 50000;
        public const int ConcurrencyPortBase = 55000;
        public const int NodeDiscoveryPortBase = 60000;

        /// <summary>
        /// Allocates the next TIME_WAIT-safe port from the given base. Thread-safe;
        /// callers hold a static counter per test class.
        /// </summary>
        public static int NextPort(ref int counter)
        {
            return Interlocked.Increment(ref counter);
        }
    }
}
```

After creating both files, build the project to prove everything compiles clean.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" -c Debug --nologo 2>&1 | tail -15</verify>
  <done>Both files exist. `dotnet build` on the csproj exits 0, `Build succeeded`, 0 errors. `grep -c "DoNotParallelize" Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/AssemblyInit.cs` returns >= 1. The project produces both `net8.0` and `net10.0` output DLLs under `bin/Debug/`. Running `dotnet test` on the project succeeds (0 tests discovered is OK at this stage — plan 1.1 adds no test methods).</done>
</task>

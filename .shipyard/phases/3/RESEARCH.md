# Phase 3 Research — DNQ Integration Test Project for TaskScheduling.Distributed.TaskScheduler 0.4.0

## Context

Phase 3 creates a new MSTest 3.x integration test project inside the DNQ repo that consumes
the 0.4.0 NuGet package `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (published
to nuget.org 2026-04-14). The project must follow DNQ's existing Memory integration test
layout/conventions exactly so the Jenkins/GitHub Actions unit+in-memory test lanes pick it up
without external-service dependencies. This doc captures everything the architect and builder
need to stand up that project.

---

## 1. Test-project template (Memory integration tests)

**File:** `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj`

Quoted verbatim:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net10.0</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AutoFixture" />
    <PackageReference Include="AutoFixture.AutoNSubstitute" />
<PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="MSTest.TestFramework" />
    <PackageReference Include="MSTest.TestAdapter" />
    <PackageReference Include="NSubstitute" />
</ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DotNetWorkQueue.IntegrationTests.Shared\DotNetWorkQueue.IntegrationTests.Shared.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.Memory\DotNetWorkQueue.Transport.Memory.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue\DotNetWorkQueue.csproj" />
  </ItemGroup>
</Project>
```

### Key facts for the builder

- **TargetFrameworks:** `net10.0` only (not multi-targeted). The upstream TaskScheduler package
  supports net8 and net10, so targeting net10 aligns with the rest of DNQ's CI matrix.
- **Central Package Management is ON.** `PackageReference` elements carry NO `Version`
  attributes — versions live in `Source/Directory.Packages.props`. Confirmed versions:
  - `FluentAssertions` — **6.12.2** (the pinned last-MIT release; do NOT bump)
  - `MSTest.TestFramework` — `4.1.0`
  - `MSTest.TestAdapter` — `4.1.0`
  - `NSubstitute` — `5.3.0`
  - `AutoFixture` / `AutoFixture.AutoNSubstitute` — `4.18.1`
- **TreatWarningsAsErrors:** NOT set in this csproj. It is inherited from `Source/Directory.Build.props`
  in Release builds. The builder does not need to add it.
- **No `connectionstring.txt`** needed — Memory transport has no external dependency, and the
  TaskScheduler package is in-process (UDP beacon on loopback). The new Phase 3 project is the
  same shape in that regard.
- **The new project will need an extra `PackageReference Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler"`**
  with a Version attribute (since it is NOT in `Directory.Packages.props`), OR the version must be
  added to `Directory.Packages.props` first. **Decision point for the architect:** pin the version
  in CPM or inline it on the PackageReference. Pinning in CPM is more consistent with the rest of
  the repo.
- **ProjectReferences:** only the three DNQ projects above are referenced. The Phase 3 project
  will likely need the same three plus any additional DNQ transport needed for a full
  producer/consumer cycle (Memory is sufficient for scheduler-bus tests).

---

## 2. License header

DNQ source files do not carry an inline LGPL header in integration test files (`SimpleProducer.cs`,
`SimpleConsumer.cs`, `AssemblyInit.cs` all start directly with `using` statements — the license
header is enforced by `DotNetWorkQueue.licenseheader` in the ReSharper/Rider tool chain, not
stamped into every test file). The upstream TaskScheduler package DOES have the LGPL header on
its own code. For the new Phase 3 test files, two valid options:

### Option A — match Memory integration test convention (no inline header)

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.IntegrationTests.Shared;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests
// ... rest of file
```

This is what `AssemblyInit.cs`, `SimpleProducer.cs`, and `SimpleConsumer.cs` use.

### Option B — add the standard DNQ LGPL header (quoted from `TaskSchedulerSetup.cs` in the sibling repo)

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017-2020 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
```

**Recommendation for the architect:** match the Memory integration test convention (Option A).
Adding headers only to Phase 3 files would be inconsistent with the sibling Memory test project.

---

## 3. Memory transport integration test pattern

**File:** `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Consumer/SimpleConsumer.cs`
(full producer/consumer cycle with job counting)

```csharp
using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Consumer
{
    [TestClass]
    public class SimpleConsumer
    {
        [TestMethod]
        [DataRow(1000, 0, 240, 5),
        DataRow(50, 5, 200, 10),
        DataRow(10, 15, 180, 7)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(
                    new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3,
            ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, arg5, true);
        }
    }
}
```

### Pattern observations

- The DNQ integration test shared project (`DotNetWorkQueue.IntegrationTests.Shared`) does ALL the
  heavy lifting. The transport-specific test file is a thin shell that:
  1. Creates an `IntegrationConnectionInfo` (handles connection string lifecycle)
  2. Generates a queue name via `GenerateQueueName.Create()`
  3. Instantiates a shared implementation (e.g. `Shared.Consumer.Implementation.SimpleConsumer`)
  4. Calls `.Run<TQueueInit, TMessage, TQueueCreation>(...)` generic over the transport init class
  5. Passes a `VerifyQueueCount` callback that the shared runner invokes post-processing
- `MemoryMessageQueueInit` and `MessageQueueCreation` are the Memory transport's init classes
  (from `DotNetWorkQueue.Transport.Memory.Basic`).
- `FakeMessage` is defined in `DotNetWorkQueue.IntegrationTests.Shared`.
- `Helpers.GenerateData` and `Helpers.Verify` are defined in the local project's `SharedClasses.cs`.
- **`AssemblyInit.cs` (quoted verbatim):**

  ```csharp
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using DotNetWorkQueue.IntegrationTests.Shared;

  namespace DotNetWorkQueue.Transport.Memory.Integration.Tests
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

  Every DNQ test project needs this exact `AssemblyInit` (to prevent MSTest synchronization
  context from serializing async test execution). The Phase 3 project MUST include an equivalent.

### Metrics polling pattern

**The `IMetrics` polling pattern referenced in CLAUDE.md lessons lives inside
`DotNetWorkQueue.IntegrationTests.Shared` — the transport test files themselves never
touch metrics directly.** The shared runner's `.Run<...>()` method owns the polling loop that
waits for the metric counter to reach `messageCount` before taking a final snapshot. This is a
HARD REQUIREMENT inherited from the lessons-learned doc:

> Integration test metrics assertions can race: the handler callback signals completion before
> `CommitMessage.Commit()` increments the counter. Poll the live `IMetrics` object instead of
> taking a single snapshot.

**Implication for Phase 3:** If the scheduler test needs to assert on job counts, it should ride
on top of `Shared.Consumer.Implementation.SimpleConsumer` (or equivalent) rather than reimplement
the producer/consumer loop. This keeps the metrics polling free, correct, and proven.

Alternatively, for scheduler-focused tests that just need to verify `InjectDistributedTaskScheduler`
registered the `ATaskScheduler` correctly, a minimal integration path may be:

1. Create the Memory queue consumer inside a `SchedulerContainer(RegisterService)` where
   `RegisterService` calls `container.InjectDistributedTaskScheduler(...)`.
2. Enqueue N jobs via the Memory producer.
3. Use the existing shared consumer pattern to wait for completion and assert via the metrics
   polling path.
4. Optionally assert that the registered `ATaskScheduler` is `TaskSchedulerMultiple` (reflection
   or `container.GetInstance<ATaskScheduler>()`).

---

## 4. MSTest 3.x `[DoNotParallelize]` / `[Parallelize]` syntax

**Grep result:** NO hits for `DoNotParallelize`, `Parallelize(Workers`, or `assembly: Parallelize`
anywhere in `Source/`. DNQ does not currently use assembly-level parallelization control.

### MSTest 3.x / 4.x docs-verified syntax

For assembly-level control, place this in any `.cs` file (commonly `AssemblyInit.cs`) OUTSIDE any
namespace/type:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Disable test parallelization for the entire assembly:
[assembly: DoNotParallelize]

// OR enable parallelized workers at the method level with a configurable worker count:
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]
```

For per-test-class opt-out (while the rest of the assembly runs in parallel):

```csharp
[TestClass]
[DoNotParallelize]
public class SchedulerBusTests
{
    // ...
}
```

### Recommendation for Phase 3

**The scheduler test project SHOULD apply `[assembly: DoNotParallelize]`.** Rationale:

- `TaskSchedulerMultiple` uses a **fixed UDP beacon port** (default 9999). Two tests running in
  parallel in the same process with the same port will cause port-bind conflicts or cross-talk
  between otherwise-isolated test cases.
- Scheduler-bus state is process-wide (singleton), so parallel tests would observe each other's
  worker counts and broadcast noise.
- CLAUDE.md notes DNQ test projects have NOT migrated to `EnableMSTestRunner`/`Microsoft.Testing.Platform`
  (PR #89 was reverted). Classic `vstest` MSTest 3.x semantics apply.
- **Alternative:** each test could pick a unique UDP port (e.g. random high-range port via
  `new Random().Next(50000, 60000)`), but that fights the convention documented in the
  TaskScheduler README and introduces its own flakiness on WSL/Linux where loopback is finicky.
  `[assembly: DoNotParallelize]` is simpler and 100% correct.

**Exact attribute placement (to be added to the Phase 3 `AssemblyInit.cs`):**

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

---

## 5. `InjectDistributedTaskScheduler` public API

**Source:** sibling repo `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerSetup.cs`
(full file quoted verbatim):

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017-2020 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//...
// ---------------------------------------------------------------------
namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler
{
    /// <summary>
    /// Registers <see cref="TaskSchedulerMultiple"/> with <see cref="DotNetWorkQueue"/>
    /// </summary>
    public static class TaskSchedulerSetup
    {
        /// <summary>
        /// Injects the distributed task scheduler as <see cref="ATaskScheduler"/>; this will replace the built in scheduler
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="broadCastPort">The broad cast port to use.</param>
        /// <param name="beaconInterface">
        /// The interface the UDP beacon should bind to. Defaults to <c>"loopback"</c>. See
        /// <see cref="TaskSchedulerMultipleConfiguration(int, string)"/> for valid values. On Linux,
        /// <c>"loopback"</c> does not deliver broadcasts back to sibling processes — use <c>""</c> to
        /// bind to the first available interface instead.
        /// </param>
        /// <remarks>Each scheduler that shares a port will attempt to limit threads by the shared pool</remarks>
        public static void InjectDistributedTaskScheduler(this IContainer container, int broadCastPort = 9999, string beaconInterface = "loopback")
        {
            container.Register<ITaskSchedulerBus, TaskSchedulerBus>(LifeStyles.Singleton);
            container.Register<ITaskSchedulerJobCountSync, TaskSchedulerJobCountSync>(LifeStyles.Singleton);
            container.Register<ATaskScheduler, TaskSchedulerMultiple>(LifeStyles.Singleton);

            var configuration = new TaskSchedulerMultipleConfiguration(broadCastPort, beaconInterface);
            container.Register(() => configuration, LifeStyles.Singleton);
        }
    }
}
```

### Exact signature

```csharp
public static void InjectDistributedTaskScheduler(
    this DotNetWorkQueue.IContainer container,
    int broadCastPort = 9999,
    string beaconInterface = "loopback")
```

- **Extends:** `DotNetWorkQueue.IContainer` — the SimpleInjector abstraction used across DNQ.
- **Return type:** `void` (not fluent).
- **Namespace:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
- **Class:** `TaskSchedulerSetup` (static).
- **Registers:** `ITaskSchedulerBus → TaskSchedulerBus`, `ITaskSchedulerJobCountSync → TaskSchedulerJobCountSync`,
  `ATaskScheduler → TaskSchedulerMultiple` (all Singleton), and a `TaskSchedulerMultipleConfiguration`
  instance.

### README usage example (quoted)

```csharp
using DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler;

// Inside your DotNetWorkQueue consumer setup, after creating the container
// but before calling Start(), replace the default scheduler:
//
//   Every process that should share the thread-count ceiling must pass
//   the same UDP broadcast port. Processes using different ports form
//   independent, uncoordinated pools.
container.InjectDistributedTaskScheduler(udpBroadcastPort: 9999);
```

A fuller example using `SchedulerContainer`:

```csharp
using (var schedulerContainer = new SchedulerContainer(RegisterService))
{
    // ... create queue consumers, start, etc.
}

private static void RegisterService(IContainer container)
{
    container.InjectDistributedTaskScheduler(9999);
}
```

> **Note / minor bug flag:** The README comment uses a named argument `udpBroadcastPort: 9999`,
> but the actual parameter is `broadCastPort` (not `udpBroadcastPort`). The README example as
> written will NOT compile. The builder must use `broadCastPort:` if they want to copy the
> named-argument style, OR use positional `container.InjectDistributedTaskScheduler(9999)`.
> **Decision point:** flag this upstream for a README fix, but Phase 3 is NOT the place to fix it.

### Linux/WSL consideration

Per README and per the XML-doc on the parameter:

> On Linux the default beacon interface value `"loopback"` does not loop UDP broadcast back to
> the sending host. Pass an empty string instead: `container.InjectDistributedTaskScheduler(9999, "")`

**Implication for Phase 3 tests:** Jenkins runs Linux containers (per CLAUDE.md). If a test
starts two schedulers in the same process and expects them to discover each other, it MUST pass
`beaconInterface: ""`, not rely on the default `"loopback"`. Tests that only verify DI
registration (no peer discovery needed) can use defaults.

---

## 6. SLN wiring

**File:** `Source/DotNetWorkQueue.sln`

### Project entry (line 18)

```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "DotNetWorkQueue.Transport.Memory.Integration.Tests", "DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj", "{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}"
EndProject
```

- Project type GUID `{9A19103F-16F7-4668-BE54-9A1E7A4F7556}` is the standard SDK-style .NET project type.
- The per-project GUID `{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}` is unique per project — the Phase 3 project needs a freshly generated GUID.

### GlobalSection ProjectConfigurationPlatforms entries (lines 162–173)

```
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Debug|Any CPU.Build.0 = Debug|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Debug|x64.ActiveCfg = Debug|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Debug|x64.Build.0 = Debug|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Debug|x86.ActiveCfg = Debug|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Debug|x86.Build.0 = Debug|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Release|Any CPU.ActiveCfg = Release|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Release|Any CPU.Build.0 = Release|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Release|x64.ActiveCfg = Release|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Release|x64.Build.0 = Release|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Release|x86.ActiveCfg = Release|Any CPU
{F42D92E4-3CA3-4B9E-ACB2-CCAFE4648017}.Release|x86.Build.0 = Release|Any CPU
```

The Phase 3 project needs the same 12-line block with its own GUID. No solution-folder
`NestedProjects` entry exists for the Memory integration test project (it lives at the top level).

### `DotNetWorkQueueNoTests.sln` confirmation

`grep Memory.Integration.Tests` on `Source/DotNetWorkQueueNoTests.sln` returned **0 matches** —
confirming the no-tests SLN does NOT list Memory integration tests, and the Phase 3 project should
also be excluded from `DotNetWorkQueueNoTests.sln` (test projects never go there; it exists for
NuGet release builds).

---

## Blockers or notes for the architect

1. **Package version pinning decision.** The new project needs `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
   version `0.4.0`. Given DNQ uses Central Package Management (CPM), the cleanest path is to add
   a `<PackageVersion Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.4.0" />`
   entry to `Source/Directory.Packages.props` and then a bare `<PackageReference Include="...">` in
   the csproj. The architect should confirm this vs. inlining the version.

2. **Project name / folder naming.** The convention in DNQ is `DotNetWorkQueue.Transport.<X>.Integration.Tests`.
   Phase 3 is NOT a transport — it is a task-scheduler module. Recommended name:
   `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests` (mirrors the upstream
   package name, signals clearly it is not a transport). Architect should confirm.

3. **`[assembly: DoNotParallelize]` is strongly recommended** — without it, UDP port 9999 contention
   and shared scheduler-bus singletons will cause flaky/broken tests. The alternative (random
   per-test ports) is brittle on Linux/WSL and contradicts the README.

4. **README `udpBroadcastPort` parameter name bug in the upstream repo.** The README shows
   `container.InjectDistributedTaskScheduler(udpBroadcastPort: 9999)`, but the actual parameter is
   `broadCastPort`. Phase 3 tests must use `broadCastPort:` or positional arguments. Flag for a
   README fix upstream, but out-of-scope for Phase 3.

5. **Linux beacon interface default.** `"loopback"` does not work on Linux for multi-process peer
   discovery. If Phase 3 tests spin up multiple scheduler instances and expect them to see each
   other, tests MUST pass `beaconInterface: ""`. Single-scheduler DI-registration tests are
   unaffected.

6. **Metrics polling is inherited from `DotNetWorkQueue.IntegrationTests.Shared`**, not
   re-implemented per test file. Phase 3 tests should ride on the existing
   `Shared.Consumer.Implementation.SimpleConsumer` or a derivative to get the metrics-poll-loop
   for free and avoid the race documented in CLAUDE.md.

7. **`TreatWarningsAsErrors`** is inherited from `Source/Directory.Build.props`. The new csproj
   does NOT need to set it. Confirmed by the Memory integration test csproj not setting it.

8. **No integration test for the upstream package exists today in DNQ.** This Phase 3 project is
   the first consumer-side integration test of the TaskScheduler NuGet inside the main DNQ repo.
   The sibling repo (`/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`) has
   its own tests, but those are not visible to DNQ's CI — Phase 3 is specifically about DNQ-side
   validation of the 0.4.0 NuGet contract.

## Sources

1. `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj` (csproj template)
2. `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/AssemblyInit.cs`
3. `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Consumer/SimpleConsumer.cs`
4. `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs`
5. `Source/Directory.Packages.props` (lines 44–52; FluentAssertions 6.12.2, MSTest 4.1.0, NSubstitute 5.3.0, AutoFixture 4.18.1)
6. `Source/DotNetWorkQueue.sln` (lines 18, 162–173)
7. `Source/DotNetWorkQueueNoTests.sln` (no Memory.Integration.Tests entries — confirmed)
8. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerSetup.cs` (public API)
9. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/README.md` (usage example + Linux note)
10. MSTest 3.x/4.x docs — `[assembly: DoNotParallelize]` and `[assembly: Parallelize(Workers=N, Scope=ExecutionScope.MethodLevel)]` (no DNQ precedent found; docs-verified syntax)
11. DNQ `CLAUDE.md` lessons — metrics polling race, FluentAssertions 6.12.2 pin, MSTest runner migration blocked

## Uncertainty Flags

- **FluentAssertions 6.12.2 pin rationale** — pinned at last MIT license version per memory entry.
  The builder must NOT bump this, even if a newer version is available. Confirmed in
  `Directory.Packages.props`.
- **Whether Phase 3 needs a `VerifyQueueData.cs` equivalent** — depends on whether the tests just
  verify DI registration or also verify job execution through the Memory queue. Architect decision.
- **Whether to add the TaskScheduler package version to `Directory.Packages.props` now or inline
  it** — stylistic decision for the architect. Recommended: add to `Directory.Packages.props` for
  consistency.
- **Whether the Phase 3 project should live at `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/`**
  or under a different folder — naming/location decision for the architect.

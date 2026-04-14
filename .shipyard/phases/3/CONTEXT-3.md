# Phase 3 Context: Design Decisions

Captured during `/shipyard:plan 3` discussion. Phase 3 creates a new integration test project in the **DNQ repo** (this one) that consumes the freshly-published `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 NuGet package.

## Repository

**Target for Phase 3:** `/mnt/f/git/dotnetworkqueue` (DNQ repo, current working directory).
**Referenced package:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 (shipped in Phase 2 on 2026-04-14).

## Phase 1/2 Artifacts to Consume in Phase 3

- **Phase 2 shipped 0.4.0 to nuget.org.** The hard gate that unblocks Phase 3 (package visibility) is now open.
- The sibling repo already contains the internal-level unit tests (concurrency / state / lifecycle) added in Phase 1 PLAN-2.1. Phase 3 tests the DNQ-facing consumer story at a higher layer — no duplication of internal-class testing.

## Locked Decisions

### 1. Test the public DNQ consumer API (`InjectDistributedTaskScheduler`), not internals

**Decision:** Integration tests construct a real DNQ queue (Memory transport) and call `InjectDistributedTaskScheduler(...)` — the public consumer entry point documented in the sibling repo's `README.md`. They assert queue-level behavior: jobs dispatched across nodes, counts converge, discovery works.

**Why:** Phase 1's unit tests in the sibling repo already cover `TaskSchedulerJobCountSync` at the internal class level. Phase 3's value is proving the end-to-end integration between DNQ's queue abstraction and the 0.4.0 task-scheduler NuGet. Duplicating internal class tests in a different project gives zero new signal.

**How to apply:** Test classes construct a DNQ consumer queue via the standard DNQ helper pattern (look at existing `DotNetWorkQueue.Transport.Memory.Integration.Tests` for the idiomatic shape), call `InjectDistributedTaskScheduler(port, beaconInterface)` or the equivalent public method, and assert at the queue boundary.

### 2. Test isolation: `[DoNotParallelize]` at the assembly level

**Decision:** Serialize all tests in the new project via assembly-level `[DoNotParallelize]`. All tests run sequentially within the assembly.

**Why:** MSTest 3.x doesn't have xUnit's `[Collection]`. Tests bind NetMQ UDP beacon interfaces and TCP ports; running two of them concurrently would create hard-to-debug collisions. `[DoNotParallelize]` is the simplest, most idiomatic MSTest solution. MSTest still parallelizes *across* assemblies, but the new project will be the only NetMQ-binding project in DNQ, so cross-assembly parallelism is a non-issue.

**How to apply:** Add a `[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]` override or, more directly, `[assembly: DoNotParallelize]` in a small `AssemblyInfo.cs` or global using file inside the test project. Builder should verify the exact MSTest 3.x syntax at implementation time — MSTest 3.x changed some attribute names from 2.x.

### 3. Test scope: both lightweight (Increase/Decrease + count convergence) and full end-to-end (real DNQ jobs)

**Decision:** Implement BOTH layers:
- **`EndToEndSchedulingTests`** — dispatch actual DNQ jobs through a Memory-transport queue wired up with `InjectDistributedTaskScheduler`, assert all jobs are consumed across 2–3 in-process nodes. This is the faithful consumer story.
- **`ConcurrencyRegressionTests`** — bypass real jobs; hammer `IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount` directly from N threads against the scheduler's task-count sync. Fast, focused, catches any regression of Phase 1's lock fix under load at the DNQ-integration layer. Uses a 30-second deadlock-detector timeout like the sibling's xUnit concurrency test.
- **`NodeDiscoveryTests`** — verify that starting and stopping nodes produces the expected discovery events (RemoteCountChanged event fires, remote counts update). Asserts the wire protocol works end-to-end through the DNQ wiring, without directly reaching into internal wire-format handlers.

**Why:** Lightweight gives fast regression gating; full end-to-end proves the consumer story; discovery ensures cluster dynamics are exercised. Three classes, distinct concerns, MSTest-parallelized off via decision #2.

**How to apply:** Each test class owns its own set of nodes. Tests clean up via `[TestCleanup]` or `IDisposable`-wrapped fixtures.

### 4. Project layout and SLN wiring

- **Project path:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/`
- **Project file:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj` (note the `.Integration.Tests` suffix matches DNQ convention, e.g. `DotNetWorkQueue.Transport.Memory.Integration.Tests`)
- **Target frameworks:** `net10.0;net8.0` (matches the rest of DNQ's test projects).
- **Package management:** DNQ uses **Central Package Management** via `Source/Directory.Packages.props`. All package versions live there, NOT in individual csproj files. The Phase 3 builder must add a `<PackageVersion Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.4.0" />` entry to `Source/Directory.Packages.props` AND a bare `<PackageReference Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" />` (no `Version=` attribute) in the test project's csproj.
- **Test stack versions (pinned in `Source/Directory.Packages.props`):**
  - MSTest 4.1.0
  - NSubstitute 5.3.0
  - AutoFixture 4.18.1
  - **FluentAssertions 6.12.2** (house style, last MIT version — do not upgrade)
  Builder should inspect the Memory integration test project's csproj for the exact set of PackageReferences to clone.
- **Added to:** `Source/DotNetWorkQueue.sln` only. `DotNetWorkQueueNoTests.sln` does NOT contain any Memory integration test project entries; the Phase 3 project should also NOT be added there.
- **License header:** Every new source file must carry the LGPL-2.1 header from `DotNetWorkQueue.licenseheader` (DNQ convention — differs from the sibling repo which has none).

### 5. Consumer API + port and beacon-interface policy (verified via research)

**Public API signature** (from `TaskSchedulerSetup.cs` in the sibling repo at tag `v0.4.0`):
```csharp
public static void InjectDistributedTaskScheduler(
    this DotNetWorkQueue.IContainer container,
    int broadCastPort = 9999,
    string beaconInterface = "loopback")
```

- Namespace: `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
- Class: `TaskSchedulerSetup`
- Extends: `DotNetWorkQueue.IContainer`
- Registers: `ITaskSchedulerBus`, `ITaskSchedulerJobCountSync`, `ATaskScheduler → TaskSchedulerMultiple`, `TaskSchedulerMultipleConfiguration` — all as Singletons
- Returns: `void`

**Port collision policy.** The default `broadCastPort = 9999` means every test that constructs the scheduler with the default will try to bind 9999. Even with `[DoNotParallelize]` ensuring tests run sequentially within the assembly, lingering `TIME_WAIT` sockets between runs can still cause intermittent "address already in use" errors. **Each test class must pass an explicit port argument** from a disjoint seed range, mirroring the sibling repo's `NextPort()` pattern:
- `EndToEndSchedulingTests` — base seed **50000** (e.g., `50000 + Interlocked.Increment` counter)
- `ConcurrencyRegressionTests` — base seed **55000**
- `NodeDiscoveryTests` — base seed **60000**

These seeds are disjoint from any port the default argument would use, and disjoint from each other.

**Beacon interface policy.** The public API default is `beaconInterface = "loopback"`, which **does not work on Linux** — UDP beacon discovery silently produces no peer events with `"loopback"` on Linux. Tests that need multi-node discovery must pass `""` on Linux and `"loopback"` on Windows. Use the platform-aware helper mirroring the sibling's existing xUnit test pattern:

```csharp
private static readonly string BeaconInterface =
    System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
        System.Runtime.InteropServices.OSPlatform.Linux) ? "" : "loopback";
```

**Upstream API naming inconsistency (do NOT auto-fix).** The sibling repo's `README.md` usage example uses the named argument `udpBroadcastPort:` but the actual parameter name is `broadCastPort`. This is an upstream documentation bug in the 0.4.0 package. Phase 3 tests must use the **actual** parameter name `broadCastPort` (or positional arguments). The README bug will be filed separately as a new issue against the sibling repo — Phase 3's job is to consume 0.4.0 as-is, not patch upstream docs.

### 6. CI wiring is explicitly out of scope for Phase 3

**Decision:** Phase 3 delivers "tests green locally" only. CI wiring (Jenkinsfile + GitHub Actions) is Phase 4.

**Why:** ROADMAP.md splits these intentionally. Phase 3 establishes that the tests pass on a developer workstation before we touch CI. Phase 4 handles the Docker / UDP-beacon / stagger-startup concerns that only matter in CI.

**How to apply:** Phase 3's builder must NOT edit `Jenkinsfile` or `.github/workflows/ci.yml`. Phase 3's success criterion is the test project builds + runs 5 consecutive times green locally.

## Out-of-Scope for Phase 3

- Adding the new test project to Jenkins or GitHub Actions (Phase 4).
- Updating `CLAUDE.md` to mention the new project (can happen in Phase 4 alongside CI wiring).
- Any changes to the sibling TaskScheduler repo.
- Any changes to `DotNetWorkQueueNoTests.sln`.

## Release-Hard Constraints

- **FluentAssertions pinned at 6.12.2.** Do not upgrade. Memory entry: last MIT-licensed version.
- **No ProjectReference fallback to the sibling.** The whole point of Phase 3 is to prove the shipped NuGet works — a project reference would bypass that.
- **0.4.0 must restore from nuget.org successfully.** If `dotnet restore` can't find the package, something is wrong with the Phase 2 publish and we should investigate before writing tests.
- **License header on every new `.cs` file.**
- **`TreatWarningsAsErrors` likely on** in Release per DNQ convention — test code must be warning-clean.

## Verification Approach for Phase 3

1. After project creation: `dotnet restore "Source/DotNetWorkQueue.sln"` resolves 0.4.0 cleanly from nuget.org.
2. After each test class is added: `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` clean.
3. Per-class verification: `dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/..." --filter "FullyQualifiedName~ClassName"` green.
4. Full project verification: 5 consecutive full runs of the new project locally to shake out flakiness.
5. Full solution build: `dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true` clean.
6. Phase 3 is NOT done until all 5 local runs are green and the full solution builds clean in Release.

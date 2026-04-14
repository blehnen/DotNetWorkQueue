# Phase 3 Plan Critique — DNQ Integration Test Project for 0.4.0

**Phase:** 3
**Type:** feasibility stress test (Mode B)
**Date:** 2026-04-14
**Authored by:** orchestrator (inline) after verifier agent runs truncated mid-investigation
**Target sibling for API inspection:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` at tag `v0.4.0` / master `b904ac3`

## Scope

Per-plan feasibility checks against the live state of the DNQ repo and the sibling TaskScheduler repo (the source of the 0.4.0 NuGet). Answers the architect's two open questions and flags any remaining risks.

## Key Unknowns Resolved During Critique

### 1. Is `ITaskSchedulerJobCountSync` public in the 0.4.0 NuGet?

**YES — public.** Confirmed by partial verifier output (agent truncated during broader investigation, but this finding was explicit):

> `ITaskSchedulerJobCountSync` is **PUBLIC**, has `GetCurrentTaskCount`, `IncreaseCurrentTaskCount`, `DecreaseCurrentTaskCount`, `Start`, and a `RemoteCountChanged` event. **CRUCIAL:** The interface requires calling `Start()` explicitly — PLAN-2.2 and PLAN-2.3 do not mention this. There's no `AddWorkItem` method on this interface.

Namespace: `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`.

**Impact on plans:** PLAN-2.2 and PLAN-2.3 were REVISED inline by the orchestrator (see VERIFICATION.md) to:
- Strip the "pivot to ATaskScheduler / AddWorkItem" fallback prose (no longer needed).
- Add explicit `sync.Start()` calls before any wire traffic or Increase/Decrease loops.
- Add explicit rationale block explaining that skipping `Start()` produces a false-positive test because of the `_outbound?.Enqueue` null-safe guard from Phase 1 PLAN-1.2.

### 2. Is `SchedulerContainer` public in the 0.4.0 NuGet?

**N/A — but the right question.** `SchedulerContainer` is not from the TaskScheduler NuGet; it's a DNQ core type at `Source/DotNetWorkQueue/SchedulerContainer.cs:30`:

```csharp
public class SchedulerContainer : BaseContainer
```

Confirmed public in DNQ. It's the IoC container that scheduler consumers use as the entry point. Builder should spot-check the constructor signature during PLAN-1.1 Task 1 implementation — the README example implies `new SchedulerContainer(Action<IContainer>)` but this should be verified against the actual class.

## Per-Plan Feasibility Findings

### PLAN-1.1 (scaffold + helpers)

- **File paths:** all new, all sensible. `Source/Directory.Packages.props` exists (confirmed via earlier researcher). `Source/DotNetWorkQueue.sln` exists. `DotNetWorkQueue.licenseheader` exists at repo root (referenced for the test-file license header).
- **CPM workflow:** `<PackageVersion Include="..." Version="0.4.0" />` goes in `Directory.Packages.props`, bare `<PackageReference Include="..." />` in the csproj. Standard DNQ pattern.
- **SLN wiring:** Builder will need to pick a GUID, assign a solution folder (likely "Source" or "Source/Tests" matching the existing convention for integration test projects), and add the build configuration block. This is mechanical but tedious; the plan instructs the builder to mirror the existing `DotNetWorkQueue.Transport.Memory.Integration.Tests` entry.
- **`[assembly: DoNotParallelize]` syntax:** MSTest 4.1.0 supports the assembly-level attribute. The bare form `[assembly: DoNotParallelize]` is sufficient. No complementary `Parallelize` attribute required.
- **TestHelpers.cs ingredients:** `BeaconInterface` constant (RuntimeInformation.IsOSPlatform check), three port-base constants (50000/55000/60000), and a `NextPort(ref int)` method. All documented in the plan.
- **Risk:** LOW. Pure scaffolding.

### PLAN-2.1 (EndToEndSchedulingTests)

- **File paths:** new `EndToEndSchedulingTests.cs`, reads `TestHelpers.cs` from PLAN-1.1.
- **API surface:** uses `SchedulerContainer` + `InjectDistributedTaskScheduler` + Memory transport + DNQ producer/consumer queue wiring. The plan references RESEARCH.md section 3 for the Memory transport pattern.
- **Potential hidden dependency:** Memory transport integration tests in DNQ typically need a specific `IJobScheduler` / `IProducerMethodJobQueue` wire-up. The plan should spot-check the existing `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests` for the idiomatic pattern at implementation time.
- **Metric race polling pattern:** CLAUDE.md has a lesson about `IMetrics` polling (handler callback signals completion before `CommitMessage.Commit()` increments the counter). The plan doesn't explicitly cite this pattern; the builder should use polling-with-deadline for completion assertions, not single snapshots.
- **Risk:** MEDIUM. Real end-to-end DNQ integration is nontrivial; flakiness risk is higher than the internal-only tests.

### PLAN-2.2 (ConcurrencyRegressionTests)

- **File paths:** new `ConcurrencyRegressionTests.cs`, reads `TestHelpers.cs`.
- **API surface:** uses `ITaskSchedulerJobCountSync` directly via container resolution (confirmed public).
- **CRITICAL REVISION (orchestrator inline):** added explicit `sync.Start()` requirement before the Increase/Decrease hammering. Without it, the test is a silent false positive that would pass even if Phase 1's lock fix were reverted.
- **30s deadlock detector:** 12 threads × 5000 iterations each = 60k op pairs. The sibling's xUnit equivalent runs in ~3s. DNQ-layer overhead may slow this 2–4× due to container resolution — still well under 30s.
- **Risk:** MEDIUM (was HIGH before the Start() fix).

### PLAN-2.3 (NodeDiscoveryTests)

- **File paths:** new `NodeDiscoveryTests.cs`, reads `TestHelpers.cs`.
- **API surface:** two SchedulerContainers sharing one UDP port, resolving `ITaskSchedulerJobCountSync` from each (confirmed public), subscribing to `RemoteCountChanged`.
- **CRITICAL REVISION (orchestrator inline):** added explicit `syncA.Start()` and `syncB.Start()` requirement before discovery assertions. Without Start() the poller never runs, no wire messages flow, the discovery test hangs until the 10s timeout.
- **Linux beacon interface policy:** plan correctly uses `TestHelpers.BeaconInterface` (`""` on Linux, `"loopback"` on Windows). The 10s deadline is tight but matches UDP beacon discovery latency expectations.
- **Teardown decay assertion:** polls node B's `GetCurrentTaskCount()` with a 15s deadline for the post-dispose-A decay. The 0.4.0 code's `RemovedNode` handler removes the peer from `_otherProcessorCounts`, so aggregate count should drop. Polling-with-deadline is the right pattern.
- **Risk:** MEDIUM. UDP beacon discovery in local networking is sometimes flaky; the 10-15s deadlines are generous but not infinite.

### PLAN-3.1 (full verification)

- **Verification scope:** 5 consecutive runs of the full new test project, then a full-solution Release build.
- **Solution path:** `Source/DotNetWorkQueue.sln` — confirmed exists. `dotnet build ... -c Release -p:CI=true` is the standard DNQ Release invocation.
- **Risk:** LOW. Final gate.

## File Conflict Check (Wave 2 parallel-safety)

Plans in Wave 2 (PLAN-2.1, PLAN-2.2, PLAN-2.3) all create DIFFERENT test files:
- `EndToEndSchedulingTests.cs`
- `ConcurrencyRegressionTests.cs`
- `NodeDiscoveryTests.cs`

All three READ `TestHelpers.cs` from PLAN-1.1 Task 3 but none modify it. Parallel-safe.

## CI Touchpoints Check

Per CONTEXT-3 decision #6: Phase 3 must NOT edit `.github/workflows/ci.yml` or `Jenkinsfile`. Spot-check of all 5 plans confirms none reference these files. PASS.

## Complexity Flags

No plan touches >10 files or crosses >3 directories. All stay within `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/` plus `Source/Directory.Packages.props` and `Source/DotNetWorkQueue.sln` (2 root-level edits for CPM + sln wiring in PLAN-1.1).

## Verdict: **READY** (after inline revisions)

All file paths exist or are sensible new paths. The architect's two open questions are resolved (`ITaskSchedulerJobCountSync` is public, `SchedulerContainer` is a DNQ core public type). The critical Start() issue flagged by the partial verifier was addressed via surgical edits to PLAN-2.2 and PLAN-2.3. Wave structure is clean, no file collisions, no CI scope creep.

Builder can proceed.

## Builder Notes (carry into Wave 1)

1. At PLAN-1.1 Task 1 kickoff: run `dotnet restore` against a tiny scratch project that references `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0` to verify the package resolves from nuget.org. If restore fails, STOP — Phase 3 is fundamentally blocked on 0.4.0 visibility.
2. Confirm `SchedulerContainer` constructor signature before PLAN-2.1 test body authoring. Source: `Source/DotNetWorkQueue/SchedulerContainer.cs`.
3. At every test body: use positional args on `InjectDistributedTaskScheduler(container, port, beaconInterface)`. Any `udpBroadcastPort:` named-arg usage is a grep-failure (verify commands already guard against it).
4. Every test that touches `ITaskSchedulerJobCountSync` MUST call `Start()` explicitly before Increase/Decrease/discovery work, and MUST call `Dispose()` in cleanup.

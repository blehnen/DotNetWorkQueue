# Phase 3 Verification — DNQ Integration Test Project for TaskScheduler 0.4.0

**Phase:** 3
**Type:** plan-review (Mode A)
**Date:** 2026-04-14
**Authored by:** orchestrator (inline) after verifier agent runs truncated mid-investigation

Phase 3 creates `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/` in the DNQ repo, consuming the freshly-published 0.4.0 NuGet package via CPM.

## Plan Coverage Check

| Plan | Wave | Tasks | Outcome |
|------|------|-------|---------|
| PLAN-1.1 | 1 | 3 | Scaffolding: csproj + CPM entry, SLN wiring, AssemblyInit + TestHelpers |
| PLAN-2.1 | 2 | 2 | EndToEndSchedulingTests + 5x flakiness loop |
| PLAN-2.2 | 2 | 2 | ConcurrencyRegressionTests + 5x flakiness loop (REVISED inline to use confirmed-public interface + explicit Start()) |
| PLAN-2.3 | 2 | 2 | NodeDiscoveryTests + 5x flakiness loop (REVISED inline for same reason) |
| PLAN-3.1 | 3 | 2 | Full-project 5x loop + solution Release build |

Wave 2's three plans touch strictly disjoint files (three separate test `.cs` files). They all read from `TestHelpers.cs` (created in PLAN-1.1 Task 3) but none modify it — parallel-safe.

## Phase 3 Success Criteria (from ROADMAP lines 229–241)

| # | Criterion | Covered by |
|---|-----------|-----------|
| 1 | Project builds clean for net10.0 and net8.0 | PLAN-1.1 Task 2 verify, PLAN-3.1 Task 2 |
| 2 | All three test classes pass locally; 5 consecutive runs in loop | PLAN-2.1/2.2/2.3 Task 2 (per-class) + PLAN-3.1 Task 1 (full suite) |
| 3 | Test project references NuGet 0.4.0 (no project reference) | PLAN-1.1 Task 1 (`Directory.Packages.props` PackageVersion + bare PackageReference) |
| 4 | `dotnet build -c Release -p:CI=true` clean | PLAN-3.1 Task 2 |
| 5 | All pre-existing DNQ tests continue to pass | PLAN-3.1 Task 2 (solution-wide Release build + test) |

## CONTEXT-3 Decision Coverage

| Decision | Status | Where |
|----------|--------|-------|
| #1 Public API via InjectDistributedTaskScheduler | HONORED | All Wave 2 plans use `container.InjectDistributedTaskScheduler(port, beaconInterface)` |
| #2 `[assembly: DoNotParallelize]` | HONORED | PLAN-1.1 Task 3 creates AssemblyInit.cs |
| #3 Both lightweight + end-to-end tests | HONORED | PLAN-2.1 (end-to-end jobs), PLAN-2.2 (lightweight counter hammering), PLAN-2.3 (discovery) |
| #4 Project layout + CPM + sln wiring | HONORED | PLAN-1.1 Tasks 1 & 2 |
| #5 Public API signature + port seeds + beacon policy + ISSUE-030 workaround | HONORED | All Wave 2 plans use positional args, disjoint seeds 50000/55000/60000, TestHelpers.BeaconInterface |
| #6 CI wiring out of scope | HONORED | No plan touches Jenkinsfile or .github/workflows/ci.yml |

## Structural Rules

- Task count per plan: all plans ≤ 3 tasks (1.1=3, 2.1=2, 2.2=2, 2.3=2, 3.1=2) ✓
- Wave dependencies: Wave 2 depends on Wave 1, Wave 3 depends on Wave 2 ✓
- Intra-wave file conflicts: none (three disjoint test files in Wave 2) ✓
- Acceptance criteria are testable (every plan has dotnet test / dotnet build verify commands) ✓

## Inline Revisions Made (2 surgical edits)

Two issues flagged during verifier investigation and fixed inline by the orchestrator rather than dispatching another architect round:

1. **PLAN-2.2 Task 1 — Visibility fallback stripped, explicit Start() added.**
   The plan originally hedged on whether `ITaskSchedulerJobCountSync` was public in the 0.4.0 NuGet and included a pivot-to-ATaskScheduler fallback. Verifier investigation confirmed the interface IS public (namespace `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`, members `GetCurrentTaskCount`, `IncreaseCurrentTaskCount`, `DecreaseCurrentTaskCount`, `Start`, event `RemoteCountChanged`). The fallback prose was replaced with confirmed-public guidance. Additionally, an explicit `sync.Start()` step was added with a rationale block: without Start(), the `_outbound?.Enqueue(...)` null-safe guard added in Phase 1 PLAN-1.2 causes every enqueue to silently no-op, making the test a **false positive** that would pass even if Phase 1's lock fix were reverted. A test that doesn't call Start() doesn't exercise the poller path at all.

2. **PLAN-2.3 Task 1 — Visibility fallback stripped, explicit Start() added for BOTH nodes.**
   Same fix as above, applied to both `[TestMethod]` blocks in NodeDiscoveryTests. Both `syncA.Start()` and `syncB.Start()` must be called before any wire traffic flows — otherwise the discovery test hangs until the 10s timeout expires with no peer events.

Both edits were made directly in the plan files; no content was removed or restructured beyond these two sections.

## Open Questions (non-blocking)

- **`SchedulerContainer` usage shape.** The plan references `new SchedulerContainer(RegisterService)` from RESEARCH.md's README quote. Verifier investigation confirmed `SchedulerContainer` is a public class in DNQ core at `Source/DotNetWorkQueue/SchedulerContainer.cs:30` (`public class SchedulerContainer : BaseContainer`). Builder should spot-check the constructor signature during PLAN-1.1 implementation — the name suggests `Action<IContainer> registerService` but the exact shape should be verified against the class definition.
- **Container disposal propagation to the sync Singleton.** PLAN-2.3 says "verify at implementation time" whether disposing `SchedulerContainer` propagates to the registered `ITaskSchedulerJobCountSync` Singleton. Builder should confirm and adjust cleanup code if necessary.

## Mode A Verdict

**PASS (after inline revisions).** All phase requirements are covered, all CONTEXT-3 decisions are honored, and the two outstanding verifier-flagged issues have been addressed via surgical edits to PLAN-2.2 and PLAN-2.3.

Proceed to task scaffolding + commit.

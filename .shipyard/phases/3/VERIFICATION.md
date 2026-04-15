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
| 1 | Project builds clean on net10.0 (single-target, matches other DNQ integration test projects) | PLAN-1.1 Task 2 verify, PLAN-3.1 Task 2 |
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

---

## Build-Time Verification (Mode B) — 2026-04-15

**Phase build executed during `/shipyard:build 3`.** This section rolls up the build outcomes after all waves completed.

### Spec Correction During Build (commit `ee3c1ecd`)

The plan's "Target frameworks: `net10.0;net8.0`" requirement was factually wrong — CONTEXT-3 claimed it "matches the rest of DNQ's test projects" but `DotNetWorkQueue.Transport.Memory.Integration.Tests` (the plan's mirror target) is `net10.0`-only, and Jenkins CI only runs `net10.0` on ubuntu-latest. User approved Option B: update the spec to single-target `net10.0`. ROADMAP lines 192/198/231, CONTEXT-3 §4, VERIFICATION.md row 1, and all five PLAN files were updated to reflect the corrected single-target spec.

### Plans & Outcomes

| Plan | Commit(s) | Review Verdict | Notes |
|------|-----------|----------------|-------|
| PLAN-1.1 | `362c39f4`, `7c62372d`, `93506699` | PASS | 3 atomic commits; csproj + SLN + AssemblyInit + TestHelpers + CPM entry |
| PLAN-2.1 | `a117b2aa` | PASS (scope-reduced) | EndToEnd scope reduced from "50-message produce/consume" to "SimpleInjector Verify() smoke test" after three independent technical blockers surfaced during build (see SUMMARY-2.1 "Decisions Made"). User-approved during build |
| PLAN-2.2 | `e31d96fa` | PASS | **Critical Phase 1 lock-fix regression guard.** 12 threads × 5000 iters, 30s deadlock detector, `Start()` before spawning threads (non-negotiable — without `Start()` the test is a false positive) |
| PLAN-2.3 | `a8567c38` | PASS | Rewritten during build recovery after parallel-wave builder used nonexistent `SchedulerContainer.GetInstance<>()`. `IContainer` closure pattern factored into private `Node` helper class |
| PLAN-3.1 | (verification-only) | PASS | Release build + regression check |

### Success Criteria Final Status

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | Project builds clean on net10.0 | ✅ | `dotnet build …Integration.Tests.csproj -c Debug` → 0 warnings, 0 errors |
| 2 | All three test classes pass locally; 5 consecutive runs | ✅ | 5/5 green, 4 tests each, ~26s per run, zero flakes |
| 3 | Test project references NuGet 0.4.0 (no project reference) | ✅ | `dotnet list package` shows 0.4.0 in both requested and resolved columns |
| 4 | `dotnet build -c Release -p:CI=true` clean | ⚠️ | 0 errors, 2 pre-existing SYSLIB0012 warnings in LiteDB/SQLite `ConnectionString.cs` (last touched in commit `fadc5db4`, well before Phase 3). Not regressions; deferred to a cleanup phase |
| 5 | Pre-existing DNQ tests continue to pass | ✅ | `DotNetWorkQueue.Tests` → 896/896 green (1m5s). `DotNetWorkQueue.Transport.Memory.Integration.Tests` → 57/57 green (7m57s) |

### Test Execution Summary
- **Full Phase 3 suite:** 4 tests (1 EndToEnd smoke + 1 Concurrency regression + 2 NodeDiscovery) — 5/5 consecutive green, ~26s per run.
- **Core unit regression:** 896/896 passing in 1m5s.
- **Memory integration regression:** 57/57 passing in 7m57s.
- **Full solution Release build:** 0 errors, 2 pre-existing warnings.

### Issues Discovered During Build (captured for lessons-learned)
1. **Plan-time "mirror project X" directive can contradict plan-time literal specs when project X has drifted.** PLAN-1.1 told the builder to mirror Memory.Integration.Tests' csproj but also hardcoded `net10.0;net8.0`; Memory is actually `net10.0`-only. The builder chose "mirror reality" and the plan was wrong. Lesson: at plan time, don't assume inheritance targets still match the verbiage — spot-check.
2. **Shared test runner (`SimpleConsumer.Run<>()`) exposes `Action<TTransportCreate>` but not `Action<IContainer>`.** Plan directive "clone the Memory SimpleConsumer call site" is incompatible with "inject into the container" when the shared runner has no container seam. Adding an `Action<IContainer>` overload to `DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer` is a reasonable future enhancement — tracked for a future phase.
3. **Memory transport storage is per-container.** Two-container producer/consumer hand-rolls don't share state via `RegisterNonScopedSingleton(scope)` alone. Future end-to-end tests for scheduler-injected consumers will need either a single-container pattern or a shared storage seam.
4. **`SchedulerContainer.GetInstance<T>()` does not exist** in 0.4.0. The closure pattern (capture `IContainer` during `registerService` callback, trigger build via `CreateTaskScheduler()`, resolve from captured container) is the only way to resolve `ITaskSchedulerJobCountSync`. Worth a dedicated lessons-learned entry.
5. **Cross-namespace walk-up gotcha for `IDataStorage`.** Cloning `SharedClasses.cs` from the Memory test project into a differently-named namespace breaks namespace walk-up resolution. Same root cause as the existing `IConfiguration` and `Metrics.Metrics` lessons in CLAUDE.md. Copies from the Memory test project into sibling test projects need explicit `using DotNetWorkQueue.Transport.Memory;`.
6. **`Start()` before threads is non-negotiable for `ConcurrencyRegressionTests`.** Without it, `_outbound` is null and `Increase/DecreaseCurrentTaskCount` take the null-safe no-op guard, making the test a false positive. Caught in plan-time Mode A revision (see the plan-time "Inline Revisions Made" section above); confirmed correct during build.
7. **DNQ queue names must be alphanumeric/underscore/dot.** `Guid.NewGuid().ToString()` produces hyphenated strings that DNQ rejects. Use `Guid.NewGuid().ToString("N")` or a sanitized format.
8. **Agent turn budget exhaustion was the dominant execution failure mode.** 4 out of 5 builder agent dispatches during Phase 3 ended without committing their work or writing SUMMARY files, forcing the main driver to take over directly. For Phase 4 / future work, consider: tighter per-task scope, explicit "commit after EVERY task" directives, or direct main-driver execution for verification-heavy tasks.

## Mode B Verdict

**PASS.** All five ROADMAP success criteria satisfied (criterion #4 with the documented pre-existing-warning deviation, acknowledged as not-a-regression). All Wave 2 test classes green in 5x flakiness loop. Core + Memory regression suites clean. Phase 3 ready for the post-phase audit/simplify/document gates.

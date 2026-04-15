# Milestone Report: TaskScheduler 0.4.0 + DNQ Integration Tests + CI Wiring

**Completed:** 2026-04-15
**Shipped via:** PR #115 merged to master (commit `190f1226`)
**Phases:** 4/4 complete
**Cross-repo scope:** Phases 1-2 shipped from `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`, Phases 3-4 shipped from `DotNetWorkQueue` (this repo)

## Overview

Fixed NetMQ lock contention in `TaskSchedulerJobCountSync` (issue #6), released the fix as NuGet `0.4.0` on nuget.org, added a new integration test project in DNQ that consumes the published package, and wired the new tests into both CI surfaces (Jenkins + GitHub Actions).

## Phase Summaries

### Phase 1: TaskScheduler Lock Contention Fix + Unit Tests — SHIPPED
**Repo:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (branch `phase-1-lock-fix`, 12 commits)

Eliminated `_lockSocket` contention in `TaskSchedulerJobCountSync` so `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` no longer stall up to 10ms behind the receive loop. Replaced the `TryReceiveFrameString(10ms)` polling pattern with a `NetMQPoller` that owns `_actor` on a single dedicated thread, and routed outbound `SetCount` messages through a `NetMQQueue<T>`.

**Outcome:** `_lockSocket = 0`, 9/9 unit tests green (concurrency 5/5 in loop), Release build clean. Audit CLEAN, Simplification production-clean (2 deferred), Documenter CHANGELOG draft ready for Phase 2.

### Phase 2: TaskScheduler NuGet 0.4.0 Release — SHIPPED
**Repo:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`

0.4.0 is live on nuget.org, published via GH Actions tag-triggered workflow mirroring 0.3.0. Release commit `b904ac3` (5 files, +23/-2), tag `v0.4.0` (annotated, unsigned). Both `.nupkg` and `.snupkg` published cleanly via run 24423676631. Symbols + deterministic Source Link confirmed green on nuget.org.

**Deferred gates by explicit user decision:** audit / simplifier / documenter — Phase 2 diff was 5-file version/text/doc, no code logic changes, no new deps.

**Closed:** ISSUE-028 (Start() remarks XML doc landed in release commit).
**Opened:** ISSUE-029 (GH Actions Node.js 20 deprecation advisory).

### Phase 3: DotNetWorkQueue Integration Test Project — COMPLETE
**Repo:** `DotNetWorkQueue` (this repo)

Created `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/` as a net10.0-only test project that `PackageReference`s `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0` via Central Package Management.

**Three test classes:**
- **ConcurrencyRegressionTests** (PLAN-2.2) — **critical cross-repo regression guard for Phase 1's lock fix.** 12 threads × 5000 iterations hammering `Increase/DecreaseCurrentTaskCount`, 30s deadlock detector via `Task.WaitAll`, final count asserted == 0 via FluentAssertions. `Start()` called before spawning threads so `_outbound` is initialized and the real concurrency path is exercised (without `Start()`, the null-safe guard from Phase 1 makes the test a false positive).
- **NodeDiscoveryTests** (PLAN-2.3) — UDP beacon discovery convergence + disposal decay. Two `SchedulerContainer` instances share one port, assert `RemoteCountChanged` convergence and post-dispose decay.
- **EndToEndSchedulingTests** (PLAN-2.1) — **scope-reduced during build** to a SimpleInjector `Verify()` smoke test. The shared `SimpleConsumer.Run<>()` runner has no `Action<IContainer>` seam for the distributed scheduler injection, Memory transport storage is per-container, and `SharedSetup` / `VerifyMetrics` / `Metrics.Metrics` are internal to the shared test project. Smoke test proves `InjectDistributedTaskScheduler` passes SimpleInjector `Verify()` in a real DNQ consumer container.

**Spec correction during build:** `TargetFrameworks` changed from `net10.0;net8.0` → `net10.0` single-target. The plan's claim "matches the rest of DNQ's test projects" was factually wrong — every other DNQ integration test project is `net10.0`-only, and Jenkins CI runs `net10.0` only on ubuntu-latest.

**5/5 flakiness loop:** full suite runs green consecutively, ~26s per run, zero flakes. **896/896 core unit tests** and **57/57 Memory integration tests** continue to pass (regression check).

### Phase 4: CI Wiring (Jenkins + GitHub Actions) — SHIPPED (via PR #115)
**Repo:** `DotNetWorkQueue`

- **`.github/workflows/ci.yml`** +3 lines: appended `Integration Tests - TaskScheduler Distributed` step after the last unit-test step (first integration test in `ci.yml`).
- **`Jenkinsfile`** +12 lines: appended `stage('TaskScheduler Distributed')` as the 14th parallel stage after `stage('Dashboard')` with `sleep(time: 65, unit: 'SECONDS')` following the `(n-1)*5` stagger formula.

**Total Phase 4 diff:** 15 insertions, 0 deletions, 2 files. Strictly additive.

**Optimistic UDP decision paid off:** NetMQ beacon discovery on Docker bridge network worked on the first Jenkins run of PR #115. No `[TestCategory("BeaconRequired")]` skip mechanism needed.

**All 14 Jenkins parallel stages + GitHub Actions build-and-test job green** on the PR before merge.

## Key Decisions

1. **Phase 2 — audit/simplifier/documenter deferred** (Phase 2 was a 5-file version/text/doc change with no code logic changes).
2. **Phase 3 — target framework corrected to net10.0 single-target** mid-build after discovering the plan's "matches DNQ test projects" rationale was incorrect.
3. **Phase 3 — EndToEndSchedulingTests scope reduced** to a SimpleInjector `Verify()` smoke test. The shared runner's seam limitation and Memory transport's per-container storage made the original "produce 50 messages, consume them" pattern infeasible without modifying shared test infrastructure. ConcurrencyRegressionTests is Phase 3's real regression guard.
4. **Phase 4 — optimistic UDP multicast** (no pre-emptive `[TestCategory]` skip, no `--network=host`). Risk accepted; outcome: beacon worked on the first Jenkins run.
5. **Phase 4 — new stage excluded from Codecov** (no `--collect`, no `--settings`, no `--results-directory`, no `stash`). The Phase 3 project tests an external NuGet; its ProjectReference DLLs are already covered by the other 13 Jenkins stages.
6. **Phase 4 — stage appended at end** of Jenkins parallel block (stagger 65s), preserving the existing 13 stages' stagger offsets one-for-one.
7. **Pre-ship — local master merged origin/master**. Local master had been working the TaskScheduler milestone in isolation while origin shipped Phase 5 of the dashboard-coverage milestone (PR #114). The two diverged; a merge commit unified them, then `phase-4-ci-wiring` was rebased onto the unified master so PR #115 could ship cleanly.

## Documentation Status

- **Per-phase SUMMARY files:** complete for all plans across Phases 1-4 in `.shipyard/phases/{N}/results/`
- **Per-phase REVIEW files:** complete, all PASS verdicts
- **Per-phase VERIFICATION:** complete, all PASS
- **Per-phase AUDIT:** Phase 1 (part of Phase 1 ship), Phase 3 (PASS, 0 blocking), Phase 4 (PASS, 0 blocking, 2 informational). Phase 2 audit deferred.
- **Per-phase SIMPLIFICATION:** Phase 3 PASS_NO_ACTION, Phase 4 PASS_NO_ACTION
- **Per-phase DOCUMENTATION:** Phase 3 + Phase 4 both PASS_NO_ACTION, deferred to ship time
- **CLAUDE.md updates** (pending ship cleanup): 13 → 14 parallel Jenkins stages; new lessons from this milestone
- **CHANGELOG:** Phase 2 shipped a release commit in the sibling repo with CHANGELOG + README updates for `v0.4.0`

## Known Issues

**30 open issues** in `.shipyard/ISSUES.md` — accumulated across multiple milestones, preserved for the next cleanup pass. Notable Phase 3 / Phase 4 entries:
- **ISSUE-030** — README usage example in sibling `TaskScheduler` repo uses the wrong named argument (`udpBroadcastPort:` instead of `broadCastPort:`). Workaround applied in Phase 3 tests: positional args only.
- **Pre-existing `SYSLIB0012` warnings** in `LiteDB/SQLite ConnectionString.cs` — `Assembly.CodeBase` is obsolete in net10.0. Trivial cleanup, deferred.
- **Jenkinsfile stagger at ceiling** — 14 stages × 5s = 65s worst-case startup delay. A future 15th stage will need formula revisit.

## Metrics

**Commits:** 20 on the TaskScheduler milestone branches (4 phases), plus 1 GitHub merge commit on master (`190f1226`).

**Files created:**
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/` — 7 `.cs` files (AssemblyInit, TestHelpers, SharedClasses, EndToEndSchedulingTests, ConcurrencyRegressionTests, NodeDiscoveryTests, csproj)
- `.shipyard/phases/1/` through `.shipyard/phases/4/` — plan + result artifacts

**Files modified:**
- `Source/Directory.Packages.props` — added CPM entry for `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0`
- `Source/DotNetWorkQueue.sln` — added new test project entry
- `.github/workflows/ci.yml` — added integration test step
- `Jenkinsfile` — added 14th parallel stage
- `.gitignore` — added `.worktrees/` pattern

**Regression check:** 896/896 core unit tests + 57/57 Memory integration tests + 4/4 new TaskScheduler Distributed integration tests — all green on the merged master head.

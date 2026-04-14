# Build Summary: Plan 2.1 (Concurrency + state + lifecycle test suite)

## Status: complete

Wave 4 of Phase 1 — the test suite that exercises the refactored production code from Waves 1–3. Three new xUnit test files added to the existing Tests project. No production code touched.

## Tasks Completed

- **Task 1 — Concurrency regression test** — complete — commit `03ccba6`
  - New file: `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncConcurrencyTests.cs` (106 lines).
  - 12 producer threads, 5000 iterations each, alternating Increase/Decrease. 30s deadlock-detector deadline. Final count asserted against expected delta.
  - Port base seed **50000** (disjoint from existing 40000–49999).
  - **Concurrency test ran 5 consecutive times in a loop: 5/5 pass, durations 6s, 3s, 3s, 3s, 3s — far under the 30s deadline, no flakiness observed.**

- **Task 2 — State consistency test** — complete — commit `4bea71b`
  - New file: `TaskSchedulerJobCountSyncStateTests.cs` (91 lines).
  - Two-node test with real `TaskSchedulerBus` on a shared port. Node A issues a scripted `Increase`×3 + `Decrease`×1 sequence; waits up to 15s for Node B's `GetCurrentTaskCount()` to converge to 2L.
  - Port base seed **55000**.

- **Task 3 — Lifecycle test** — complete — commit `4a89ca7`
  - New file: `TaskSchedulerJobCountSyncLifecycleTests.cs` (72 lines).
  - `Start() → operate → Dispose()` within a 10s bounded deadline. Catches any regression of the old `while(_running) Sleep(100)` hot-wait or a poller-thread that fails to exit.
  - Port base seed **60000**.

## Files Modified (sibling repo)

- `Source/.../Tests/TaskSchedulerJobCountSyncConcurrencyTests.cs` (new)
- `Source/.../Tests/TaskSchedulerJobCountSyncStateTests.cs` (new)
- `Source/.../Tests/TaskSchedulerJobCountSyncLifecycleTests.cs` (new)

## Decisions Made (Pre-flight Plan Divergences)

The builder did pre-flight reads of the existing test file and `TaskSchedulerBus.cs` before writing code — this surfaced three plan-snippet bugs that would have failed to compile. All three were fixed during Task 1 authoring; no separate commits needed.

1. **`TaskSchedulerBus` constructor is `(ILogger, TaskSchedulerMultipleConfiguration)`**, NOT `(logger, port, beaconInterface)` as every plan snippet assumed. All three new test files wrap port+interface in `new TaskSchedulerMultipleConfiguration(port, BeaconInterface)` before passing to `TaskSchedulerBus`.
2. **Task 2 port semantics:** the existing `TwoNodesDiscoverEachOtherAndSyncCounts` test (lines 58–66) uses a **single shared port** for both node configs. Confirmed against `TaskSchedulerBus` constructor behavior. New state test uses `portA == portB`, one `NextPort()` call only. The plan explicitly asked the builder to verify this and fix if needed.
3. **`BeaconInterface` kept as a `static readonly string` field** per existing file convention, not inlined per-test.
4. **`XunitLogger`** copied byte-identically from the existing file (lines 154–168), non-generic as required.
5. **`sync.Start()` called directly** in new tests (not wrapped in `Task.Run`) per the PLAN-2.1 explicit override. Existing tests still use `_ = Task.Run(() => sync.Start())` for legacy diff-minimization — the new tests' direct call and consistent ~3s execution time confirms that `Start()` is truly non-blocking after PLAN-1.3.

## Issues Encountered (Lesson Seeds)

- **Plan snippets referencing APIs the architect hadn't read.** Same class of bug as Wave 3's `AddedNode`/`BroadCast` mistake. Captured as a Phase 1 lessons-learned item: when a plan embeds inline code snippets that reference existing class constructors, the architect must cross-reference the actual constructor signature before writing the snippet. Otherwise every test the builder writes hits a compile break, and the builder has to reverse-engineer the real API from existing tests at build time.
- `git log ... | head -N` in WSL can yield exit code 1 via SIGPIPE without being an error. Prefer `git log -n N` over piping to head.

## Verification Results

- **Concurrency 5x loop:** 5/5 PASS, durations 6s / 3s / 3s / 3s / 3s (far under the 30s deadline).
- **Full test suite:** `Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9` (net8.0). All 6 original + 3 new.
- **Release build `-c Release -p:CI=true`:** 0 errors, 0 warnings on net8.0 and net10.0. `TreatWarningsAsErrors=true` gate held.
- **Regression guard:** `grep -c _lockSocket Source/TaskSchedulerJobCountSync.cs` = **0** (Phase 1 success criterion #1 still holds).

## Readiness

Phase 1 is in its final shape. Branch `phase-1-lock-fix` has **11 commits** (2 PLAN-1.1 + 3 PLAN-1.2 + 2 PLAN-1.3 + 1 orchestrator fix-up + 3 PLAN-2.1). Ready for phase verification, audit, simplifier, documenter, and the eventual merge to master that unblocks Phase 2 (NuGet 0.4.0 release).

<!-- context: turns=16, compressed=no, task_complete=yes -->

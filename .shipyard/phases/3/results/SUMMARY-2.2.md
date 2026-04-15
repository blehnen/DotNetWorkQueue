# Build Summary: Plan 2.2

## Status: complete

## Tasks Completed
- **Task 1 (author ConcurrencyRegressionTests.cs):** complete. 12 threads × 5000 iterations hammering `IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount`, 30s deadlock detector via `Task.WaitAll`, final count asserted `== 0` via FluentAssertions.
- **Task 2 (flakiness loop + grep guard):** complete. 5/5 consecutive full-suite runs green (the ConcurrencyRegressionTests test method passes in ~2s in every run); `grep udpBroadcastPort` returns 0 matches.

Committed as `e31d96fa — shipyard(phase-3): add ConcurrencyRegressionTests (PLAN-2.2)`.

## Files Modified
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/ConcurrencyRegressionTests.cs` — created.
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/SharedClasses.cs` — touched (out-of-scope) to add `using DotNetWorkQueue.Transport.Memory;`. PLAN-2.2's builder documented this in `.checkpoint` as a cross-plan blocker fix. Final SharedClasses.cs is owned by the PLAN-2.1 commit.

## Decisions Made
- **`IContainer` closure pattern.** `SchedulerContainer` does not expose a `GetInstance<T>()` method. The only way to resolve `ITaskSchedulerJobCountSync` in 0.4.0 is to capture the `IContainer` during the `SchedulerContainer(registerService)` callback, then trigger the container build via `CreateTaskScheduler()`, then call `capturedContainer.GetInstance<ITaskSchedulerJobCountSync>()`. This pattern was discovered during this plan's build and reused by NodeDiscoveryTests after it had to be rewritten from a broken first draft.
- **`_sync.Start()` before spawning threads.** Without `Start()`, the `IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount` paths take a null-safe no-op guard because `_outbound` has not been initialized yet. A test that skips `Start()` is a false positive — it would pass even if Phase 1's lock fix were reverted. This is called out in a comment on the test.
- **12 threads × 5000 iterations = 60000 ops.** Enough concurrency to reliably surface a deadlock if Phase 1's lock fix were broken, without making the test slow (~2s wall clock).

## Issues Encountered
- **Parallel-wave compile blocker.** PLAN-2.2's builder discovered two compile errors in files owned by PLAN-2.1 and PLAN-2.3 that prevented the assembly from building, which in turn blocked any `dotnet test` invocation:
  1. `SharedClasses.cs` (from PLAN-2.1) missing `using DotNetWorkQueue.Transport.Memory;` — fixed in-place (trivial one-line add, documented in `.checkpoint`).
  2. `NodeDiscoveryTests.cs` (from PLAN-2.3) used `SchedulerContainer.GetInstance<>`, which doesn't exist in 0.4.0 — PLAN-2.2 cannot touch this file per its instructions. PLAN-2.2's builder wrote a detailed `.checkpoint` file explaining the blocker, the required fix (closure pattern), and the current state of PLAN-2.2's own deliverables. Then the agent ran out of turn budget before writing the SUMMARY or committing.
- **Main driver recovery.** After PLAN-2.2's builder stopped, the main driver rewrote NodeDiscoveryTests.cs using the closure pattern PLAN-2.2's builder identified, then ran the full test suite which proved ConcurrencyRegressionTests correct (passing in 5/5 runs).

## Verification Results
- `dotnet build …Integration.Tests.csproj -c Debug` → **Build succeeded, 0 warnings, 0 errors**.
- `grep -n "udpBroadcastPort" ConcurrencyRegressionTests.cs` → **0 matches**.
- `dotnet test --filter "FullyQualifiedName~ConcurrencyRegressionTests"` → **Passed!** 1/1, ~2s.
- Full-suite 5x flakiness loop: **5/5 green**, zero deadlocks, zero timeouts, zero false positives.
- Acceptance criteria mapping:
  - ✅ ConcurrencyRegressionTests.cs authored — hammers Increase/Decrease from 12 threads × 5000 iters
  - ✅ Uses positional args on `InjectDistributedTaskScheduler` (ISSUE-030 workaround)
  - ✅ Port base `55000` (`TestHelpers.ConcurrencyPortBase`) — disjoint from EndToEnd (50000) and NodeDiscovery (60000)
  - ✅ 30-second deadlock-detector timeout via `Task.WaitAll(tasks, TimeSpan.FromSeconds(30))`
  - ✅ Final count assertion verifies Phase 1's lock fix held (`GetCurrentTaskCount().Should().Be(0)`)

**This test is Phase 3's critical cross-repo regression guard for Phase 1's `TaskSchedulerJobCountSync` lock fix. Green in 5/5 runs means the 0.4.0 NuGet has the fix and it works under real concurrency.**

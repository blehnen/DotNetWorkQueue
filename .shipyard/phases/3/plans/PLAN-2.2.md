---
phase: phase-3
plan: 2.2
wave: 2
dependencies: [1.1]
must_haves:
  - ConcurrencyRegressionTests.cs authored — hammers Increase/Decrease from N threads
  - Uses positional args on InjectDistributedTaskScheduler (ISSUE-030 workaround)
  - Port base 55000 (TestHelpers.ConcurrencyPortBase) — disjoint from other test classes
  - 30-second deadlock-detector timeout mirroring the sibling xUnit concurrency test
  - Final count assertion verifies Phase 1's lock fix held
files_touched:
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/ConcurrencyRegressionTests.cs
tdd: false
risk: medium
---

# PLAN-2.2: ConcurrencyRegressionTests

## Context

This is the cross-repo regression guard for Phase 1's `TaskSchedulerJobCountSync`
lock-fix. It bypasses real DNQ jobs and hammers
`IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount` directly from many threads,
asserting no deadlock and a consistent final count. Parallel-safe with PLAN-2.1
and PLAN-2.3: disjoint file, disjoint port base (55000).

**Ground rules:**

- Resolve `ITaskSchedulerJobCountSync` via the DNQ container after
  `InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)` has run
  (positional args, ISSUE-030 workaround).
- Port: `TestHelpers.NextPort(ref _portCounter)` where
  `private static int _portCounter = TestHelpers.ConcurrencyPortBase;`.
- **Deadlock detector**: the test body must complete within 30 seconds. Wrap the
  worker join in `Task.WaitAll(..., TimeSpan.FromSeconds(30))` and fail via
  `Assert.Fail("deadlock / timeout")` if any thread hangs.
- 12 worker threads, each performing N iterations of
  Increase → Decrease (or Increase → Increase → Decrease → Decrease) so that the
  final net delta is predictable (e.g., N net-zero cycles so final count == 0).

<task id="1" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/ConcurrencyRegressionTests.cs" tdd="false">
  <action>
Create `ConcurrencyRegressionTests.cs`. Structure:

1. Using directives: `System`, `System.Threading`, `System.Threading.Tasks`, `DotNetWorkQueue`, `DotNetWorkQueue.TaskScheduling`, `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`, `Microsoft.VisualStudio.TestTools.UnitTesting`.

2. `[TestClass] public class ConcurrencyRegressionTests`

3. `private static int _portCounter = TestHelpers.ConcurrencyPortBase;`

4. `[TestMethod] public void HammerIncreaseDecrease_NoDeadlock_FinalCountConsistent()`:
   - Allocate `var port = TestHelpers.NextPort(ref _portCounter);`
   - Construct a minimal DNQ container that can host `InjectDistributedTaskScheduler`. Look at how `SchedulerContainer` is used in `DotNetWorkQueue.IntegrationTests.Shared` or in the README example `using (var schedulerContainer = new SchedulerContainer(RegisterService)) { ... }` (RESEARCH.md section 5). Inside `RegisterService`: `container.InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface);`
   - Resolve `ITaskSchedulerJobCountSync` from the container via `container.GetInstance<ITaskSchedulerJobCountSync>()`. **`ITaskSchedulerJobCountSync` is PUBLIC** in the 0.4.0 NuGet (confirmed by verifier — interface lives in `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` namespace with members `GetCurrentTaskCount`, `IncreaseCurrentTaskCount`, `DecreaseCurrentTaskCount`, `Start`, and the `RemoteCountChanged` event). No pivot to `ATaskScheduler` needed.
   - **CRITICAL — call `sync.Start()` explicitly BEFORE spawning worker threads.** The 0.4.0 `IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount` methods use a `_outbound?.Enqueue(...)` null-safe guard: if `Start()` has not been called, `_outbound` is null, the enqueue silently no-ops, and the test appears to pass without actually exercising the poller path. A test that skips Start() is a false positive — it would pass even if Phase 1's lock fix were reverted. Always call `sync.Start()` before the hammering loop so the real concurrency path is exercised.
   - Spawn `const int Threads = 12;` and `const int Iterations = 5000;` (tune down if 5000 is too slow on slow machines; 1000 is the floor).
   - Each thread runs a loop: for i in 0..Iterations: call Increase, then call Decrease. Final net delta across all threads should be 0.
   - Use `Task.Run(...)` to create tasks; collect them into an array; `Task.WaitAll(tasks, TimeSpan.FromSeconds(30))` — if it returns `false`, call `Assert.Fail("Deadlock detected: 30s timeout elapsed")`.
   - After join: read `sync.GetCurrentTaskCount()` and assert it equals 0 with FluentAssertions: `sync.GetCurrentTaskCount().Should().Be(0, "all increments matched by decrements")`.

5. Clean up: call `sync.Dispose()` (or `IDisposable` dispose via the container) in `[TestCleanup]` or a `try/finally` so the poller thread exits cleanly before the next test runs. Also `using (var schedulerContainer = ...)` to dispose the container.

License header: none (Memory test convention).
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --filter "FullyQualifiedName~ConcurrencyRegressionTests" --nologo 2>&1 | tail -30</verify>
  <done>`dotnet test --filter ConcurrencyRegressionTests` exits 0 on net10.0. Test completes well under 30s (typical: < 5s). Final count asserted == 0 via FluentAssertions. No `udpBroadcastPort:` usage in the file.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/ConcurrencyRegressionTests.cs" tdd="false">
  <action>
Flakiness check + named-arg guard:

1. Grep: `grep -n "udpBroadcastPort" ...ConcurrencyRegressionTests.cs` must return zero matches.

2. Run the concurrency test 5 times in a row:
   `for i in 1 2 3 4 5; do dotnet test ...csproj --filter "FullyQualifiedName~ConcurrencyRegressionTests" --nologo || break; done`

3. If deadlock detector trips on any run, that is a hard failure — STOP and flag to the verifier. Do NOT raise the 30s timeout to work around it (that would mask a real regression of Phase 1's lock fix). A passing 30s deadlock detector is Phase 3's cross-repo regression signal.

4. Confirm the test ran on net10.0.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && (grep -n "udpBroadcastPort" Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/ConcurrencyRegressionTests.cs && echo "FAIL" && exit 1 || true) && for i in 1 2 3 4 5; do dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --filter "FullyQualifiedName~ConcurrencyRegressionTests" --nologo || { echo "FAIL run $i"; break; }; done</verify>
  <done>5 consecutive runs green. No named-argument usage. No timeout/deadlock in any run. `net10.0` exercised.</done>
</task>

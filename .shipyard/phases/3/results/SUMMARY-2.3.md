# Build Summary: Plan 2.3

## Status: complete (after rewrite during build recovery)

## Tasks Completed
- **Task 1 (author NodeDiscoveryTests.cs with 2 test methods):** complete after rewrite. First draft from the parallel-wave builder used a nonexistent `SchedulerContainer.GetInstance<>()` method (10 compile errors). Main driver rewrote the file using the `IContainer` closure pattern discovered by PLAN-2.2's builder.
- **Task 2 (flakiness loop + grep guards):** complete. 5/5 consecutive full-suite runs green (~24s per run for these two tests); `grep udpBroadcastPort` returns 0 matches; `grep '"loopback"'` returns 0 matches (both tests go through `TestHelpers.BeaconInterface`).

Committed as `a8567c38 — shipyard(phase-3): add NodeDiscoveryTests (PLAN-2.3)`.

## Files Modified
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/NodeDiscoveryTests.cs` — created, then rewritten.

## Decisions Made
- **Private `Node` helper class.** Both test methods spin up two scheduler containers sharing one port, each of which needs the capture-container-in-closure trick to resolve `ITaskSchedulerJobCountSync`. Factored this into a private `sealed class Node : IDisposable` with a `Create(int port)` static factory. Keeps the test methods short and makes the closure discipline impossible to forget.
- **`TwoNodes_SharedPort_DiscoverEachOther_RemoteCountConverges`:** uses `ManualResetEventSlim` on `nodeB.Sync.RemoteCountChanged` with a 10-second deadline. Bumps `nodeA.Sync.IncreaseCurrentTaskCount()` and waits for node B to observe the transition.
- **`NodeStop_RemoteCountDecays`:** same initial handshake, then explicitly disposes node A while node B is still running, then polls `nodeB.Sync.GetCurrentTaskCount()` every 100ms for up to 15s waiting for the count to drop below the pre-disposal snapshot. Asserts with FluentAssertions `.Should().BeLessThan(countBeforeDispose)`.
- **Disposal ordering:** second test uses explicit `nodeA.Dispose()` mid-test to exercise the decay path, with a `try/finally` guarding double-dispose. Assembly-level `[DoNotParallelize]` handles cross-test serialization so there is no port collision with the first test.
- **Both tests reuse the same port** via `TestHelpers.NextPort(ref _portCounter)` — well, each test allocates its own port from `NodeDiscoveryPortBase=60000`, so the two tests are port-disjoint from each other, and disjoint from the other test classes because of the base-port separation.

## Issues Encountered
- **First-draft API error.** The parallel-wave builder called `nodeA.GetInstance<ITaskSchedulerJobCountSync>()` on `SchedulerContainer` directly — but `SchedulerContainer` does not expose `GetInstance` (it only has `CreateTaskScheduler()` / `CreateTaskFactory()`). 10 `CS1061` errors. PLAN-2.3 builder agent correctly refused to touch files owned by other plans and reported the blocker. PLAN-2.2 builder agent separately hit the same compile error and documented the required closure pattern in `.checkpoint`. Main driver then rewrote the file using that pattern.
- **Discovery-timing nondeterminism.** UDP broadcast + event-driven receive loop has non-deterministic latency. Using a polling loop with a generous deadline (10s for discovery, 15s for decay) rather than a single-shot `Thread.Sleep` + assert. This follows the CLAUDE.md lesson about metric assertions racing with callbacks.
- **NetMQ port binding on Linux/Windows.** Handled by `TestHelpers.BeaconInterface` (empty string on Linux, `"loopback"` on Windows).

## Verification Results
- `dotnet build …Integration.Tests.csproj -c Debug` → **Build succeeded, 0 warnings, 0 errors**.
- `grep -n "udpBroadcastPort" NodeDiscoveryTests.cs` → **0 matches**.
- `grep -n '"loopback"' NodeDiscoveryTests.cs` → **0 matches**.
- `dotnet test --filter "FullyQualifiedName~NodeDiscoveryTests"` → **Passed!** 2/2, ~24s.
- Full-suite 5x flakiness loop: **5/5 green**, zero flakes.
- Acceptance criteria mapping:
  - ✅ Two `[TestMethod]`s (discovery + decay) authored
  - ✅ Uses positional args on `InjectDistributedTaskScheduler` (ISSUE-030 workaround)
  - ✅ Port base `60000` (`TestHelpers.NodeDiscoveryPortBase`)
  - ✅ `TestHelpers.BeaconInterface` (never hardcoded `"loopback"`)
  - ✅ `using` blocks guarantee container disposal even on assertion failure
  - ✅ Tests pass on net10.0

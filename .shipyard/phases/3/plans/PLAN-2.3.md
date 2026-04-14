---
phase: phase-3
plan: 2.3
wave: 2
dependencies: [1.1]
must_haves:
  - NodeDiscoveryTests.cs authored — verify multi-node discovery via UDP beacon
  - Uses positional args on InjectDistributedTaskScheduler (ISSUE-030 workaround)
  - Port base 60000 (TestHelpers.NodeDiscoveryPortBase) — disjoint from other test classes
  - Uses TestHelpers.BeaconInterface (Linux-aware: "" on Linux, "loopback" on Windows)
  - Asserts RemoteCountChanged fires and remote counts converge, then node teardown is clean
files_touched:
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/NodeDiscoveryTests.cs
tdd: false
risk: medium
---

# PLAN-2.3: NodeDiscoveryTests

## Context

Spins up 2 scheduler nodes sharing a UDP broadcast port, asserts they discover
each other, exercises Increase on node A, asserts node B's remote-count view
converges, then tears down node A cleanly and asserts node B notices the
disappearance. Parallel-safe with PLAN-2.1 and PLAN-2.2: disjoint file, disjoint
port base (60000).

**Ground rules:**

- **Beacon interface MUST be `TestHelpers.BeaconInterface`** — this is the ONLY
  test class that strictly requires non-default on Linux because it does
  multi-node peer discovery (CONTEXT-3.md §5, RESEARCH.md section 5 "Linux/WSL
  consideration"). PLAN-2.1 and PLAN-2.2 would mostly work with "loopback" on
  Linux; this one will silently hang forever if you pass "loopback" on Linux.
- Two nodes share the SAME `broadCastPort` so they form a single pool — do NOT
  allocate two different ports for node A and node B.
- Use a generous polling timeout (e.g., 10s) when waiting for
  `RemoteCountChanged` — UDP beacon discovery can take 1–3 seconds to
  establish.
- Positional args: `container.InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)`.

<task id="1" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/NodeDiscoveryTests.cs" tdd="false">
  <action>
Create `NodeDiscoveryTests.cs`:

1. Using directives: `System`, `System.Threading`, `System.Threading.Tasks`, `DotNetWorkQueue`, `DotNetWorkQueue.TaskScheduling`, `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`, `Microsoft.VisualStudio.TestTools.UnitTesting`, `FluentAssertions`.

2. `[TestClass] public class NodeDiscoveryTests`

3. `private static int _portCounter = TestHelpers.NodeDiscoveryPortBase;`

4. `[TestMethod] public void TwoNodes_SharedPort_DiscoverEachOther_RemoteCountConverges()`:
   - Allocate ONE port: `var sharedPort = TestHelpers.NextPort(ref _portCounter);`
   - Spin up node A: `new SchedulerContainer(c => c.InjectDistributedTaskScheduler(sharedPort, TestHelpers.BeaconInterface))`
   - Spin up node B: same port, same container pattern — two independent containers in the same process sharing the UDP bus.
   - Resolve `ITaskSchedulerJobCountSync` from BOTH containers via `container.GetInstance<ITaskSchedulerJobCountSync>()`. **The interface is PUBLIC in 0.4.0** (verifier confirmed — see PLAN-2.2 Task 1 update). Members: `GetCurrentTaskCount`, `IncreaseCurrentTaskCount`, `DecreaseCurrentTaskCount`, `Start`, event `RemoteCountChanged`.
   - **CRITICAL — call `syncA.Start()` and `syncB.Start()` explicitly** before any Increase/Decrease or event subscription. Without Start(), the poller is not running, no wire messages flow, and discovery never happens. This test will silently hang until the 10s timeout fires if Start() is skipped.
   - Subscribe to `RemoteCountChanged` on node B via a `ManualResetEventSlim` signal: `syncB.RemoteCountChanged += (s, e) => signal.Set();`
   - Bump node A's local count: `syncA.IncreaseCurrentTaskCount();`
   - Assert node B's `RemoteCountChanged` event fires within a 10-second wait (`signal.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("node B must discover node A's count bump")`).
   - Read node B's aggregate count: `syncB.GetCurrentTaskCount().Should().BeGreaterThanOrEqualTo(1, "node B should see node A's remote bump")`.

5. `[TestMethod] public void NodeStop_RemoteCountDecays()`:
   - Same setup as test 1: sharedPort, two nodes, both `sync.Start()`'d.
   - After both are running and have seen each other (wait ~2s post-Start for handshake), bump node A's count so node B has something to "forget".
   - Dispose node A (`syncA.Dispose()` or dispose the container which propagates).
   - Poll node B's `GetCurrentTaskCount()` with a deadline of 15 seconds; assert it eventually drops to 0 (or below the pre-dispose value) via `await Task.Delay(100)` loop. The 0.4.0 code treats a `RemovedNode` wire message as "remove the peer from `_otherProcessorCounts`", so after the decay node B's aggregate should reflect only its own local count.
   - If the wait times out, fail with a clear message — don't swallow the flake.

6. Use `using (var nodeA = new SchedulerContainer(...)) using (var nodeB = new SchedulerContainer(...)) { ... }` to guarantee cleanup even when assertions fail. Also ensure both `sync.Dispose()` calls run (or rely on container disposal propagating to the Singleton-registered sync instances — verify at implementation time).

**No inline LGPL header** (Memory test convention).
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --filter "FullyQualifiedName~NodeDiscoveryTests" --nologo 2>&1 | tail -40</verify>
  <done>Both test methods discovered and passing on both net8.0 and net10.0. Tests complete in < 30 seconds total. No `udpBroadcastPort:` in file. Both test methods exercise the SAME port (node-pool semantics).</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/NodeDiscoveryTests.cs" tdd="false">
  <action>
Flakiness + beacon guard:

1. Grep: `grep -n "udpBroadcastPort" ...NodeDiscoveryTests.cs` must return zero matches.

2. Grep: confirm the file uses `TestHelpers.BeaconInterface` (not hardcoded `"loopback"`). `grep -n '"loopback"' NodeDiscoveryTests.cs` must return zero matches (the file must go through the helper).

3. Run the node discovery tests 5 times in a row:
   `for i in 1 2 3 4 5; do dotnet test ...csproj --filter "FullyQualifiedName~NodeDiscoveryTests" --nologo || break; done`

4. If any run fails with a timeout, verify that the Linux code path is using `""` by checking `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` in `TestHelpers.cs`. Do NOT weaken the discovery assertions — if the beacon does not fire within 10s on localhost, that is a legitimate flake and must be investigated, not swallowed.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && (grep -n "udpBroadcastPort" Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/NodeDiscoveryTests.cs && echo "FAIL named arg" && exit 1 || true) && (grep -n '"loopback"' Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/NodeDiscoveryTests.cs && echo "FAIL hardcoded beacon" && exit 1 || true) && for i in 1 2 3 4 5; do dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --filter "FullyQualifiedName~NodeDiscoveryTests" --nologo || { echo "FAIL run $i"; break; }; done</verify>
  <done>5 consecutive runs green on both TFMs. No `udpBroadcastPort:` or hardcoded `"loopback"` in the file.</done>
</task>

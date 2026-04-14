---
phase: phase-3
plan: 2.1
wave: 2
dependencies: [1.1]
must_haves:
  - EndToEndSchedulingTests.cs authored, using positional args on InjectDistributedTaskScheduler (ISSUE-030 workaround)
  - Uses Memory transport and TestHelpers.BeaconInterface / EndToEndPortBase
  - Full end-to-end: producer enqueues jobs, consumer processes them via a distributed-scheduler-wired container, assertion that all jobs are consumed
  - Tests pass on both net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/EndToEndSchedulingTests.cs
tdd: false
risk: medium
---

# PLAN-2.1: EndToEndSchedulingTests

## Context

Authors the first test class: real DNQ jobs flowing through a Memory-transport
consumer wired with `InjectDistributedTaskScheduler`. Proves the consumer story
end-to-end. Parallel-safe with PLAN-2.2 and PLAN-2.3 (different files, shared
helpers are read-only in `TestHelpers.cs`).

**Ground rules** (CONTEXT-3.md §5, RESEARCH.md sections 3 & 5):

- Use **positional arguments**: `container.InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)` — do NOT use the named argument `udpBroadcastPort:` which is the upstream README bug (ISSUE-030).
- Port: allocate via `TestHelpers.NextPort(ref _portCounter)` where `_portCounter` is a `static int _portCounter = TestHelpers.EndToEndPortBase` field on the class. Each test gets a fresh port.
- Beacon interface: use `TestHelpers.BeaconInterface` (platform-aware).
- Ride on `DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer` (or closest equivalent) for the metrics-polling loop — do NOT hand-roll metric assertions (CLAUDE.md lesson: "Integration test metrics assertions can race").
- License header: **none** (Memory test convention — RESEARCH.md section 2, Option A).

<task id="1" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/EndToEndSchedulingTests.cs" tdd="false">
  <action>
Create `EndToEndSchedulingTests.cs` as a new `[TestClass]` in namespace `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests`.

Structural requirements:

1. Top of file: `using` directives for `System.Threading`, `DotNetWorkQueue`, `DotNetWorkQueue.Configuration`, `DotNetWorkQueue.IntegrationTests.Shared`, `DotNetWorkQueue.IntegrationTests.Shared.Consumer`, `DotNetWorkQueue.IntegrationTests.Shared.Producer`, `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (the NuGet package namespace — contains `TaskSchedulerSetup.InjectDistributedTaskScheduler`), `DotNetWorkQueue.Transport.Memory.Basic`, `Microsoft.VisualStudio.TestTools.UnitTesting`.

2. Static port counter: `private static int _portCounter = TestHelpers.EndToEndPortBase;`

3. `[TestMethod]` `DispatchAndConsume_AllJobsProcessed` — the faithful consumer path:
   - Allocate one port via `var port = TestHelpers.NextPort(ref _portCounter);`
   - Create `IntegrationConnectionInfo`, `GenerateQueueName.Create()`.
   - Use `DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer` (clone the Memory test's `Consumer/SimpleConsumer.cs` call site verbatim) with `MemoryMessageQueueInit`, `FakeMessage`, `MessageQueueCreation`. Pass a non-empty `registerService` callback `x => x.InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)` (positional args).
   - Use test parameters `messageCount=50, runtime=0, timeOut=180, workerCount=5` (mid-range values that exercise the scheduler without long wall-clock).
   - `Helpers.GenerateData`, `Helpers.Verify`, and `VerifyQueueCount` wiring: copy the pattern from `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Consumer/SimpleConsumer.cs` — the shared runner's `.Run<...>(...)` method already owns the metrics polling loop (CLAUDE.md lesson) so we inherit the race-free assertion for free.
   - Do NOT add `[DoNotParallelize]` at the class level — the assembly-level attribute from PLAN-1.1 task 3 already handles it.

4. Platform note: if the test runs on net8.0 and net10.0, use no conditional compilation — the public API is identical on both TFMs.

5. Inspect `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Consumer/SimpleConsumer.cs` for the exact `Helpers.GenerateData` / `Helpers.Verify` identifiers and any local `SharedClasses.cs` you must clone into the new test project. If a `SharedClasses.cs` helper is missing in the new project, copy it from the Memory test project verbatim (new file is still in scope for this task — same class file).

**Name collision warning**: the helper class `DotNetWorkQueue.IntegrationTests.Shared.Helpers` versus local `SharedClasses.cs`-defined `Helpers` may need disambiguation. If so, use the local namespace-nested name.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --filter "FullyQualifiedName~EndToEndSchedulingTests" --nologo 2>&1 | tail -30</verify>
  <done>`dotnet test --filter EndToEndSchedulingTests` exits 0. At least one test method is discovered and passes on BOTH net8.0 and net10.0 (the output shows `Passed!` lines for both TFMs). The test does not use named argument `udpBroadcastPort:` (grep the file to confirm). Run the filter loop 3 times locally to check for flakes.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/EndToEndSchedulingTests.cs" tdd="false">
  <action>
Hardening + ISSUE-030 grep guard:

1. Run a grep to confirm the file does NOT use the buggy named argument:
   `grep -n "udpBroadcastPort" Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/EndToEndSchedulingTests.cs` must return zero matches.

2. Run the single test 5 times in a row via a for-loop to check for flakiness:
   `for i in 1 2 3 4 5; do dotnet test ...csproj --filter "FullyQualifiedName~EndToEndSchedulingTests" --nologo || break; done`

3. If any run fails, adjust timeout (the `timeOut` parameter on the `.Run<>()` call) up to 240s, and re-run the loop. Do NOT relax assertions.

4. Confirm both TFMs are exercised: look for `net8.0` and `net10.0` in the test run output, or pass `-f net8.0` and `-f net10.0` separately and confirm green on each.

Keep the test class small — this task is verification only, not new code.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && (grep -n "udpBroadcastPort" Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/EndToEndSchedulingTests.cs && echo "FAIL: named arg present" && exit 1 || true) && for i in 1 2 3 4 5; do dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --filter "FullyQualifiedName~EndToEndSchedulingTests" --nologo || { echo "FAIL run $i"; break; }; done</verify>
  <done>5 consecutive runs green. Zero `udpBroadcastPort` references in the file. Both net8.0 and net10.0 results appear in the output with `Passed!`.</done>
</task>

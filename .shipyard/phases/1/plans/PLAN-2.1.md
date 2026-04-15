---
phase: taskscheduler-lock-fix
plan: 2.1
wave: 4
dependencies: [1.3]
must_haves:
  - Concurrency regression test: N producer threads hammer Increase/Decrease, final count matches expected delta
  - State consistency test for remote SetCount aggregation via GetCurrentTaskCount
  - Lifecycle test: Start -> operate -> Dispose completes within timeout
files_touched:
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncConcurrencyTests.cs
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncStateTests.cs
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncLifecycleTests.cs
tdd: true
risk: medium
---

# PLAN-2.1 — Phase 1 regression + state + lifecycle test suite

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 4 (depends on PLAN-1.3 — tests target the fully-refactored implementation)
**Risk:** MEDIUM — concurrency tests can be flaky if timeouts are too tight. Use generous deadlock-detector timeouts (30s) per CONTEXT-1.md decision #2.

## Shared conventions for every test file in this plan

- Uses `[Collection("NetMQ")]` to serialize against other NetMQ tests (RESEARCH.md 6.1, 6.3).
- Uses a `NextPort()` counter pattern. The existing `TaskSchedulerJobCountSyncTests.cs:15-16` has a `NextPort()` with a base seed in the **40000–49999** range — DO NOT reuse that range. Each new test file gets a disjoint base seed (see per-task assignments below) so parallel Linux/Docker runs with lingering `TIME_WAIT` sockets do not collide with the existing tests or each other.
  - `TaskSchedulerJobCountSyncConcurrencyTests` — base seed **50000**
  - `TaskSchedulerJobCountSyncStateTests` — base seed **55000**
  - `TaskSchedulerJobCountSyncLifecycleTests` — base seed **60000**
- Uses a `XunitLogger` adapter. **The existing `XunitLogger` at `TaskSchedulerJobCountSyncTests.cs:154-168` is NON-GENERIC** — its constructor takes `ITestOutputHelper` and the class does NOT take a type parameter. `new XunitLogger<T>(output)` will fail with CS0305. For each new test file, **copy the non-generic class declaration verbatim as a private nested class** inside the new test class. This is zero-touch to the existing file and avoids InternalsVisibleTo changes. Instantiate with `new XunitLogger(_output)` and pass it to `TaskSchedulerBus` / `TaskSchedulerJobCountSync` — both accept a non-generic `ILogger`, so no generic is needed.

  The exact declaration to copy (from `TaskSchedulerJobCountSyncTests.cs` lines 154–168):

  ```csharp
  private class XunitLogger : ILogger
  {
      private readonly ITestOutputHelper _output;

      public XunitLogger(ITestOutputHelper output) => _output = output;

      public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
      public bool IsEnabled(LogLevel logLevel) => true;

      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
      {
          try { _output.WriteLine($"[{logLevel}] {formatter(state, exception)}"); }
          catch { /* test may have ended */ }
      }
  }
  ```

  Required usings for the nested logger: `Microsoft.Extensions.Logging`, `Xunit.Abstractions`, `System`.
- Respects platform-aware beacon interface from `TaskSchedulerJobCountSyncTests.cs:19-23`: `RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback"`. Do NOT hardcode `"loopback"`.
- Uses plain `Assert.*` — no FluentAssertions, no MSTest (RESEARCH.md 6.2).
- Wraps `sync.Start()` in `Task.Run(() => sync.Start())` + `await Task.Delay(...)` per existing convention even though Start() is now non-blocking — this matches the test harness style and keeps diff noise minimal (RESEARCH.md 5.2).
- Target framework: net8.0 ONLY (the test project is single-TFM).

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncConcurrencyTests.cs" tdd="true">
  <action>
    Write the concurrency regression test per CONTEXT-1.md decision #2. Real `TaskSchedulerJobCountSync` + real `TaskSchedulerBus` on a loopback/free TCP port. N = 12 producer threads run for ~2 seconds hammering `Increase`/`Decrease`. Final count must equal `increments - decrements` within a 30-second deadlock-detector timeout. The test's purpose is to fail LOUDLY if `_lockSocket` is reintroduced or the poller wiring deadlocks.

    **Port seed for this file:** base **50000** (disjoint from the existing 40000–49999 range used by `TaskSchedulerJobCountSyncTests.cs`).

    **Logger:** copy the non-generic `XunitLogger` nested class declaration shown in the shared conventions block above. Use `new XunitLogger(_output)` — NEVER `new XunitLogger<T>(_output)`.

    TDD order:
    1. Write the test first (expect it to pass immediately if PLAN-1.2/1.3 are correct; if it hangs or fails, that's a real regression).
    2. Body (match existing `TaskSchedulerJobCountSyncTests` construction style — look at the 3-node test at lines 102–152 for the bus wiring template):

    ```csharp
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Xunit;
    using Xunit.Abstractions;

    namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests
    {
        [Collection("NetMQ")]
        public class TaskSchedulerJobCountSyncConcurrencyTests
        {
            // Port base 50000 — disjoint from existing TaskSchedulerJobCountSyncTests (40000-49999)
            // and from the sibling state/lifecycle test files (55000, 60000).
            private static int _nextPort = 50000 + System.Random.Shared.Next(0, 1000);
            private static int NextPort() => Interlocked.Increment(ref _nextPort);

            private readonly ITestOutputHelper _output;

            public TaskSchedulerJobCountSyncConcurrencyTests(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public async Task Increase_And_Decrease_Under_Contention_Final_Count_Matches_Delta()
            {
                var port = NextPort();
                var beaconInterface = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback";
                var bus = new TaskSchedulerBus(
                    new XunitLogger(_output),
                    port,
                    beaconInterface);
                using var sync = new TaskSchedulerJobCountSync(
                    bus,
                    new XunitLogger(_output));

                _ = Task.Run(() => sync.Start());
                await Task.Delay(2500);

                const int threadCount = 12;
                const int iterationsPerThread = 5000;
                var increments = 0L;
                var decrements = 0L;
                var doneBarrier = new CountdownEvent(threadCount);
                var overallDeadline = Task.Delay(TimeSpan.FromSeconds(30));

                for (var t = 0; t < threadCount; t++)
                {
                    var isIncrementer = t % 2 == 0;
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            for (var i = 0; i < iterationsPerThread; i++)
                            {
                                if (isIncrementer)
                                {
                                    sync.IncreaseCurrentTaskCount();
                                    Interlocked.Increment(ref increments);
                                }
                                else
                                {
                                    sync.DecreaseCurrentTaskCount();
                                    Interlocked.Increment(ref decrements);
                                }
                            }
                        }
                        finally
                        {
                            doneBarrier.Signal();
                        }
                    }) { IsBackground = true };
                    thread.Start();
                }

                var completed = await Task.WhenAny(Task.Run(() => doneBarrier.Wait()), overallDeadline);
                Assert.True(
                    completed != overallDeadline,
                    $"Deadlock detected: producer threads did not finish within 30s. increments={increments}, decrements={decrements}");

                // Local (Interlocked) reading — GetCurrentTaskCount includes remote dict,
                // which should be empty in a single-node test.
                var expectedDelta = Interlocked.Read(ref increments) - Interlocked.Read(ref decrements);
                Assert.Equal(expectedDelta, sync.GetCurrentTaskCount());
            }

            // Copied verbatim from TaskSchedulerJobCountSyncTests.cs:154-168. Non-generic.
            private class XunitLogger : ILogger
            {
                private readonly ITestOutputHelper _output;

                public XunitLogger(ITestOutputHelper output) => _output = output;

                public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    try { _output.WriteLine($"[{logLevel}] {formatter(state, exception)}"); }
                    catch { /* test may have ended */ }
                }
            }
        }
    }
    ```

    3. Run the test 5 times in a loop locally (`for i in 1 2 3 4 5; do dotnet test --filter ...ConcurrencyTests; done`) to shake out flakes per CONTEXT-1.md verification #3.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; for i in 1 2 3 4 5; do dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" --filter "FullyQualifiedName~TaskSchedulerJobCountSyncConcurrencyTests" || break; done</verify>
  <done>Test passes 5 consecutive times with no hangs and no deadlocks. Final count matches `increments - decrements` exactly on every run. If any single run fails or takes more than ~10s (well under the 30s deadline), investigate — intermittent failures at this layer are Phase 1 blockers. Commit message: `phase-1: add concurrency regression test for TaskSchedulerJobCountSync`</done>
</task>

<task id="2" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncStateTests.cs" tdd="true">
  <action>
    State consistency test per roadmap line 89–91. Two real nodes on the same loopback bus. Node A issues a scripted sequence of Increase/Decrease; wait for the remote SetCount broadcast to propagate to Node B; assert `B.GetCurrentTaskCount()` returns the correct aggregate (A's local + any of B's own).

    **Port seed for this file:** base **55000** (disjoint from the existing 40000–49999 range and from the other new test files at 50000 / 60000).

    **Logger:** copy the non-generic `XunitLogger` nested class declaration shown in the shared conventions block. Use `new XunitLogger(_output)` — NEVER `new XunitLogger<T>(_output)`.

    Rationale: CONTEXT-1.md decision #2 emphasizes real bus wiring over mock-based tests because the bug is a real concurrency bug. The "fake `ITaskSchedulerBus`" mentioned in roadmap line 89 is acceptable if the builder prefers — but the real-bus path more closely mirrors the concurrency test and gives stronger coverage. Builder may choose. If going with a fake, use `NSubstitute` which is already declared in the csproj (RESEARCH.md 6.2).

    TDD sequence:
    1. Write test first. Real-bus variant sketch:

    ```csharp
    // Class-level:
    // private static int _nextPort = 55000 + System.Random.Shared.Next(0, 1000);
    // private static int NextPort() => Interlocked.Increment(ref _nextPort);

    [Fact]
    public async Task RemoteSetCount_From_Node_A_Is_Aggregated_By_Node_B()
    {
        var port = NextPort();
        var beaconInterface = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback";
        var busA = new TaskSchedulerBus(new XunitLogger(_output), port, beaconInterface);
        var busB = new TaskSchedulerBus(new XunitLogger(_output), port, beaconInterface);
        var syncA = new TaskSchedulerJobCountSync(busA, new XunitLogger(_output));
        var syncB = new TaskSchedulerJobCountSync(busB, new XunitLogger(_output));
        try
        {
            _ = Task.Run(() => syncA.Start());
            _ = Task.Run(() => syncB.Start());
            await Task.Delay(3000); // beacon + handshake

            // Scripted sequence on A
            syncA.IncreaseCurrentTaskCount(); // A=1
            syncA.IncreaseCurrentTaskCount(); // A=2
            syncA.IncreaseCurrentTaskCount(); // A=3
            syncA.DecreaseCurrentTaskCount(); // A=2

            // Poll with deadline — don't fixed-sleep (RESEARCH.md 6.5)
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && syncB.GetCurrentTaskCount() != 2L)
            {
                await Task.Delay(50);
            }

            // B has no local count; aggregate = A's remote contribution = 2
            Assert.Equal(2L, syncB.GetCurrentTaskCount());
            Assert.Equal(2L, syncA.GetCurrentTaskCount());
        }
        finally
        {
            syncA.Dispose();
            syncB.Dispose();
        }
    }

    // Copied verbatim from TaskSchedulerJobCountSyncTests.cs:154-168. Non-generic.
    private class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;

        public XunitLogger(ITestOutputHelper output) => _output = output;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try { _output.WriteLine($"[{logLevel}] {formatter(state, exception)}"); }
            catch { /* test may have ended */ }
        }
    }
    ```

    2. Run test; if it fails, investigate whether the poller is draining the outbound queue correctly (PLAN-1.2 Task 3) and whether `OnActorReady`'s `SetCount` handler is reaching `_otherProcessorCounts` (PLAN-1.2 Task 2).
    3. If using the NSubstitute fake variant instead: substitute an `ITaskSchedulerBus` whose `Start()` returns a test-owned `NetMQActor.Create(...)` shim that lets the test feed arbitrary `Publish/SetCount/<port>/<value>` frames. Slightly more surgical but more fragile; real-bus is recommended.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" --filter "FullyQualifiedName~TaskSchedulerJobCountSyncStateTests"</verify>
  <done>Test passes. `syncB.GetCurrentTaskCount()` converges to `2L` within the 15s polling deadline on every run. Commit message: `phase-1: add state consistency test for remote SetCount aggregation`</done>
</task>

<task id="3" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncLifecycleTests.cs" tdd="true">
  <action>
    Lifecycle test: `Start()` returns, some work happens, `Dispose()` completes within a bounded timeout. This test catches any regression of the `Dispose` hot-wait (PLAN-1.3 Task 2) or the `Thread.Join(timeout)` sequencing.

    **Port seed for this file:** base **60000** (disjoint from the existing 40000–49999 range and from the other new test files at 50000 / 55000).

    **Logger:** copy the non-generic `XunitLogger` nested class declaration shown in the shared conventions block. Use `new XunitLogger(_output)` — NEVER `new XunitLogger<T>(_output)`.

    TDD sequence:
    1. Write test first:

    ```csharp
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Xunit;
    using Xunit.Abstractions;

    namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests
    {
        [Collection("NetMQ")]
        public class TaskSchedulerJobCountSyncLifecycleTests
        {
            // Port base 60000 — disjoint from existing TaskSchedulerJobCountSyncTests (40000-49999)
            // and from the sibling concurrency/state test files (50000, 55000).
            private static int _nextPort = 60000 + System.Random.Shared.Next(0, 1000);
            private static int NextPort() => Interlocked.Increment(ref _nextPort);

            private readonly ITestOutputHelper _output;
            public TaskSchedulerJobCountSyncLifecycleTests(ITestOutputHelper output) { _output = output; }

            [Fact]
            public async Task Start_Operate_Dispose_Completes_Within_Timeout()
            {
                var port = NextPort();
                var beaconInterface = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback";
                var bus = new TaskSchedulerBus(
                    new XunitLogger(_output), port, beaconInterface);
                var sync = new TaskSchedulerJobCountSync(
                    bus, new XunitLogger(_output));

                _ = Task.Run(() => sync.Start());
                await Task.Delay(2500);

                // Operate
                Assert.Equal(1L, sync.IncreaseCurrentTaskCount());
                Assert.Equal(2L, sync.IncreaseCurrentTaskCount());
                Assert.Equal(1L, sync.DecreaseCurrentTaskCount());
                Assert.Equal(1L, sync.GetCurrentTaskCount());

                // Dispose must not hang.
                var disposeTask = Task.Run(() => sync.Dispose());
                var deadline = Task.Delay(TimeSpan.FromSeconds(10));
                var completed = await Task.WhenAny(disposeTask, deadline);
                Assert.True(
                    completed == disposeTask,
                    "Dispose() did not complete within 10s — likely hot-wait regression or poller thread not exiting");
                await disposeTask; // surface any exceptions
            }

            // Copied verbatim from TaskSchedulerJobCountSyncTests.cs:154-168. Non-generic.
            private class XunitLogger : ILogger
            {
                private readonly ITestOutputHelper _output;

                public XunitLogger(ITestOutputHelper output) => _output = output;

                public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    try { _output.WriteLine($"[{logLevel}] {formatter(state, exception)}"); }
                    catch { /* test may have ended */ }
                }
            }
        }
    }
    ```

    2. Run test. If Dispose hangs, investigate the `Thread.Join(TimeSpan.FromSeconds(5))` from PLAN-1.3 Task 2 and whether `_poller.Stop()` is reaching the poller thread.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" --filter "FullyQualifiedName~TaskSchedulerJobCountSyncLifecycleTests" &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Release -p:CI=true</verify>
  <done>Lifecycle test passes. Full test suite (original 3 tests + SetCountMsg + probe + 3 new) is green. `dotnet build -c Release -p:CI=true` is clean (0 errors, 0 warnings — hits Phase 1 success criterion #5). `grep -n _lockSocket Source/TaskSchedulerJobCountSync.cs` returns ZERO hits (Phase 1 success criterion #1). Commit message: `phase-1: add lifecycle test for Start/Dispose timing`</done>
</task>
</content>
</invoke>
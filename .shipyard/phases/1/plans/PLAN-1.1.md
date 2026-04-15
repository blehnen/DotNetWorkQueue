---
phase: taskscheduler-lock-fix
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Verify NetMQQueue<T> API surface against NetMQ 4.0.2.2 before committing to the refactor
  - Introduce SetCountMsg record struct with a round-trip unit test
files_touched:
  - Source/TaskSchedulerJobCountSync.cs
  - Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/SetCountMsgTests.cs
tdd: true
risk: high
---

# PLAN-1.1 — NetMQ API probe and SetCountMsg scaffolding

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 1 (foundational — no dependencies)
**Risk:** HIGH — if `NetMQQueue<T>` is not available in NetMQ 4.0.2.2 the downstream plans must pivot to an inproc PairSocket fallback.

This plan proves the NetMQ 4.0.2.2 API surface needed by the entire refactor and introduces the locked-in `SetCountMsg` type (CONTEXT-1.md decision #1) in isolation — zero production call sites, zero behavioral change. If Task 1 uncovers a missing API, STOP and update `.shipyard/phases/1/CONTEXT-1.md` before continuing.

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/NetMqQueueApiProbeTests.cs" tdd="false">
  <action>
    Create a throwaway probe test that verifies NetMQQueue&lt;T&gt; + NetMQPoller + ReceiveReady actually compile and run against NetMQ 4.0.2.2. This is a compile-time gate: if it fails, STOP and update CONTEXT-1.md decision #1 to use an inproc PairSocket pair instead. Create file `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/NetMqQueueApiProbeTests.cs`:

    ```csharp
    using System;
    using System.Threading;
    using NetMQ;
    using Xunit;

    namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests
    {
        [Collection("NetMQ")]
        public class NetMqQueueApiProbeTests
        {
            [Fact]
            public void NetMqQueue_WithPoller_ReceivesEnqueuedItem()
            {
                using var queue = new NetMQQueue<int>();
                var received = 0;
                var signal = new ManualResetEventSlim();

                queue.ReceiveReady += (_, args) =>
                {
                    received = args.Queue.Dequeue();
                    signal.Set();
                };

                using var poller = new NetMQPoller { queue };
                poller.RunAsync();

                queue.Enqueue(42);

                Assert.True(signal.Wait(TimeSpan.FromSeconds(5)), "Queue ReceiveReady never fired");
                Assert.Equal(42, received);

                poller.Stop();
            }
        }
    }
    ```

    Do NOT add a license header — existing test files in this repo have none (RESEARCH.md section 6.7). Use block-scoped namespace — file-scoped is not house style (6.8).
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" --filter "FullyQualifiedName~NetMqQueueApiProbeTests.NetMqQueue_WithPoller_ReceivesEnqueuedItem"</verify>
  <done>Test passes. Build has zero errors (TreatWarningsAsErrors is on — any warning is a failure). If this task fails because `NetMQQueue&lt;T&gt;` does not exist, HALT the plan and update `/mnt/f/git/dotnetworkqueue/.shipyard/phases/1/CONTEXT-1.md` decision #1 to use an inproc PairSocket-pair fallback before continuing. Commit message: `phase-1: probe NetMQQueue<T> API against NetMQ 4.0.2.2`</done>
</task>

<task id="2" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs, /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/SetCountMsgTests.cs" tdd="true">
  <action>
    Introduce the `SetCountMsg` locked-in type from CONTEXT-1.md decision #1. It MUST be `internal readonly record struct SetCountMsg(int Port, long Count)`. Place it at the bottom of `Source/TaskSchedulerJobCountSync.cs`, INSIDE the same `namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` block but OUTSIDE the `TaskSchedulerJobCountSync` class. No consumers yet — do not touch `IncreaseCurrentTaskCount`, `DecreaseCurrentTaskCount`, `ProcessMessages`, `_lockSocket`, or any other method body.

    TDD order:
    1. Write the test file `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/SetCountMsgTests.cs` FIRST:

    ```csharp
    using System;
    using System.Threading;
    using NetMQ;
    using Xunit;

    namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests
    {
        [Collection("NetMQ")]
        public class SetCountMsgTests
        {
            [Fact]
            public void SetCountMsg_Equality_IsValueBased()
            {
                var a = new SetCountMsg(5000, 42L);
                var b = new SetCountMsg(5000, 42L);
                var c = new SetCountMsg(5000, 43L);
                Assert.Equal(a, b);
                Assert.NotEqual(a, c);
            }

            [Fact]
            public void SetCountMsg_RoundTripsThroughNetMqQueue()
            {
                using var queue = new NetMQQueue<SetCountMsg>();
                SetCountMsg received = default;
                var signal = new ManualResetEventSlim();

                queue.ReceiveReady += (_, args) =>
                {
                    received = args.Queue.Dequeue();
                    signal.Set();
                };

                using var poller = new NetMQPoller { queue };
                poller.RunAsync();

                var sent = new SetCountMsg(12345, 7L);
                queue.Enqueue(sent);

                Assert.True(signal.Wait(TimeSpan.FromSeconds(5)), "SetCountMsg never surfaced on the poller");
                Assert.Equal(sent, received);

                poller.Stop();
            }
        }
    }
    ```

    2. Run the tests — expect both to fail with CS0246 (type not found).
    3. Add to `Source/TaskSchedulerJobCountSync.cs`, after the closing brace of `TaskSchedulerJobCountSync` but before the closing brace of the namespace:

    ```csharp
        /// &lt;summary&gt;
        /// Outbound message placed on the NetMQQueue&lt;SetCountMsg&gt; by
        /// IncreaseCurrentTaskCount / DecreaseCurrentTaskCount; drained on the
        /// poller thread and translated into a Publish/SetCount wire frame.
        /// &lt;/summary&gt;
        internal readonly record struct SetCountMsg(int Port, long Count);
    ```

    4. Re-run the tests — both pass. Note: InternalsVisibleTo to the Tests assembly is already wired (csproj line 32), so the struct is visible. No XML doc warnings because `NoWarn CS1591` is set in the main csproj.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" --filter "FullyQualifiedName~SetCountMsgTests"</verify>
  <done>Both `SetCountMsg_Equality_IsValueBased` and `SetCountMsg_RoundTripsThroughNetMqQueue` pass. `dotnet build -c Release -p:CI=true` of the main csproj still succeeds with 0 errors, 0 warnings. Grep for `_lockSocket` in `TaskSchedulerJobCountSync.cs` still shows the 9 pre-existing occurrences (no production code touched yet). Commit message: `phase-1: introduce internal SetCountMsg record struct with round-trip test`</done>
</task>

---
phase: taskscheduler-lock-fix
plan: 1.3
wave: 3
dependencies: [1.2]
must_haves:
  - Start() returns quickly after launching the poller on a dedicated thread (CONTEXT-1.md decision #3)
  - Dispose() joins the poller thread with a bounded timeout; no while(_running) Sleep hot-wait
  - TaskSchedulerMultiple.Start() drops its redundant Task.Run wrapper now that _jobCount.Start() is non-blocking
files_touched:
  - Source/TaskSchedulerJobCountSync.cs
  - Source/TaskSchedulerMultiple.cs
tdd: false
risk: medium
---

# PLAN-1.3 — Async-friendly Start and Dispose cleanup

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 3 (depends on PLAN-1.2 — poller and `NetMQQueue<SetCountMsg>` must already exist)
**Risk:** MEDIUM — thread lifecycle refactor inside proven scaffolding. Existing tests act as regression gate.

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs" tdd="false">
  <action>
    Refactor `Start()` to launch the poller on a dedicated background `Thread` and return once the startup handshake completes (CONTEXT-1.md decision #3). Preserve the 1100ms beacon sleep.

    **Initial broadcast routing — see CONTEXT-1.md decision #5 (LOCKED):** The initial `BroadCast` / `AddedNode` publish is sent DIRECTLY via `_actor.SendMoreFrame(TaskSchedulerBusCommands.Publish.ToString()).SendMoreFrame(TaskSchedulerBusCommands.AddedNode.ToString()).SendFrame(_hostAddress)` on the caller thread, exactly once, BEFORE the poller thread is spawned and assumes ownership of `_actor`. Do NOT route the initial broadcast through `NetMQQueue<SetCountMsg>` — that queue is exclusively for `Increase`/`Decrease` traffic (it is typed to `SetCountMsg`, which has no broadcast variant). `_lockSocket` still disappears entirely; this direct-send resolution does not resurrect it. This supersedes the earlier PROJECT.md language about "Start publishes the initial broadcast through the queue path."

    1. Add a field next to `_poller`:
       ```csharp
       private Thread _pollerThread;
       ```
    2. Restructure `Start()`:
       - Phase A (caller thread): `_actor = _bus.Start();` — unchanged. Do the `GetHostAddress` send + `ReceiveFrameString` round-trip on the caller thread (RESEARCH.md 3.3 — this is inherently synchronous and the poller hasn't started yet, so cross-thread actor use is still safe here). Parse `_hostPort`. `Thread.Sleep(1100)` for the beacon grace period.
       - Phase B (caller thread): Construct `_outbound` and the initial broadcast (direct `_actor.SendMoreFrame(...).SendFrame(_hostAddress);`) BEFORE handing `_actor` to the poller. Per CONTEXT-1.md #5 this is a direct send, NOT a queue enqueue. This is the current Task 3 behavior unchanged.
       - Phase C (spawn): Create and start the dedicated poller thread. The thread body wires `ReceiveReady` handlers, constructs `_poller = new NetMQPoller { _actor, _outbound };`, then calls `_poller.Run()` (blocking) until `_poller.Stop()` is called from Dispose. Wrap the body in a try/catch that logs fatal errors (preserving the existing behavior at lines 164–168 of the pre-refactor file).
       - After spawning the thread, Start() returns immediately.

       Sketch:
       ```csharp
       public void Start()
       {
           _actor = _bus.Start();
           _actor.SendFrame(TaskSchedulerBusCommands.GetHostAddress.ToString());
           _hostAddress = _actor.ReceiveFrameString();
           _hostPort = new Uri("http://" + _hostAddress).Port;

           Thread.Sleep(1100); // beacon grace — kept per CONTEXT-1.md decision #3

           _outbound = new NetMQQueue<SetCountMsg>();

           // CONTEXT-1.md #5: initial broadcast goes DIRECT, not via _outbound.
           _actor.SendMoreFrame(TaskSchedulerBusCommands.Publish.ToString())
               .SendMoreFrame(TaskSchedulerBusCommands.AddedNode.ToString())
               .SendFrame(_hostAddress);

           _pollerThread = new Thread(RunPoller)
           {
               IsBackground = true,
               Name = "TaskSchedulerJobCountSync.Poller"
           };
           _pollerThread.Start();
       }

       private void RunPoller()
       {
           try
           {
               _actor.ReceiveReady += OnActorReady;
               _outbound.ReceiveReady += OnOutboundReady;
               _poller = new NetMQPoller { _actor, _outbound };
               _poller.Run();
           }
           catch (Exception ex)
           {
               _log.LogError(ex, "TaskSchedulerJobCountSync poller thread terminated");
           }
       }
       ```
       (Use whatever `_log` API the file currently uses — `_log.LogError(ex, ...)` or equivalent. Inspect existing error-logging call in the pre-refactor `Start()` catch block and match it.)
    3. Existing single-caller `TaskSchedulerMultiple.cs:55–63` continues to work: `Task.Run(() => _jobCount.Start())` now just runs a non-blocking Start and completes instantly. No change required in this task — Task 2 below removes the now-redundant wrapper.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Debug &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj"</verify>
  <done>Build clean (0/0). All existing tests pass — they already use `Task.Run(() => sync.Start())` + `await Task.Delay(...)` so the semantic change is transparent (RESEARCH.md 5.2). `_pollerThread` field exists; `RunPoller` method exists; `Start()` returns after spawning the thread, not after `_poller.Run()` completes. Commit message: `phase-1: move TaskSchedulerJobCountSync poller onto dedicated thread; Start() is non-blocking`</done>
</task>

<task id="2" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs, /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerMultiple.cs" tdd="false">
  <action>
    Clean up `Dispose()` and update `TaskSchedulerMultiple.Start()`:

    1. In `TaskSchedulerJobCountSync.Dispose(bool disposing)`:
       - Remove the old `while (_running) Thread.Sleep(100);` hot-wait if any residue remains (should already be gone after PLAN-1.2 Task 2 but double-check).
       - Sequence:
         ```csharp
         try
         {
             _poller?.Stop();
             _pollerThread?.Join(TimeSpan.FromSeconds(5));
             _outbound?.Dispose();
             _actor?.Dispose();
             _poller?.Dispose();
         }
         catch (SocketException error)
         {
             if (error.ErrorCode == 10035 || error.ErrorCode == 10054)
             {
                 return;
             }
             throw;
         }
         ```
       - Preserve the SocketException swallow for 10035/10054 (RESEARCH.md 2.2). Bounded Join timeout (5 seconds) — if the poller thread doesn't exit in time, log and proceed with disposal rather than hang. (Add a log line if the Join returns false.)
    2. In `Source/TaskSchedulerMultiple.cs`, lines 55–63, `public override void Start()`:
       - Replace:
         ```csharp
         Task.Run(() => { _jobCount.Start(); });
         ```
         with a direct call:
         ```csharp
         _jobCount.Start();
         ```
       - Rationale: `_jobCount.Start()` is now non-blocking (PLAN-1.3 Task 1), so the `Task.Run` wrapper is dead weight. This is the only production caller (RESEARCH.md 5.1), and removing it is in-scope cleanup per the suggested decomposition.
    3. Grep confirms: no `while` loops that reference `_running` or similar hot-waits remain in `TaskSchedulerJobCountSync.cs`.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Debug &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Release -p:CI=true &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj"</verify>
  <done>Both Debug and Release (`-p:CI=true`) builds clean — 0 errors, 0 warnings. All existing tests pass. `grep -n "while" Source/TaskSchedulerJobCountSync.cs` shows no `_running` hot-wait. `TaskSchedulerMultiple.cs` no longer wraps `_jobCount.Start()` in `Task.Run`. The SocketException swallow for codes 10035/10054 is still present. Commit message: `phase-1: bounded Dispose() join + drop redundant Task.Run wrapper in TaskSchedulerMultiple`</done>
</task>
</content>
</invoke>
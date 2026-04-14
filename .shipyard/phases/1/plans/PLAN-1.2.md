---
phase: taskscheduler-lock-fix
plan: 1.2
wave: 2
dependencies: [1.1]
must_haves:
  - Introduce NetMQPoller lifecycle without regressing existing behavior
  - Migrate _actor.ReceiveReady handling onto the poller
  - Replace _lockSocket + direct SendFrame sequences with NetMQQueue<SetCountMsg> enqueues; delete _lockSocket field
files_touched:
  - Source/TaskSchedulerJobCountSync.cs
tdd: false
risk: high
---

# PLAN-1.2 — Poller infrastructure refactor

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 2 (depends on PLAN-1.1 — `SetCountMsg` must exist before Task 3 consumes it)
**Risk:** HIGH — all three tasks touch `TaskSchedulerJobCountSync.cs`, the production code path. They MUST be sequenced (same file). The existing `TaskSchedulerJobCountSyncTests.cs` suite is the regression gate for every task in this plan.

This plan is the hot path for the lock-contention fix. Each task is one logical refactor step + one commit. After each task the existing test suite MUST remain green — no regressions are tolerated.

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs" tdd="false">
  <action>
    Add a `NetMQPoller` field + start/stop lifecycle skeleton. The poller is constructed empty; `_actor` still feeds the existing `ProcessMessages()` loop under `_lockSocket`. No behavioral change yet — this task is pure scaffolding so Tasks 2 and 3 have a place to stand.

    Steps:
    1. Add `using NetMQ;` if not already imported (it already is — `TaskSchedulerBus.cs` imports it, and so does this file).
    2. Add field (near line 44, next to `_actor`):
       ```csharp
       private NetMQPoller _poller;
       ```
    3. In `Start()`, after `_actor = _bus.Start();` (line 128) and the `GetHostAddress` round-trip completes (around line 140), but BEFORE the beacon sleep and initial broadcast, construct the poller:
       ```csharp
       _poller = new NetMQPoller();
       ```
       Do NOT add `_actor` to the poller in this task — that happens in Task 2. Do NOT call `_poller.RunAsync()` yet. Keep the existing `while(!_stopRequested) ProcessMessages();` loop exactly as it is.
    4. In `Dispose(bool disposing)`, before `_actor?.Dispose()` (around line 253), add:
       ```csharp
       _poller?.Dispose();
       ```
       Inside the existing `lock(_lockSocket)` / try-catch — do not restructure the Dispose body yet.
    5. Do NOT delete `_lockSocket`, `_stopRequested`, `_running`, or `ProcessMessages` in this task.
    6. Build + run the full existing test suite. All three pre-existing tests must still pass.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Debug &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj"</verify>
  <done>Build: 0 errors, 0 warnings (TreatWarningsAsErrors is on). All pre-existing xUnit tests in `TaskSchedulerJobCountSyncTests` pass (at least 3 facts), plus the two `SetCountMsgTests` and the `NetMqQueueApiProbeTests`. Grep `_poller` in `TaskSchedulerJobCountSync.cs` returns hits; `_lockSocket` still returns its original 9 occurrences. Commit message: `phase-1: scaffold NetMQPoller field and lifecycle in TaskSchedulerJobCountSync`</done>
</task>

<task id="2" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs" tdd="false">
  <action>
    Move `_actor` message handling onto the poller. Delete `ProcessMessages()`, `_stopRequested`, `_running`, and the `while(!_stopRequested)` loop in `Start()`. All inbound handling now happens inside a new `OnActorReady` callback subscribed to `_actor.ReceiveReady` and driven by `_poller.Run()`. Outbound sends in `Increase`/`Decrease` STILL use direct `_actor.SendMoreFrame/SendFrame` under `_lockSocket` in this task — the `NetMQQueue<SetCountMsg>` wiring is Task 3.

    Concretely:
    1. Add new method `private void OnActorReady(object sender, NetMQSocketEventArgs e)`. Body is the existing `ProcessMessages()` body MINUS the `lock (_lockSocket)` wrapper and MINUS the `if (_stopRequested) return;` and the `TryReceiveFrameString(TimeSpan.FromMilliseconds(10), ...)` early return. Because this is a `ReceiveReady` callback, a frame IS available — use `_actor.ReceiveFrameString(Encoding.ASCII)` (blocking read of the first frame). Preserve the `int.TryParse` / `long.TryParse` guards on the `SetCount` handler (RESEARCH.md 2.2).
    2. In `Start()`:
       - After the `GetHostAddress` round-trip (lines 134–140) and the 1100ms beacon sleep (line 142), wire the poller:
         ```csharp
         _actor.ReceiveReady += OnActorReady;
         _poller = new NetMQPoller { _actor };
         ```
       - Perform the initial broadcast (the code currently at lines 145–150) inline BEFORE starting the poller — `_actor.SendMoreFrame(...).SendFrame(_hostAddress);` — since at this point the caller thread still owns `_actor`.
       - Replace the `while(!_stopRequested) ProcessMessages();` loop (lines 153–170) with `_poller.Run();` — this blocks until `_poller.Stop()` is called from `Dispose`.
       - Keep the outer try/catch that catches the fatal-error logger (lines 164–168) so a dying poller thread doesn't crash the host.
    3. In `Dispose(bool disposing)`:
       - BEFORE the try-catch around `_actor?.Dispose()`, call `_poller?.Stop()` (if it's running). `NetMQPoller.Stop()` is thread-safe per RESEARCH.md 7.4.
       - Keep the `SocketException` swallow for error codes 10035/10054 exactly as it is (RESEARCH.md 2.2).
       - Keep the `while(_running) Sleep(100)` hot-wait in place for now — it becomes a no-op once `_running` is deleted, but untangling it is PLAN-1.3 Task 2. Actually since `_running` is being deleted in this task, **delete** the hot-wait in this task too (it references `_running`).
    4. Delete fields: `_stopRequested`, `_running` (lines 36–37). Delete method: `ProcessMessages()` (lines 175–233).
    5. In `IncreaseCurrentTaskCount`, `DecreaseCurrentTaskCount`, and the `Dispose` teardown — LEAVE `_lockSocket` alone. Those sends still run on the caller thread into `_actor` via `SendMoreFrame/SendFrame` while the poller owns `_actor` for reads. This is intentionally a transitional state: Task 3 replaces the direct sends with queue-enqueues and removes `_lockSocket`.

       CRITICAL NOTE on thread safety for this intermediate state: `NetMQActor.SendFrame` across threads is generally unsafe once the poller owns the actor. This intermediate state relies on the `_lockSocket` mutex serializing writes AND on the actor's internal PairSocket tolerating the cross-thread write for this brief window. If it deadlocks or throws, COLLAPSE Tasks 2 and 3 into a single commit (document the decision in CONTEXT-1.md) — do not try to "fix" the intermediate state.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Debug &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj"</verify>
  <done>Build: 0 errors, 0 warnings. All 5+ existing tests (3 original + 2 SetCountMsg + 1 probe) pass. `ProcessMessages`, `_stopRequested`, and `_running` no longer appear in the file (grep: zero hits). `OnActorReady` method exists. `_poller.Run()` is called in `Start()`. `_lockSocket` still appears (exact count drops to ~5 after removing `ProcessMessages` and the lines inside `Start()` that guarded `_actor` reads). Commit message: `phase-1: move actor inbound handling onto NetMQPoller`. If this task deadlocks under tests, abort, roll back, and collapse into Task 3 as a single atomic commit.</done>
</task>

<task id="3" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs" tdd="false">
  <action>
    Route outbound SetCount messages through `NetMQQueue&lt;SetCountMsg&gt;` and delete `_lockSocket` entirely. After this task, every socket touch on `_actor` happens on the poller thread — `Increase`/`Decrease` only increment counters and enqueue.

    Steps:
    1. Add field near `_poller`:
       ```csharp
       private NetMQQueue<SetCountMsg> _outbound;
       ```
    2. In `Start()`, construct the queue BEFORE the poller and add to poller collection-initializer:
       ```csharp
       _outbound = new NetMQQueue<SetCountMsg>();
       _outbound.ReceiveReady += OnOutboundReady;
       _actor.ReceiveReady += OnActorReady;
       _poller = new NetMQPoller { _actor, _outbound };
       ```
    3. Add `OnOutboundReady`:
       ```csharp
       private void OnOutboundReady(object sender, NetMQQueueEventArgs<SetCountMsg> e)
       {
           while (e.Queue.TryDequeue(out var msg, TimeSpan.Zero))
           {
               _actor.SendMoreFrame(TaskSchedulerBusCommands.Publish.ToString())
                   .SendMoreFrame(TaskSchedulerBusCommands.SetCount.ToString())
                   .SendMoreFrame(msg.Port.ToString())
                   .SendFrame(msg.Count.ToString());
           }
       }
       ```
       (Use `TryDequeue` per RESEARCH.md 7.2. If `TryDequeue(out,TimeSpan)` does not exist on NetMQ 4.0.2.2, fall back to `while (e.Queue.Count > 0) { var m = e.Queue.Dequeue(); ... }`.)
    4. Rewrite `IncreaseCurrentTaskCount`:
       ```csharp
       public long IncreaseCurrentTaskCount()
       {
           var newValue = Interlocked.Increment(ref _currentTaskCount);
           _outbound?.Enqueue(new SetCountMsg(_hostPort, newValue));
           return newValue;
       }
       ```
       No `lock (_lockSocket)`. The return value is still post-increment (RESEARCH.md 2.5).
    5. Rewrite `DecreaseCurrentTaskCount` symmetrically using `Interlocked.Decrement`.
    6. Rewrite `GetCurrentTaskCount`:
       ```csharp
       public long GetCurrentTaskCount()
       {
           return Interlocked.Read(ref _currentTaskCount) + _otherProcessorCounts.Values.Sum();
       }
       ```
       No lock. (`ConcurrentDictionary` + `Interlocked.Read` per CONTEXT-1.md and RESEARCH.md 2.1.)
    7. Dispose: add `_outbound?.Dispose()` AFTER `_poller.Stop()` returns but BEFORE `_actor?.Dispose()`. Per RESEARCH.md 3.2, the `_outbound` queue must be disposed on the thread that owned the poller — since PLAN-1.3 moves poller ownership to a dedicated thread, this task (which still runs the poller on the caller thread of `Start()`) can dispose from the caller. Keep it simple for now; PLAN-1.3 will rearrange.
    8. DELETE the `_lockSocket` field and every remaining `lock (_lockSocket)` block in the file. Grep to confirm zero hits.
    9. The Dispose-guard `lock (_lockSocket) { _actor?.Dispose(); }` at line 251–254 becomes just `_actor?.Dispose();` inside the existing try-catch for `SocketException` 10035/10054.
  </action>
  <verify>cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler &amp;&amp; dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Debug &amp;&amp; dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj"</verify>
  <done>Build: 0 errors, 0 warnings. All existing tests pass. `grep -n _lockSocket Source/TaskSchedulerJobCountSync.cs` returns ZERO hits (CONTEXT-1.md success criterion #1 and verification #4 — this is the headline deliverable of the phase). `Increase`/`Decrease` bodies contain no `lock`. `GetCurrentTaskCount` contains no `lock`. `_outbound` is constructed + disposed. Also confirm `dotnet build -c Release -p:CI=true` is clean. Commit message: `phase-1: route SetCount through NetMQQueue<SetCountMsg>; remove _lockSocket`</done>
</task>

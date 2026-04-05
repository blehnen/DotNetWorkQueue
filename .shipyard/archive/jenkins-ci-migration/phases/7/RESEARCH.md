# Phase 7 Research: Replace Manual Threads with Task-Based Patterns

## Current State (Post-Phase 6)

Phase 6 has been completed. `Thread.Abort()`, `IAbortWorkerThread`, `AbortWorkerThreadsWhenStopping`, and all `ThreadAbortException` catches have been removed. `StopThread` is now a thin wrapper around `WaitForThreadToFinish`.

## File-by-File Analysis

### WorkerBase.cs (lines 29-154)
- **Line 35**: `protected Thread WorkerThread;` -- the field that must become `protected Task WorkerTask`
- **Line 130**: `_workerTerminate.AttemptToTerminate(WorkerThread, TimeSpan.Zero)` -- passes Thread to WorkerTerminate
- No direct Thread creation here; subclasses assign `WorkerThread`

### PrimaryWorker.cs (lines 29-150)
- **Line 75**: `if (WorkerThread != null) return;` -- guard against double-start
- **Line 83**: `WorkerThread = new Thread(MainLoop) { Name = _nameFactory.Create() };` -- the Thread creation
- **Line 84**: `WorkerThread.Start();` -- thread start
- **Line 86**: `_log.LogDebug($"{WorkerThread.Name} created");` -- uses Thread.Name for logging
- **Line 98-100**: `if (WorkerThread != null)` / `_log.LogDebug($"Stopping worker thread {WorkerThread.Name}");` -- uses Thread.Name in stop

### Worker.cs (lines 28-122)
- **Line 71**: `if (WorkerThread != null) return;` -- guard against double-start
- **Line 77**: `WorkerThread = new Thread(MainLoop) { Name = _nameFactory.Create() };` -- the Thread creation
- **Line 78**: `WorkerThread.Start();` -- thread start
- **Line 80**: `_log.LogDebug($"{WorkerThread.Name} created");` -- uses Thread.Name
- **Line 92-95**: `if (WorkerThread == null) return;` / `_log.LogDebug($"Stopping worker thread {WorkerThread.Name}");` -- uses Thread.Name in stop

### MultiWorkerBase.cs (lines 28-93)
- **Line 62**: `public override bool Running => WorkerThread != null && WorkerThread.IsAlive || ...` -- checks Thread.IsAlive
- **Line 73**: `if (WorkerThread == null || !WorkerThread.IsAlive) return;` -- checks Thread.IsAlive
- **Line 75**: `StopThread.TryForceTerminate(WorkerThread);` -- passes Thread to StopThread

### BaseMonitor.cs (lines 30-290)
- **Lines 170-173**: `while (Running) { Thread.Sleep(20); }` in `Cancel()` -- spin-wait
- The monitor uses a `System.Threading.Timer` for scheduling (line 98), and `_running` bool with lock for state (lines 206-237)
- `RunMonitor()` sets `Running = false` in finally block (line 136)
- This is the ONLY spin-wait in BaseMonitor; replacing with `ManualResetEventSlim` is straightforward

### WaitForThreadToFinish.cs (lines 31-75)
- **Line 49**: `public bool Wait(Thread workerThread, TimeSpan? timeout = null)` -- takes Thread parameter
- **Line 58**: `while (workerThread != null && workerThread.IsAlive)` -- polls Thread.IsAlive
- **Line 64**: `Thread.Sleep(20);` -- 20ms poll interval
- Must change signature to accept `Task` and use `Task.Wait(timeout)` instead of polling

### StopThread.cs (lines 27-52)
- **Line 46**: `public bool TryForceTerminate(Thread workerThread)` -- takes Thread parameter
- **Line 48**: `_waitForThreadToFinish.Wait(workerThread);` -- delegates to WaitForThreadToFinish
- Must change signature to accept `Task`

### WorkerTerminate.cs (lines 27-48)
- **Line 35**: `public bool AttemptToTerminate(Thread workerThread, TimeSpan? timeout)` -- takes Thread parameter
- **Line 37**: `if (workerThread == null || !workerThread.IsAlive)` -- checks Thread.IsAlive
- **Line 42**: `return workerThread.Join(timeout.Value);` -- uses Thread.Join
- **Line 44**: `workerThread.Join();` -- uses Thread.Join
- Must change to use `Task.Wait()` / `Task.IsCompleted`

### DI Registration (ComponentRegistration.cs)
- Line 230: `container.Register<StopThread>(LifeStyles.Singleton);`
- Line 265: `container.Register<WaitForThreadToFinish>(LifeStyles.Singleton);`
- Line 266: `container.Register<WorkerTerminate>(LifeStyles.Singleton);`
- These are concrete type registrations; class renames would require updating these lines

## Existing Tests

### WaitForThreadToFinishTests.cs
- 3 tests: `Wait`, `Wait_Long`, `Wait_With_Timeout`
- All create real `Thread` objects with `Thread.Sleep` workloads and verify timing
- Must be rewritten to use `Task.Run` with `Task.Delay` workloads

### StopWorkerTests.cs
- Tests `StopWorker` (a different class), NOT `StopThread`
- No existing tests for `StopThread` or `WorkerTerminate`

## Key Design Decisions

1. **Task.Run vs Task.Factory.StartNew**: Use `Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning)` since worker loops are long-running and should get their own thread from the pool rather than occupying a ThreadPool thread. `Task.Run` does NOT support `TaskCreationOptions.LongRunning`.

2. **Worker Name for Logging**: `Task` has no `.Name` property. Store the worker name in a separate `string _workerName` field, assigned from `_nameFactory.Create()` before task start. Use it in log messages.

3. **Running Property**: Replace `WorkerThread.IsAlive` with `WorkerTask != null && !WorkerTask.IsCompleted` in MultiWorkerBase.

4. **WaitForThreadToFinish Replacement**: The `Thread.IsAlive` polling loop with `Thread.Sleep(20)` can be replaced with `Task.Wait(timeout)` which blocks until completion or timeout. This eliminates the busy-wait entirely.

5. **WorkerTerminate Replacement**: `Thread.Join(timeout)` becomes `Task.Wait(timeout)`. `Thread.Join()` (no timeout) becomes `Task.Wait()`.

6. **BaseMonitor.Cancel() Spin-Wait**: Add `ManualResetEventSlim _monitorCompleted` field. Set it in `RunMonitor()` finally block. In `Cancel()`, replace `while(Running) { Thread.Sleep(20); }` with `_monitorCompleted.Wait(TimeSpan.FromSeconds(30))`. Reset it at start of `RunMonitor()`. Dispose in `Dispose(bool)`.

7. **No interface changes needed**: `IWorkerBase` does not expose `Thread` anywhere -- it only has `Running`, `Start`, `Stop`, `TryForceTerminate`, `AttemptToTerminate`. All return `bool` or `void`. The Thread-to-Task migration is entirely internal.

## Dependency Graph

```
WorkerBase.WorkerThread field
  <- PrimaryWorker.Start() assigns it
  <- Worker.Start() assigns it
  <- MultiWorkerBase.Running reads it
  <- MultiWorkerBase.TryForceTerminate() reads it, passes to StopThread
  <- WorkerBase.AttemptToTerminate() passes to WorkerTerminate

StopThread.TryForceTerminate(Thread)
  <- MultiWorkerBase.TryForceTerminate()

WaitForThreadToFinish.Wait(Thread)
  <- StopThread.TryForceTerminate()

WorkerTerminate.AttemptToTerminate(Thread)
  <- WorkerBase.AttemptToTerminate()

BaseMonitor.Cancel() -- independent, no connection to worker Thread usage
```

## Wave Analysis

**Wave 1** (no inter-dependencies):
- Plan 1.1: WorkerBase/PrimaryWorker/Worker/MultiWorkerBase -- replace Thread field with Task, adapt all usages. Also adapt WorkerTerminate and StopThread and WaitForThreadToFinish since they all share the Thread parameter chain.
- Plan 1.2: BaseMonitor.Cancel() -- completely independent from worker thread changes

**Revised Assessment**: The worker Thread chain (WorkerBase -> PrimaryWorker/Worker -> MultiWorkerBase -> StopThread -> WaitForThreadToFinish -> WorkerTerminate) is ONE connected dependency graph. All these changes must happen together for the code to compile. BaseMonitor is the only truly independent change.

However, since all these classes share file dependencies via the Thread type, splitting the worker chain across plans would leave the code in a non-compiling state between plans. Therefore:

- **Plan 1.1** (Wave 1): BaseMonitor spin-wait replacement -- independent, verifiable alone
- **Plan 1.2** (Wave 1): Replace Thread with Task in the entire worker chain (WorkerBase + PrimaryWorker + Worker + MultiWorkerBase + WorkerTerminate + StopThread + WaitForThreadToFinish) + update tests

These two plans can execute in parallel since they touch completely different code paths.

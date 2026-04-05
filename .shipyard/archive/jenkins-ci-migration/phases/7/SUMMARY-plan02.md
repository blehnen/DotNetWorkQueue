# Phase 7 Plan 02: Replace Thread with Task in Worker Infrastructure

## Status: COMPLETE

## What Was Done

### Task 1: Replace Thread with Task in Worker Classes (4 files)
- **WorkerBase.cs**: Replaced `protected Thread WorkerThread` with `protected Task WorkerTask` and added `protected string WorkerName`. Updated `AttemptToTerminate` call to pass `WorkerTask`.
- **PrimaryWorker.cs**: Replaced `new Thread(MainLoop)` with `Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning)`. Worker name stored separately in `WorkerName` field. All `WorkerThread` null checks changed to `WorkerTask`.
- **Worker.cs**: Same pattern as PrimaryWorker -- Task.Factory.StartNew with LongRunning, WorkerName field, updated null checks and log messages.
- **MultiWorkerBase.cs**: `Running` property changed from `WorkerThread.IsAlive` to `!WorkerTask.IsCompleted`. `TryForceTerminate` passes `WorkerTask` instead of `WorkerThread` to `StopThread`.

### Task 2: Adapt Termination Helpers (3 files)
- **WorkerTerminate.cs**: Method signature changed from `Thread workerThread` to `Task workerTask`. `Thread.Join` replaced with `Task.Wait`. `Thread.IsAlive` replaced with `Task.IsCompleted`.
- **StopThread.cs**: Method signature changed from `Thread workerThread` to `Task workerTask`. Delegates to `WaitForThreadToFinish.Wait(workerTask)`.
- **WaitForThreadToFinish.cs**: Replaced polling loop (Thread.Sleep + Stopwatch) with direct `Task.Wait()` calls. AggregateException caught for faulted/canceled tasks. Removed `System.Diagnostics` using (Stopwatch no longer needed). Kept `ILogger` constructor parameter to preserve DI compatibility.

### Task 3: Update Tests (1 file)
- **WaitForThreadToFinishTests.cs**: Replaced `new Thread(RunMe)` with `Task.Factory.StartNew(() => Task.Delay(N).Wait(), TaskCreationOptions.LongRunning)`. Added `Wait_Null_Task_Returns_True` and `Wait_Completed_Task_Returns_True` tests. Replaced `Assert.IsInRange` with `Assert.IsTrue` for clearer MSTest assertions. Removed `RunMe`/`RunMeLong` helper methods.

## Verification Results
- `grep "new Thread(" Source/DotNetWorkQueue/Queue/*.cs` -- **0 hits** (all replaced)
- `grep "Thread.IsAlive" Source/DotNetWorkQueue/Queue/*.cs` -- **0 hits** (all replaced)
- `dotnet build Source/DotNetWorkQueue.sln -c Debug` -- **Build succeeded, 0 warnings, 0 errors**
- `dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` -- **875 passed, 0 failed, 0 skipped**

## Deviations
- **WaitForThreadToFinish._log field retained**: The `ILogger` field and constructor parameter were kept despite the new implementation not logging. Removing them would break SimpleInjector DI registrations, which is an architectural change outside the plan scope.
- **Assert.IsInRange still available**: The plan noted `Assert.IsInRange` may not exist in MSTest, but it does compile in this project. Tests were updated to use `Assert.IsTrue` anyway for explicit range assertions as specified.

## Commits
1. `c867bfa5` - `refactor(queue): replace Thread with Task in worker infrastructure`
2. `b67e035d` - `refactor(queue): adapt termination helpers from Thread to Task`
3. `278ae0e1` - `test(queue): update WaitForThreadToFinishTests for Task-based API`

## Files Modified
| File | Change |
|------|--------|
| `Source/DotNetWorkQueue/Queue/WorkerBase.cs` | Thread -> Task field, added WorkerName |
| `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` | Task.Factory.StartNew, WorkerName |
| `Source/DotNetWorkQueue/Queue/Worker.cs` | Task.Factory.StartNew, WorkerName |
| `Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs` | IsCompleted checks, Task parameter |
| `Source/DotNetWorkQueue/Queue/WorkerTerminate.cs` | Task.Wait instead of Thread.Join |
| `Source/DotNetWorkQueue/Queue/StopThread.cs` | Task parameter |
| `Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` | Direct Task.Wait, no polling loop |
| `Source/DotNetWorkQueue.Tests/Queue/WaitForThreadToFinishTests.cs` | Task-based test workloads |

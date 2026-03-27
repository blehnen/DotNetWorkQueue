# Roadmap: Thread Management Modernization

## Overview

Remove the unsafe `Thread.Abort()` pattern and modernize thread management in the core queue worker infrastructure. The codebase already uses `CancellationToken` for cooperative cancellation throughout -- `Thread.Abort()` was a legacy fallback. Manual `new Thread()` usage is replaced with `Task.Run` with `TaskCreationOptions.LongRunning`, and `Thread.Sleep` spin-waits are replaced with proper signaling.

**Prerequisite**: PR #82 (Security & Stability Fixes) must be merged before code changes begin.

---

## Phase Summary

| Phase | Name | Complexity | Dependencies | Plans | Risk |
|-------|------|-----------|-------------|-------|------|
| 6 | Remove Thread.Abort | Medium | PR #82 merged | 2 | Low -- abort path already no-op on modern .NET |
| 7 | Replace Manual Threads | Medium | Phase 6 | 2 | Medium -- thread lifecycle changes affect shutdown |

---

## Phase 6: Remove Thread.Abort (M-1)

**Complexity**: Medium
**Dependencies**: PR #82 merged to `master`
**Risk**: Low. The `Thread.Abort()` call is behind `#if NETFULL` and only fires when `AbortWorkerThreadsWhenStopping` is `true` (defaults to `false`). Removing it is a simplification, not a behavior change for most users. The only risk is to .NET Framework 4.8 users who explicitly enabled abort -- they get a log warning instead of a forced abort.

### Items

1. **Gut `AbortWorkerThread` implementation**: Replace the `#if NETFULL` abort call with a no-op that logs a warning if the thread is still alive. The class stays (for DI compatibility) but `Abort()` always returns `false`.
2. **Remove `AbortWorkerThreadsWhenStopping` from configuration**: Remove the property from `IWorkerConfiguration` and `WorkerConfiguration`. Update `TimeToWaitForWorkersToCancel` XML docs to remove references to abort behavior.
3. **Remove `ThreadAbortException` catch blocks**: Remove the 5 `catch (ThreadAbortException)` blocks across `HeartBeatWorker.cs`, `MessageProcessing.cs`, `MessageProcessingAsync.cs`, `ProcessMessage.cs`, `ProcessMessageAsync.cs`. The general `catch (Exception)` blocks already handle any exception type adequately.
4. **Remove `AbortWorkerThreadDecorator`**: Delete the decorator class file and remove its registration from `ComponentRegistration.cs` (line 410).
5. **Simplify `StopThread.TryForceTerminate`**: Since `_abortWorkerThread.Abort()` now always returns `false`, simplify to call `_waitForThreadToFinish.Wait()` directly. Evaluate whether `IAbortWorkerThread` and its DI wiring can be collapsed into a no-op or removed entirely.
6. **Update tests**: Update `AbortWorkerThreadTests.cs`, `StopWorkerTests.cs`, and `WorkerConfigurationTests.cs` to reflect the removed configuration and changed behavior.

### Key Files

| File | Change |
|------|--------|
| `Source/DotNetWorkQueue/Queue/AbortWorkerThread.cs` | Gut implementation to no-op with log warning |
| `Source/DotNetWorkQueue/IAbortWorkerThread.cs` | Keep interface (DI compatibility), update XML docs |
| `Source/DotNetWorkQueue/IWorkerConfiguration.cs` | Remove `AbortWorkerThreadsWhenStopping` property |
| `Source/DotNetWorkQueue/Configuration/WorkerConfiguration.cs` | Remove `_abortWorkerThreadsWhenStopping` field and property |
| `Source/DotNetWorkQueue/Queue/StopThread.cs` | Simplify `TryForceTerminate` |
| `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` | Remove `ThreadAbortException` catch (lines 221-233) |
| `Source/DotNetWorkQueue/Queue/MessageProcessing.cs` | Remove `ThreadAbortException` catch (line 162) |
| `Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs` | Remove `ThreadAbortException` catch (line 170) |
| `Source/DotNetWorkQueue/Queue/ProcessMessage.cs` | Remove `ThreadAbortException` catch (line 81) |
| `Source/DotNetWorkQueue/Queue/ProcessMessageAsync.cs` | Remove `ThreadAbortException` catch (line 82) |
| `Source/DotNetWorkQueue/Logging/Decorator/IAbortWorkerThreadDecorator.cs` | Delete file |
| `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` | Remove decorator registration (line 410), keep base registration (line 230) |
| `Source/DotNetWorkQueue.Tests/Queue/AbortWorkerThreadTests.cs` | Update tests for new no-op behavior |
| `Source/DotNetWorkQueue.Tests/Queue/StopWorkerTests.cs` | Update tests |
| `Source/DotNetWorkQueue.Tests/Configuration/WorkerConfigurationTests.cs` | Remove `AbortWorkerThreadsWhenStopping` tests |

### Success Criteria

1. Zero `Thread.Abort()` calls in the codebase: `grep -r "\.Abort()" Source/DotNetWorkQueue/Queue/ --include="*.cs"` returns no hits
2. Zero `ThreadAbortException` catch blocks: `grep -r "ThreadAbortException" Source/DotNetWorkQueue/ --include="*.cs"` returns no hits
3. No `AbortWorkerThreadsWhenStopping` property: `grep -r "AbortWorkerThreadsWhenStopping" Source/ --include="*.cs"` returns no hits
4. No `#if NETFULL` blocks related to thread abort remain in Queue files
5. `AbortWorkerThreadDecorator` file deleted
6. All unit tests pass: `dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj`
7. Solution builds cleanly: `dotnet build Source/DotNetWorkQueue.sln -c Debug`
8. In-memory integration tests pass: `dotnet test Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj`

---

## Phase 7: Replace Manual Threads (M-2)

**Complexity**: Medium
**Dependencies**: Phase 6 complete (abort removal simplifies thread replacement -- no need to track `Thread` objects for abort)
**Risk**: Medium. Changing the thread lifecycle model for workers affects shutdown behavior, diagnostics, and the `Running` property check in `MultiWorkerBase` (which currently checks `WorkerThread.IsAlive`). The `WaitForThreadToFinish` class also depends on `Thread.IsAlive`. These must be adapted to work with `Task` completion state.

### Items

1. **Replace `new Thread(MainLoop)` with `Task.Run` in `PrimaryWorker` and `Worker`**: Use `Task.Run(() => MainLoop(), TaskCreationOptions.LongRunning)`. Store the `Task` instead of `Thread`. Update `WorkerBase` to hold a `Task` field instead of `Thread`. Add worker-name-equivalent logging context (e.g., log the worker name at loop start since `Task` has no `.Name` property).
2. **Adapt `MultiWorkerBase.Running` property**: Currently checks `WorkerThread.IsAlive`. Replace with `WorkerTask != null && !WorkerTask.IsCompleted` check. Also adapt `TryForceTerminate` to use `Task`-based waiting.
3. **Adapt `WorkerTerminate` and `WaitForThreadToFinish`**: Replace `Thread.Join()` / `Thread.IsAlive` polling with `Task.Wait(timeout)`. The `WaitForThreadToFinish.Wait()` method's `Thread.Sleep(20)` busy-wait loop becomes a simple `Task.Wait(timeout)` call. Consider renaming `WaitForThreadToFinish` to `WaitForTaskToFinish` or making it generic.
4. **Adapt `StopThread.TryForceTerminate`**: After Phase 6 gutted the abort path, this now just waits. Adapt to accept `Task` instead of `Thread`, use `Task.Wait(timeout)` with a configurable timeout, log a warning if the task does not complete within the timeout. Consider renaming class to `StopWorker`.
5. **Replace `Thread.Sleep(20)` spin-wait in `BaseMonitor.Cancel()`**: Add a `ManualResetEventSlim` field. Signal it when `RunMonitor()` completes (in the `finally` block). In `Cancel()`, replace the `while(Running) { Thread.Sleep(20); }` loop with `_monitorCompleted.Wait(timeout)`. Dispose the event in `Dispose(bool)`.
6. **Update tests**: Update `WorkerTests.cs`, `StopWorkerTests.cs`, and any tests that mock or reference `Thread` objects to use `Task`-based equivalents.

### Key Files

| File | Change |
|------|--------|
| `Source/DotNetWorkQueue/Queue/WorkerBase.cs` | Replace `protected Thread WorkerThread` with `protected Task WorkerTask` |
| `Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs` | Update `Running` property to check `WorkerTask.IsCompleted`, update `TryForceTerminate` and `Dispose` |
| `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` | Replace `new Thread(MainLoop)` with `Task.Run(() => MainLoop(), ...)`, remove `.Name` assignment |
| `Source/DotNetWorkQueue/Queue/Worker.cs` | Same replacement as PrimaryWorker |
| `Source/DotNetWorkQueue/Queue/BaseMonitor.cs` | Add `ManualResetEventSlim`, signal on `RunMonitor` completion, wait in `Cancel()` with timeout |
| `Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` | Adapt to use `Task.Wait(timeout)` instead of `Thread.IsAlive` polling loop |
| `Source/DotNetWorkQueue/Queue/WorkerTerminate.cs` | Adapt to use `Task.Wait(timeout)` instead of `Thread.Join()` |
| `Source/DotNetWorkQueue/Queue/StopThread.cs` | Adapt to accept `Task` parameter instead of `Thread` |
| `Source/DotNetWorkQueue.Tests/Queue/WorkerTests.cs` | Update for Task-based workers |
| `Source/DotNetWorkQueue.Tests/Queue/StopWorkerTests.cs` | Update for Task-based termination |

### Success Criteria

1. Zero `new Thread(` in worker classes: `grep -r "new Thread(" Source/DotNetWorkQueue/Queue/ --include="*.cs"` returns no hits
2. Zero `Thread.Sleep` in `BaseMonitor.Cancel()`: the `Cancel()` method uses `ManualResetEventSlim.Wait()` instead
3. `PrimaryWorker` and `Worker` use `Task.Run` with `TaskCreationOptions.LongRunning`
4. `MultiWorkerBase.Running` checks task completion state instead of `Thread.IsAlive`
5. `WaitForThreadToFinish` uses `Task.Wait()` instead of `Thread.IsAlive` polling with `Thread.Sleep(20)`
6. All unit tests pass: `dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj`
7. Solution builds cleanly across all target frameworks: `dotnet build Source/DotNetWorkQueue.sln -c Debug`
8. In-memory integration tests pass (verifies end-to-end worker lifecycle): `dotnet test Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj`
9. Shutdown is clean -- no hung tasks after consumer queue disposal

---

## Parallelism Notes

- **Phase 6 and Phase 7 are strictly sequential.** Phase 7 depends on Phase 6 because:
  - Removing `Thread.Abort()` first eliminates the need to preserve `Thread` object references for abort, simplifying the `Thread`-to-`Task` migration.
  - `StopThread` is simplified in Phase 6, then further adapted in Phase 7.
  - `ThreadAbortException` catch blocks removed in Phase 6 would otherwise need to be carried through the Phase 7 refactor.

- **Within Phase 6**, plans are structured as two waves:
  - Wave 1: Remove configuration property + gut `AbortWorkerThread` + delete decorator (no file dependencies between these)
  - Wave 2: Remove `ThreadAbortException` catches + simplify `StopThread` + update tests (depends on Wave 1 interface/config changes)

- **Within Phase 7**, plans are structured as two waves:
  - Wave 1: `BaseMonitor` signaling change (independent of worker thread changes) + adapt `WorkerBase`/`MultiWorkerBase` to hold `Task`
  - Wave 2: `PrimaryWorker` + `Worker` migration + `WaitForThreadToFinish`/`WorkerTerminate`/`StopThread` adaptation + tests (depends on Wave 1 base class changes)

## Breaking Changes

- **Phase 6**: Removing `AbortWorkerThreadsWhenStopping` from `IWorkerConfiguration` is a **binary-breaking and source-breaking change**. Any code that reads or writes this property will fail to compile. Must be documented in CHANGELOG.
- **Phase 7**: Changing `StopThread.TryForceTerminate` and `WaitForThreadToFinish.Wait` signatures from `Thread` to `Task` parameters is an **internal breaking change**. These are public classes but unlikely to be used directly by consumers (they are wired via DI). Lower risk than Phase 6 but still document.

## Risk Assessment

- **Phase 6** is low implementation risk. The `Thread.Abort()` path is already inactive on all modern .NET targets (net8.0, net10.0, netstandard2.0). The `AbortWorkerThreadsWhenStopping` config property defaults to `false`, so removing it changes nothing for users who did not opt in. The only users affected are those running .NET Framework 4.8 who explicitly set the property to `true` -- they lose forced termination but gain stability.
- **Phase 7** carries moderate implementation risk because it changes the fundamental threading model of the worker infrastructure. The `Running` property, `TryForceTerminate`, and `WaitForThreadToFinish` all need coordinated changes. Integration testing with the Memory transport is essential to verify that consumer startup, message processing, idle optimization, and clean shutdown all work correctly under the new model.

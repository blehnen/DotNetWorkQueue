# Project: Thread Management Modernization

## Description

Remove the unsafe `Thread.Abort()` pattern and modernize thread management in the core queue worker infrastructure. The codebase already uses `CancellationToken` for cooperative cancellation throughout — `Thread.Abort()` was a legacy "stop right now" fallback that can corrupt shared state and leave database connections open. Manual `new Thread()` usage should be replaced with `Task.Run` with `TaskCreationOptions.LongRunning`, and the `Thread.Sleep(20)` spin-wait in `BaseMonitor.Cancel()` should be replaced with proper signaling.

These changes apply to all target frameworks (net10.0, net8.0, net48, netstandard2.0). PR #82 (Security & Stability Fixes) must be merged before code changes begin, but planning can proceed now.

## Goals

1. Remove `Thread.Abort()` and the `AbortWorkerThreadsWhenStopping` configuration option entirely — rely on existing cancellation tokens for cooperative shutdown
2. Replace `new Thread(MainLoop)` with `Task.Run(() => MainLoop(), TaskCreationOptions.LongRunning)` in worker classes
3. Replace `Thread.Sleep(20)` spin-wait in `BaseMonitor.Cancel()` with `ManualResetEventSlim` signaling

## Non-Goals

- Adding new cancellation token plumbing (already exists)
- Changing the worker loop logic itself (just the thread/task creation)
- Modifying transport-specific code (this is core library only)
- Addressing other CONCERNS.md items (M-3 through M-11)

## Requirements

### Remove Thread.Abort (M-1)
- Delete `AbortWorkerThread.cs` or gut its implementation to use cooperative cancellation only
- Remove `#if NETFULL` blocks in `StopThread.cs` that call `Thread.Abort()`
- Remove `ThreadAbortException` catch blocks in `HeartBeatWorker.cs`
- Remove the `AbortWorkerThreadsWhenStopping` configuration property
- Remove any DI registrations that wire up abort-based thread stopping
- If a worker thread doesn't respond to cancellation within a timeout, log a warning and move on

### Replace Manual Threads (M-2)
- `PrimaryWorker.cs`: Replace `new Thread(MainLoop)` with `Task.Run(() => MainLoop(), TaskCreationOptions.LongRunning)`
- `Worker.cs`: Same replacement
- `BaseMonitor.Cancel()`: Replace `Thread.Sleep(20)` busy-wait with `ManualResetEventSlim` that the monitor signals when it completes
- Ensure thread naming/identification still works for diagnostics (Task doesn't have `.Name` but can use Activity or logging context)

## Non-Functional Requirements

- **Backward compatibility**: Removing `AbortWorkerThreadsWhenStopping` is a breaking change — document in CHANGELOG
- **Testing**: All existing unit tests must pass. Worker shutdown behavior must be verified.
- **Performance**: `ManualResetEventSlim` should improve shutdown responsiveness vs `Thread.Sleep(20)`
- **Multi-target**: Changes apply to all 4 targets (net10.0, net8.0, net48, netstandard2.0)
- **Constraint**: Do not begin code changes until PR #82 is merged

## Success Criteria

1. No `Thread.Abort()` calls anywhere in the codebase
2. No `AbortWorkerThreadsWhenStopping` property or configuration
3. No `#if NETFULL` blocks related to thread abort
4. No `new Thread()` in PrimaryWorker or Worker
5. No `Thread.Sleep` spin-waits in BaseMonitor.Cancel()
6. All existing unit tests pass
7. Shutdown behavior is clean — cancellation token triggers cooperative stop, no forced abort

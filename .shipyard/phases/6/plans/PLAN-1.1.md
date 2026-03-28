---
phase: remove-thread-abort
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Delete AbortWorkerThread class and IAbortWorkerThread interface
  - Delete AbortWorkerThreadDecorator
  - Delete AbortWorkerThreadTests
  - Remove all DI registrations for IAbortWorkerThread
  - Simplify StopThread to remove IAbortWorkerThread dependency
files_touched:
  - Source/DotNetWorkQueue/IAbortWorkerThread.cs (DELETE)
  - Source/DotNetWorkQueue/Queue/AbortWorkerThread.cs (DELETE)
  - Source/DotNetWorkQueue/Logging/Decorator/IAbortWorkerThreadDecorator.cs (DELETE)
  - Source/DotNetWorkQueue.Tests/Queue/AbortWorkerThreadTests.cs (DELETE)
  - Source/DotNetWorkQueue/IoC/ComponentRegistration.cs
  - Source/DotNetWorkQueue/Queue/StopThread.cs
tdd: false
---

# Plan 1.1: Remove AbortWorkerThread Infrastructure

## Goal

Eliminate the `IAbortWorkerThread` interface, its implementation (`AbortWorkerThread`), its logging decorator (`AbortWorkerThreadDecorator`), all DI registrations, and the test class. Simplify `StopThread` to call `WaitForThreadToFinish.Wait()` directly, removing its dependency on `IAbortWorkerThread`.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/IAbortWorkerThread.cs, Source/DotNetWorkQueue/Queue/AbortWorkerThread.cs, Source/DotNetWorkQueue/Logging/Decorator/IAbortWorkerThreadDecorator.cs, Source/DotNetWorkQueue.Tests/Queue/AbortWorkerThreadTests.cs" tdd="false">
  <action>Delete these 4 files entirely:
  1. `Source/DotNetWorkQueue/IAbortWorkerThread.cs` -- the interface
  2. `Source/DotNetWorkQueue/Queue/AbortWorkerThread.cs` -- the implementation
  3. `Source/DotNetWorkQueue/Logging/Decorator/IAbortWorkerThreadDecorator.cs` -- the logging decorator
  4. `Source/DotNetWorkQueue.Tests/Queue/AbortWorkerThreadTests.cs` -- tests for the deleted class</action>
  <verify>bash -c "for f in 'Source/DotNetWorkQueue/IAbortWorkerThread.cs' 'Source/DotNetWorkQueue/Queue/AbortWorkerThread.cs' 'Source/DotNetWorkQueue/Logging/Decorator/IAbortWorkerThreadDecorator.cs' 'Source/DotNetWorkQueue.Tests/Queue/AbortWorkerThreadTests.cs'; do test ! -f \"$f\" && echo \"DELETED: $f\" || echo \"STILL EXISTS: $f\"; done"</verify>
  <done>All 4 files no longer exist on disk.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue/IoC/ComponentRegistration.cs" tdd="false">
  <action>Remove two DI registration lines from `ComponentRegistration.cs`:
  1. Line 230: `container.Register<IAbortWorkerThread, AbortWorkerThread>(LifeStyles.Singleton);` -- delete this entire line.
  2. Line 410: `container.RegisterDecorator<IAbortWorkerThread, Logging.Decorator.AbortWorkerThreadDecorator>(LifeStyles.Singleton);` -- delete this entire line.
  Also remove any `using` statements that become unused after these deletions (check for `using DotNetWorkQueue.Queue` -- likely still needed for other registrations).</action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -n "IAbortWorkerThread\|AbortWorkerThread" Source/DotNetWorkQueue/IoC/ComponentRegistration.cs; echo "EXIT: $?"</verify>
  <done>No lines in ComponentRegistration.cs reference IAbortWorkerThread or AbortWorkerThread. The grep returns no output and exits cleanly.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue/Queue/StopThread.cs" tdd="false">
  <action>Rewrite `StopThread` to remove its dependency on `IAbortWorkerThread`:

  1. Remove the `_abortWorkerThread` field (line 29).
  2. Remove the `IAbortWorkerThread abortWorkerThread` constructor parameter and its `Guard.NotNull` call (lines 37, 40, 43).
  3. Update the constructor XML doc to remove the `abortWorkerThread` param tag (line 35).
  4. Simplify `TryForceTerminate()` to just call `_waitForThreadToFinish.Wait(workerThread); return true;` -- remove the `if (_abortWorkerThread.Abort(workerThread)) return true;` line (line 54).
  5. Update the class-level XML doc (line 25) from "Stops a thread by aborting it if configured to do; otherwise it will wait (forever if needed) until the thread dies." to "Waits for a worker thread to finish its current work before returning."
  6. Update the method-level XML doc (line 48) similarly.
  7. Remove the `using` for any namespace that is no longer needed (the `IAbortWorkerThread` interface is in the root `DotNetWorkQueue` namespace, which is likely still needed for `WaitForThreadToFinish`).

  The final class should have a single constructor parameter (`WaitForThreadToFinish waitForThreadToFinish`) and `TryForceTerminate` should be a two-line method.</action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -n "IAbortWorkerThread\|abortWorkerThread\|\.Abort(" Source/DotNetWorkQueue/Queue/StopThread.cs; echo "EXIT: $?" && dotnet build Source/DotNetWorkQueue/DotNetWorkQueue.csproj -c Debug --no-restore 2>&1 | tail -5</verify>
  <done>StopThread.cs has no references to IAbortWorkerThread or Abort. The project builds cleanly with zero errors.</done>
</task>

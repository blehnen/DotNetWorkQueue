---
phase: replace-manual-threads
plan: 01
wave: 1
dependencies: []
must_haves:
  - Replace Thread.Sleep(20) spin-wait in BaseMonitor.Cancel() with ManualResetEventSlim signaling
  - Signal completion when RunMonitor() finishes
  - Dispose ManualResetEventSlim in Dispose(bool)
  - Add timeout to prevent infinite hang during shutdown
files_touched:
  - Source/DotNetWorkQueue/Queue/BaseMonitor.cs
tdd: false
---

# Plan 01: Replace BaseMonitor Spin-Wait with ManualResetEventSlim

## Rationale

`BaseMonitor.Cancel()` (line 170-173) uses `while (Running) { Thread.Sleep(20); }` to wait for the current monitor action to finish. This is a CPU-wasteful spin-wait with 20ms granularity. A `ManualResetEventSlim` provides instant notification when `RunMonitor()` completes, improving shutdown responsiveness and eliminating wasted CPU cycles.

This change is completely independent from the worker Thread-to-Task migration (Plan 02) because BaseMonitor does not use `Thread` objects for its own execution -- it uses `System.Threading.Timer`.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/Queue/BaseMonitor.cs" tdd="false">
  <action>
  In BaseMonitor.cs, make these specific changes:

  1. Add field (after line 45, near other fields):
     `private readonly ManualResetEventSlim _monitorCompleted = new ManualResetEventSlim(true);`
     Initialize as signaled (true) because the monitor is not running at construction time.

  2. In RunMonitor() method, add at the START of the try block (after line 121 `Running = true;`):
     `_monitorCompleted.Reset();`
     This marks the monitor as "in progress".

  3. In RunMonitor() finally block (after line 136 `Running = false;`):
     `_monitorCompleted.Set();`
     This signals that the monitor action has completed.

  4. In Cancel() method, replace lines 170-173:
     ```csharp
     //wait for the current process to finish. It should be respecting the cancel
     //token, so it should not take long.
     while (Running)
     {
         Thread.Sleep(20);
     }
     ```
     With:
     ```csharp
     //wait for the current process to finish. It should be respecting the cancel
     //token, so it should not take long. Timeout after 30 seconds as a safety net.
     _monitorCompleted.Wait(TimeSpan.FromSeconds(30));
     ```

  5. In Dispose(bool) method, add before `_timer?.Dispose();` (line 277):
     `_monitorCompleted?.Dispose();`

  6. Remove `using System.Threading;` ONLY if Thread is no longer referenced. Check: `Monitor.Enter` and `Monitor.Exit` are in `System.Threading`, `Timer` is in `System.Threading`, `Interlocked` is in `System.Threading`, `CancellationTokenSource` is in `System.Threading`. So the using statement MUST remain.
  </action>
  <verify>cd F:\Git\DotNetWorkQueue && dotnet build Source/DotNetWorkQueue/DotNetWorkQueue.csproj -c Debug --no-restore 2>&1 | tail -5</verify>
  <done>
  - BaseMonitor.cs compiles without errors across all target frameworks
  - The Cancel() method uses `_monitorCompleted.Wait(TimeSpan.FromSeconds(30))` instead of `while (Running) { Thread.Sleep(20); }`
  - `grep -n "Thread.Sleep" Source/DotNetWorkQueue/Queue/BaseMonitor.cs` returns no hits
  - The ManualResetEventSlim is Reset() at the start of RunMonitor and Set() in the finally block
  - The ManualResetEventSlim is disposed in Dispose(bool)
  </done>
</task>

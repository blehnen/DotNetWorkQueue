---
phase: replace-manual-threads
plan: 02
wave: 1
dependencies: []
must_haves:
  - Replace protected Thread WorkerThread with protected Task WorkerTask in WorkerBase
  - Replace new Thread(MainLoop) with Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning) in PrimaryWorker and Worker
  - Store worker name in a string field since Task has no .Name property
  - Adapt MultiWorkerBase.Running to check Task.IsCompleted instead of Thread.IsAlive
  - Adapt MultiWorkerBase.TryForceTerminate to pass Task to StopThread
  - Adapt WorkerTerminate.AttemptToTerminate to use Task.Wait instead of Thread.Join
  - Adapt StopThread.TryForceTerminate to accept Task instead of Thread
  - Adapt WaitForThreadToFinish.Wait to use Task.Wait instead of Thread.IsAlive polling
  - Update WaitForThreadToFinishTests to use Task-based workloads
files_touched:
  - Source/DotNetWorkQueue/Queue/WorkerBase.cs
  - Source/DotNetWorkQueue/Queue/PrimaryWorker.cs
  - Source/DotNetWorkQueue/Queue/Worker.cs
  - Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs
  - Source/DotNetWorkQueue/Queue/WorkerTerminate.cs
  - Source/DotNetWorkQueue/Queue/StopThread.cs
  - Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs
  - Source/DotNetWorkQueue.Tests/Queue/WaitForThreadToFinishTests.cs
tdd: false
---

# Plan 02: Replace Thread with Task in Worker Infrastructure

## Rationale

The worker classes (PrimaryWorker, Worker) create `new Thread(MainLoop)` to run their processing loops. This is a legacy pattern. `Task.Factory.StartNew` with `TaskCreationOptions.LongRunning` achieves the same result (a dedicated thread for the long-running loop) while integrating with the TPL. The entire Thread dependency chain -- WorkerBase field, MultiWorkerBase checks, StopThread, WaitForThreadToFinish, WorkerTerminate -- must be migrated together because they share the `Thread` type through method parameters and field references.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/Queue/WorkerBase.cs, Source/DotNetWorkQueue/Queue/PrimaryWorker.cs, Source/DotNetWorkQueue/Queue/Worker.cs, Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs" tdd="false">
  <action>
  **WorkerBase.cs** changes:
  1. Add `using System.Threading.Tasks;` to usings (keep `using System.Threading;` for Monitor, Interlocked).
  2. Line 35: Replace `protected Thread WorkerThread;` with `protected Task WorkerTask;`
  3. Add new field on the next line: `protected string WorkerName;`
  4. Line 130: Replace `return _workerTerminate.AttemptToTerminate(WorkerThread, TimeSpan.Zero);` with `return _workerTerminate.AttemptToTerminate(WorkerTask, TimeSpan.Zero);`

  **PrimaryWorker.cs** changes:
  1. Add `using System.Threading.Tasks;` to usings.
  2. Line 75: Replace `if (WorkerThread != null) return;` with `if (WorkerTask != null) return;`
  3. Lines 83-84: Replace:
     ```csharp
     WorkerThread = new Thread(MainLoop) { Name = _nameFactory.Create() };
     WorkerThread.Start();
     ```
     With:
     ```csharp
     WorkerName = _nameFactory.Create();
     WorkerTask = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);
     ```
  4. Line 86: Replace `_log.LogDebug($"{WorkerThread.Name} created");` with `_log.LogDebug($"{WorkerName} created");`
  5. Lines 98-100: Replace:
     ```csharp
     if (WorkerThread != null)
     {
         _log.LogDebug($"Stopping worker thread {WorkerThread.Name}");
     }
     ```
     With:
     ```csharp
     if (WorkerTask != null)
     {
         _log.LogDebug($"Stopping worker {WorkerName}");
     }
     ```
  6. Remove `using System.Threading;` ONLY if no other Thread/Monitor/etc. references remain. Check: `ShouldExit` uses `Monitor` from base class, so the using is inherited. PrimaryWorker itself does not directly use `System.Threading` types -- but verify. If the only `System.Threading` usage is `Thread` (now removed), the using can go. However, `CancellationToken` etc. may be in scope. Safe to leave the using statement.

  **Worker.cs** changes:
  1. Add `using System.Threading.Tasks;` to usings.
  2. Line 71: Replace `if (WorkerThread != null) return;` with `if (WorkerTask != null) return;`
  3. Lines 77-78: Replace:
     ```csharp
     WorkerThread = new Thread(MainLoop) { Name = _nameFactory.Create() };
     WorkerThread.Start();
     ```
     With:
     ```csharp
     WorkerName = _nameFactory.Create();
     WorkerTask = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);
     ```
  4. Line 80: Replace `_log.LogDebug($"{WorkerThread.Name} created");` with `_log.LogDebug($"{WorkerName} created");`
  5. Lines 92-95: Replace:
     ```csharp
     if (WorkerThread == null)
         return;

     _log.LogDebug($"Stopping worker thread {WorkerThread.Name}");
     ```
     With:
     ```csharp
     if (WorkerTask == null)
         return;

     _log.LogDebug($"Stopping worker {WorkerName}");
     ```

  **MultiWorkerBase.cs** changes:
  1. Line 62: Replace:
     `public override bool Running => WorkerThread != null && WorkerThread.IsAlive || MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0;`
     With:
     `public override bool Running => WorkerTask != null && !WorkerTask.IsCompleted || MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0;`
  2. Line 73: Replace `if (WorkerThread == null || !WorkerThread.IsAlive) return;` with `if (WorkerTask == null || WorkerTask.IsCompleted) return;`
  3. Line 75: Replace `StopThread.TryForceTerminate(WorkerThread);` with `StopThread.TryForceTerminate(WorkerTask);`
  </action>
  <verify>cd F:\Git\DotNetWorkQueue && dotnet build Source/DotNetWorkQueue/DotNetWorkQueue.csproj -c Debug --no-restore 2>&1 | tail -5</verify>
  <done>
  - `grep -rn "new Thread(" Source/DotNetWorkQueue/Queue/PrimaryWorker.cs Source/DotNetWorkQueue/Queue/Worker.cs` returns no hits
  - `grep -n "protected Thread" Source/DotNetWorkQueue/Queue/WorkerBase.cs` returns no hits
  - `grep -n "Thread.IsAlive" Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs` returns no hits
  - PrimaryWorker and Worker use `Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning)`
  - Worker name is stored in `WorkerName` string field and used in all log messages
  - DotNetWorkQueue.csproj builds cleanly across all target frameworks
  </done>
</task>

<task id="2" files="Source/DotNetWorkQueue/Queue/WorkerTerminate.cs, Source/DotNetWorkQueue/Queue/StopThread.cs, Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs" tdd="false">
  <action>
  **WorkerTerminate.cs** changes:
  1. Add `using System.Threading.Tasks;` (keep `using System.Threading;` -- not needed after changes, but harmless to keep).
  2. Replace the entire `AttemptToTerminate` method (lines 35-46):
     ```csharp
     public bool AttemptToTerminate(Task workerTask, TimeSpan? timeout)
     {
         if (workerTask == null || workerTask.IsCompleted)
             return true; //if the task is null or completed, its terminated

         if (timeout.HasValue)
         {
             return workerTask.Wait(timeout.Value);
         }
         workerTask.Wait();
         return true;
     }
     ```
  3. Update the XML doc comment: change "thread" to "task" in the parameter doc.

  **StopThread.cs** changes:
  1. Add `using System.Threading.Tasks;`.
  2. Replace the `TryForceTerminate` method signature and body (lines 46-49):
     ```csharp
     public bool TryForceTerminate(Task workerTask)
     {
         _waitForThreadToFinish.Wait(workerTask);
         return true;
     }
     ```
  3. Update XML doc: change "thread" to "task" in parameter doc.

  **WaitForThreadToFinish.cs** changes:
  1. Add `using System.Threading.Tasks;`.
  2. Remove `using System.Diagnostics;` (Stopwatch no longer needed).
  3. Replace the entire `Wait` method (lines 49-73):
     ```csharp
     public bool Wait(Task workerTask, TimeSpan? timeout = null)
     {
         if (workerTask == null || workerTask.IsCompleted)
             return true;

         try
         {
             if (timeout.HasValue)
             {
                 return workerTask.Wait(timeout.Value);
             }
             workerTask.Wait();
             return true;
         }
         catch (AggregateException)
         {
             // Task faulted or was canceled -- either way, it has finished
             return true;
         }
     }
     ```
     This eliminates the Thread.IsAlive polling loop and Thread.Sleep(20) entirely. The `Task.Wait()` call blocks efficiently until the task completes. The try-catch handles the case where the task threw an exception (which should not happen since MainLoop does not throw, but is defensive).
  4. Update XML doc: change "thread" references to "task".
  5. The periodic "Still waiting" log message is intentionally removed because `Task.Wait()` blocks atomically -- there is no polling loop in which to log. If timeout-based waiting with logging is desired, it can be added later, but the current behavior (silent wait) matches the `Task.Wait()` semantics.
  </action>
  <verify>cd F:\Git\DotNetWorkQueue && dotnet build Source/DotNetWorkQueue/DotNetWorkQueue.csproj -c Debug --no-restore 2>&1 | tail -5</verify>
  <done>
  - `grep -n "Thread workerThread" Source/DotNetWorkQueue/Queue/WorkerTerminate.cs Source/DotNetWorkQueue/Queue/StopThread.cs Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` returns no hits
  - `grep -n "Thread.Join" Source/DotNetWorkQueue/Queue/WorkerTerminate.cs` returns no hits
  - `grep -n "Thread.IsAlive" Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` returns no hits
  - `grep -n "Thread.Sleep" Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` returns no hits
  - All three files use `Task` parameter types instead of `Thread`
  - WaitForThreadToFinish.Wait uses `Task.Wait()` instead of polling loop
  - DotNetWorkQueue.csproj builds cleanly
  </done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Tests/Queue/WaitForThreadToFinishTests.cs" tdd="false">
  <action>
  Rewrite WaitForThreadToFinishTests.cs to use Task-based workloads instead of Thread-based ones.

  Replace the entire file content with:
  ```csharp
  using System;
  using System.Diagnostics;
  using System.Threading.Tasks;
  using AutoFixture;
  using AutoFixture.AutoNSubstitute;
  using DotNetWorkQueue.Queue;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  namespace DotNetWorkQueue.Tests.Queue
  {
      [TestClass]
      public class WaitForThreadToFinishTests
      {
          [TestMethod]
          public void Wait()
          {
              var task = Task.Factory.StartNew(() => Task.Delay(3000).Wait(),
                  TaskCreationOptions.LongRunning);
              var watch = Stopwatch.StartNew();
              var test = Create();
              test.Wait(task);
              watch.Stop();
              Assert.IsTrue(task.IsCompleted);
              Assert.IsInRange(2950L, 4250L, watch.ElapsedMilliseconds);
          }

          [TestMethod]
          public void Wait_Long()
          {
              var task = Task.Factory.StartNew(() => Task.Delay(7000).Wait(),
                  TaskCreationOptions.LongRunning);
              var watch = Stopwatch.StartNew();
              var test = Create();
              test.Wait(task);
              watch.Stop();
              Assert.IsTrue(task.IsCompleted);
              Assert.IsInRange(6950L, 9000L, watch.ElapsedMilliseconds);
          }

          [TestMethod]
          public void Wait_With_Timeout()
          {
              var task = Task.Factory.StartNew(() => Task.Delay(3000).Wait(),
                  TaskCreationOptions.LongRunning);
              var watch = Stopwatch.StartNew();
              var test = Create();
              var result = test.Wait(task, TimeSpan.FromMilliseconds(1000));
              watch.Stop();
              Assert.IsFalse(result);
              Assert.IsInRange(950L, 3000L, watch.ElapsedMilliseconds);
          }

          [TestMethod]
          public void Wait_Null_Task_Returns_True()
          {
              var test = Create();
              Assert.IsTrue(test.Wait(null));
          }

          [TestMethod]
          public void Wait_Completed_Task_Returns_True()
          {
              var test = Create();
              Assert.IsTrue(test.Wait(Task.CompletedTask));
          }

          private WaitForThreadToFinish Create()
          {
              var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
              return fixture.Create<WaitForThreadToFinish>();
          }
      }
  }
  ```

  Key changes:
  - Replace `new Thread(RunMe)` / `t.Start()` with `Task.Factory.StartNew(() => Task.Delay(N).Wait(), TaskCreationOptions.LongRunning)`
  - Add `Wait_Null_Task_Returns_True` test for null guard path
  - Add `Wait_Completed_Task_Returns_True` test for already-completed task path
  - Remove the private `RunMe` / `RunMeLong` methods (replaced by inline Task.Delay)
  - Keep timing assertions (they validate that Wait actually blocks for the expected duration)
  </action>
  <verify>cd F:\Git\DotNetWorkQueue && dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj --filter "FullyQualifiedName~WaitForThreadToFinishTests" --no-restore 2>&1 | tail -10</verify>
  <done>
  - All 5 WaitForThreadToFinishTests pass (Wait, Wait_Long, Wait_With_Timeout, Wait_Null_Task_Returns_True, Wait_Completed_Task_Returns_True)
  - No references to `System.Threading.Thread` in the test file
  - `grep -rn "new Thread(" Source/DotNetWorkQueue.Tests/Queue/WaitForThreadToFinishTests.cs` returns no hits
  </done>
</task>

## Final Verification

After all 3 tasks are complete, run these commands to confirm Phase 7 success criteria:

```bash
# 1. Zero new Thread( in worker classes
grep -rn "new Thread(" Source/DotNetWorkQueue/Queue/ --include="*.cs"
# Expected: no output

# 2. Zero Thread.Sleep in BaseMonitor (covered by Plan 01)
grep -n "Thread.Sleep" Source/DotNetWorkQueue/Queue/BaseMonitor.cs
# Expected: no output

# 3. MultiWorkerBase.Running checks task completion
grep -n "IsCompleted" Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs
# Expected: line with !WorkerTask.IsCompleted

# 4. WaitForThreadToFinish uses Task.Wait
grep -n "Task.Wait\|workerTask.Wait" Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs
# Expected: lines with workerTask.Wait

# 5. Full solution builds
dotnet build Source/DotNetWorkQueue.sln -c Debug

# 6. All unit tests pass
dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj

# 7. In-memory integration tests pass
dotnet test Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj
```

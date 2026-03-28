---
phase: remove-thread-abort
plan: "2.1"
wave: 2
dependencies: ["1.1", "1.2"]
must_haves:
  - Remove AbortWorkerThreadsWhenStopping from IWorkerConfiguration and WorkerConfiguration
  - Update TimeToWaitForWorkersToCancel XML docs to remove abort references
  - Update comments in StopWorkers.cs and MultiWorkerBase.cs to remove abort references
  - Remove 2 abort-related tests from WorkerConfigurationTests.cs
  - Full solution builds and all unit tests pass
files_touched:
  - Source/DotNetWorkQueue/IWorkerConfiguration.cs
  - Source/DotNetWorkQueue/Configuration/WorkerConfiguration.cs
  - Source/DotNetWorkQueue/Queue/StopWorkers.cs
  - Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs
  - Source/DotNetWorkQueue.Tests/Configuration/WorkerConfigurationTests.cs
tdd: false
---

# Plan 2.1: Remove Config Property, Update Comments, Update Tests

## Goal

Remove the `AbortWorkerThreadsWhenStopping` configuration property from the interface and implementation, update all XML docs and comments that reference abort behavior, and remove the corresponding test methods. This is the final cleanup wave that completes Phase 6.

## Why Wave 2

This plan depends on Plans 1.1 and 1.2 because:
- Plan 1.1 removes the `AbortWorkerThread` class that reads `AbortWorkerThreadsWhenStopping` from configuration. Removing the config property first would cause a compile error in the still-existing `AbortWorkerThread` constructor.
- Plan 1.1 removes the `AbortWorkerThreadDecorator` that also reads `AbortWorkerThreadsWhenStopping`. Same issue.
- Once 1.1 has removed all consumers of the property, this plan safely removes the property itself.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/IWorkerConfiguration.cs, Source/DotNetWorkQueue/Configuration/WorkerConfiguration.cs" tdd="false">
  <action>Remove the `AbortWorkerThreadsWhenStopping` property and update `TimeToWaitForWorkersToCancel` docs:

  **IWorkerConfiguration.cs:**
  1. Delete the `AbortWorkerThreadsWhenStopping` property declaration and its XML doc comment (lines 62-69 approximately):
     ```
     /// <summary>
     /// If true, worker threads will be aborted if they don't respond to <see cref="TimeToWaitForWorkersToCancel"/>
     /// </summary>
     /// <remarks>...</remarks>
     bool AbortWorkerThreadsWhenStopping { get; set; }
     ```
  2. Update the `TimeToWaitForWorkersToCancel` XML doc `<remarks>` (lines 48-51 approximately). Replace:
     ```
     /// <remarks>
     /// If thread aborting is disabled, this setting has no affect; we will wait forever for threads to finish working
     /// Otherwise, the thread will be aborted once this time limit is reached.
     /// </remarks>
     ```
     With:
     ```
     /// <remarks>
     /// After this timeout expires, the queue will wait indefinitely for worker threads to finish their current work.
     /// </remarks>
     ```

  **WorkerConfiguration.cs:**
  1. Delete the `_abortWorkerThreadsWhenStopping` backing field (line 31).
  2. Delete the `AbortWorkerThreadsWhenStopping` property and its XML doc (lines 104-119 approximately).
  3. Update the `TimeToWaitForWorkersToCancel` XML doc `<remarks>` to match the interface change above (lines ~73-77).
  </action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -rn "AbortWorkerThreadsWhenStopping\|_abortWorkerThreadsWhenStopping" Source/DotNetWorkQueue/IWorkerConfiguration.cs Source/DotNetWorkQueue/Configuration/WorkerConfiguration.cs; echo "EXIT: $?"</verify>
  <done>Neither file contains any reference to AbortWorkerThreadsWhenStopping or its backing field.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue/Queue/StopWorkers.cs, Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs" tdd="false">
  <action>Update inline comments that reference abort behavior:

  **StopWorkers.cs:**
  1. Line 103: Change `//force kill workers that are still running by aborting the thread, or waiting until work has completed` to `//wait for workers that are still running to finish their current work`
  2. Line 156: Same change as line 103 (identical comment appears twice).

  **MultiWorkerBase.cs:**
  1. Line 71: Change `AttemptToTerminate(); //one last request to terminate without an abort or a spin and wait` to `AttemptToTerminate(); //one last request to terminate before waiting for thread to finish`
  </action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -rn "abort" Source/DotNetWorkQueue/Queue/StopWorkers.cs Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs; echo "EXIT: $?"</verify>
  <done>Neither file contains the word "abort" (case-insensitive grep returns no hits).</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Tests/Configuration/WorkerConfigurationTests.cs" tdd="false">
  <action>Remove the 2 test methods that test the deleted `AbortWorkerThreadsWhenStopping` property:

  1. Delete the `SetAndGet_AbortWorkerThreadsWhenStopping` test method (lines 16-22 approximately). This test creates a `WorkerConfiguration`, sets `AbortWorkerThreadsWhenStopping = true`, and asserts it reads back as `true`.
  2. Delete the `Set_AbortWorkerThreadsWhenStopping_WhenReadOnly_Fails` test method (lines 75-85 approximately). This test creates a `WorkerConfiguration`, sets it readonly, and asserts that setting `AbortWorkerThreadsWhenStopping` throws.

  Leave all other test methods in the class intact (`SetAndGet_WorkerCount`, `SetAndGet_SingleWorkerWhenNoWorkFound`, `SetAndGet_TimeToWaitForWorkersToCancel`, `SetAndGet_TimeToWaitForWorkersToStop`, and their readonly counterparts).

  After deletion, run the full Phase 6 verification suite.</action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -rn "AbortWorkerThreadsWhenStopping" Source/DotNetWorkQueue.Tests/ && echo "FOUND" || echo "CLEAN" && dotnet build Source/DotNetWorkQueue.sln -c Debug --no-restore 2>&1 | tail -3 && dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj --no-build -c Debug 2>&1 | tail -5</verify>
  <done>All of the following are true:
  1. `grep -r "AbortWorkerThreadsWhenStopping" Source/ --include="*.cs"` returns no hits
  2. `grep -r "ThreadAbortException" Source/DotNetWorkQueue/ --include="*.cs"` returns no hits
  3. `grep -r "\.Abort()" Source/DotNetWorkQueue/Queue/ --include="*.cs"` returns no hits
  4. `dotnet build Source/DotNetWorkQueue.sln -c Debug` succeeds with zero errors
  5. `dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` passes all tests
  6. `dotnet test Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj` passes all tests</done>
</task>

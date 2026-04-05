---
phase: remove-thread-abort
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Remove all 5 ThreadAbortException catch blocks
  - Remove associated ReSharper suppression comments
  - Verify adjacent catch blocks already handle the same logic
files_touched:
  - Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs
  - Source/DotNetWorkQueue/Queue/MessageProcessing.cs
  - Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs
  - Source/DotNetWorkQueue/Queue/ProcessMessage.cs
  - Source/DotNetWorkQueue/Queue/ProcessMessageAsync.cs
tdd: false
---

# Plan 1.2: Remove ThreadAbortException Catch Blocks

## Goal

Remove all 5 `catch (ThreadAbortException)` blocks and their associated `// ReSharper disable once UncatchableException` comments from the message processing pipeline. Each catch block has an adjacent `catch (OperationCanceledException)` or `catch (Exception)` that performs identical logic, so removal is safe and does not change behavior.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs" tdd="false">
  <action>In `HeartBeatWorker.cs`, delete the `ThreadAbortException` catch block inside `SendHeartBeatInternal()`. This is a 13-line block consisting of:
  - The ReSharper comment: `// ReSharper disable once UncatchableException`
  - The catch clause: `catch (ThreadAbortException error)`
  - The body: logging a warning, setting error on heartbeat notification, and calling `SetCancel()` (lines 221-233 approximately)

  The general `catch (Exception error)` block immediately following it (lines ~234-245) already performs the same three operations: logs the error, sets error on heartbeat notification, and calls `SetCancel()`.

  After deletion, also remove `using System.Threading;` ONLY if no other reference to types in `System.Threading` remains in the file. (Note: `System.Threading` IS still used for `CancellationTokenSource`, `Interlocked`, `Thread`, and `Monitor` -- so keep the using.)</action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -n "ThreadAbortException" Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs; echo "EXIT: $?"</verify>
  <done>HeartBeatWorker.cs contains zero references to ThreadAbortException.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue/Queue/MessageProcessing.cs, Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs" tdd="false">
  <action>In both files, delete the `ThreadAbortException` catch block inside the message processing try/catch:

  **MessageProcessing.cs** -- Inside `TryProcessIncomingMessage()`, delete the block (lines ~161-167):
  - `// ReSharper disable once UncatchableException`
  - `catch (ThreadAbortException ex) { _rollbackMessage.Rollback(context); _consumerQueueNotification.InvokeRollback(...); }`
  The preceding `catch (OperationCanceledException ex)` block (lines ~155-160) performs the identical rollback + notify logic.

  **MessageProcessingAsync.cs** -- Inside `TryProcessIncomingMessageAsync()`, delete the block (lines ~169-174):
  - `// ReSharper disable once UncatchableException`
  - `catch (ThreadAbortException ex) { _rollbackMessage.Rollback(context); _consumerQueueNotification.InvokeRollback(...); }`
  The preceding `catch (OperationCanceledException ex)` block (lines ~164-168) performs the identical rollback + notify logic.

  In both files, keep `using System.Threading;` as it is used for other types.</action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -rn "ThreadAbortException" Source/DotNetWorkQueue/Queue/MessageProcessing.cs Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs; echo "EXIT: $?"</verify>
  <done>Neither MessageProcessing.cs nor MessageProcessingAsync.cs contain any reference to ThreadAbortException.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue/Queue/ProcessMessage.cs, Source/DotNetWorkQueue/Queue/ProcessMessageAsync.cs" tdd="false">
  <action>In both files, delete the `ThreadAbortException` catch block inside the message handling try/catch:

  **ProcessMessage.cs** -- Inside `Handle()`, delete the block (lines ~80-85):
  - `// ReSharper disable once UncatchableException`
  - `catch (ThreadAbortException) { heartBeat.Stop(); throw; }`
  The following `catch (OperationCanceledException)` block (lines ~86-89) performs the identical `heartBeat.Stop(); throw;` logic.

  **ProcessMessageAsync.cs** -- Inside `HandleAsync()`, delete the block (lines ~81-85):
  - `// ReSharper disable once UncatchableException`
  - `catch (ThreadAbortException) { heartBeat.Stop(); throw; }`
  The following `catch (OperationCanceledException)` block (lines ~86-89) performs the identical `heartBeat.Stop(); throw;` logic.

  In both files, keep `using System.Threading;` as it is used for other types.</action>
  <verify>cd F:/Git/DotNetWorkQueue && grep -rn "ThreadAbortException" Source/DotNetWorkQueue/Queue/ProcessMessage.cs Source/DotNetWorkQueue/Queue/ProcessMessageAsync.cs; echo "EXIT: $?"</verify>
  <done>Neither ProcessMessage.cs nor ProcessMessageAsync.cs contain any reference to ThreadAbortException.</done>
</task>

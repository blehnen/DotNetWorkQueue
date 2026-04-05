# Root Cause Analysis

## Severity: SEV3-Moderate

Based on: intermittent test flake (1-in-100 messages), no data loss in production, only affects the metrics assertion, not actual message processing correctness.

## Problem Statement

`MultiConsumerAsync.Run(100,0,180,10,5,0,True,True)` fails on Linux/net10.0 with `Assert.AreEqual failed. Expected:<100>. Actual:<99>` in `VerifyMetrics.VerifyProcessedCount`. The assertion checks `CommitMessage.CommitCounter` in the in-process metrics snapshot. All 100 messages are fully processed and committed to SQL Server (the `processedCount` check at line 86 passes), but the metrics counter records only 99.

## Evidence Chain

1. `ConsumerAsyncShared.RunConsumer` (line 82) waits on `waitForFinish.Wait(timeOut * 1000)` — the event is **set by the user handler** (`MessageHandlingShared.HandleFakeMessages`, line 39), not by the commit path.
   - File: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncShared.cs:82`

2. The user handler increments `processedCount` and calls `waitForFinish.Set()` when count reaches `messageCount` — **before** `CommitMessage.Commit` is called.
   - File: `Source/DotNetWorkQueue.IntegrationTests.Shared/MessageHandlingShared.cs:28-41`

3. `ProcessMessageAsync.HandleAsync` calls the user handler **then** calls `_commitMessage.Commit(context)` on the very next line after `await _methodToRun.HandleAsync(...)`.
   - File: `Source/DotNetWorkQueue/Queue/ProcessMessageAsync.cs:77-78`

4. `CommitMessageDecorator.Commit` increments `_commitCounter` **only after** the underlying `_handler.Commit(context)` returns `true`.
   - File: `Source/DotNetWorkQueue/Metrics/Decorator/ICommitMessageDecorator.cs:47-51`

5. The **race window**: when the 100th message is handled, the user handler increments `processedCount` to 100 and sets `waitForFinish` on one async task. The `using (var queue = ...)` block in `RunConsumer` **exits** (disposing the consumer), which stops all worker threads. The `CommitMessage.Commit` for that 100th message is still in-flight (or queued) on the worker thread when the `using` block disposes. The metrics snapshot is taken immediately at line 87 — before the 100th commit metric increment has fired.
   - File: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncShared.cs:82-88`

6. With `enableChaos=true`, `timeOut` is doubled (line 33), so the wait itself is not the trigger. But chaos injects random `SqlException` faults (50% rate, up to `RetryCount` retries) on the SQL commit path via `ChaosFaultStrategyOptions`. On Linux with a remote SQL Server at 192.168.0.2, retries add real network latency. The final commit may take measurably longer to complete.
   - File: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/RetrySqlPolicyCreation.cs:133-143`
   - File: `Source/DotNetWorkQueue.Transport.Shared/Basic/Chaos/ChaosPolicyShared.cs:67`

7. On Windows/net48 (TeamCity), the OS thread scheduler and SQL Server co-location meant the final commit completed faster, hiding the race. On Linux/net10.0 with Docker networking to an external SQL Server, the extra latency from chaos-injected retries widens the window enough for it to manifest.

8. `waitForFinish` is a `ManualResetEventSlim`. Once set, execution falls through immediately to `Assert.IsNull(processedCount.IdError)` and `VerifyMetrics.VerifyProcessedCount(...)` — there is **no barrier** ensuring all in-flight worker commits have flushed before the snapshot is taken.
   - File: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncShared.cs:85-88`

## 5 Whys

1. **Why does `CommitCounter` show 99 when 100 messages were processed?**
   Because the metrics snapshot is captured before the 100th `CommitMessageDecorator.Commit` increments the counter.

2. **Why is the snapshot captured before the 100th commit metric fires?**
   Because `waitForFinish` is set inside the user handler, which runs *before* `_commitMessage.Commit` is called in `ProcessMessageAsync.HandleAsync`. The test proceeds to assertion the moment the event fires.

3. **Why does this matter only for the last message?**
   Because all prior commits complete well before message #100 triggers `waitForFinish.Set()`. Only the commit of message #100 is racing with the assertion.

4. **Why does it only happen on Linux/net10.0 and not Windows/net48?**
   Because chaos mode injects SQL fault retries (50% injection rate, exponential backoff) into the commit path via Polly Simmy. On Linux Docker with a remote SQL Server, each retry adds real network round-trip time. The gap between `waitForFinish.Set()` and `_commitCounter.Increment()` for message #100 grows large enough to be observable. On Windows with a local/same-LAN SQL Server and the net48 thread scheduler, the gap was sub-millisecond and never triggered.

5. **Why is the test designed to signal completion before the commit is recorded?**
   The test's completion signal (`waitForFinish.Set()`) is placed inside the *user-supplied message handler callback*, which is the correct place from an application perspective (the user knows their work is done). But the infrastructure commit — and its metric increment — happens *after* the handler returns, in framework code. The test does not account for this ordering.

## Root Cause

There is a **structural ordering gap** in the test assertion logic: `waitForFinish` is set by the user handler *before* `CommitMessage.Commit` (and thus `CommitCounter.Increment`) executes. The test snapshot `metrics.GetCurrentMetrics()` is taken immediately when the event fires. Under normal conditions the commit completes in microseconds and the race is undetectable. When chaos mode is active on Linux/Docker with a remote SQL Server, retry latency on the final commit widens the window to tens or hundreds of milliseconds, making the race consistently observable at the 1-in-100 level.

## Remediation Plan

**Option A (Preferred): Add a post-wait drain before the metrics assertion**

After `waitForFinish.Wait(timeOut * 1000)` and before the assertions, add a short spin-wait or `Task.Delay` that polls `AsyncTaskCount` (already exposed on `MessageProcessingAsync`) until it reaches zero, confirming all in-flight commits have landed. This is a test-infrastructure fix only.

1. `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncShared.cs:82` — after the `waitForFinish.Wait(...)` call, add a loop that waits for the consumer's async task count to drain to zero before proceeding to assertions.

**Option B (Simpler, smaller risk): Add a fixed small delay after the wait when chaos is enabled**

1. `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncShared.cs:82` — after `waitForFinish.Wait(...)`, if `enableChaos` is true, `Thread.Sleep(500)` to allow the final in-flight commit and its metric increment to complete before asserting.

**Option C: Move the completion signal to after the commit**

Move the `processedCount` increment and `waitForFinish.Set()` call to a post-commit notification hook (e.g., wire into `IConsumerQueueNotification.MessageComplete`). This is architecturally cleaner but requires more invasive test infrastructure changes.

**Option D: Widen the assertion to tolerate off-by-one with chaos**

Change `Assert.AreEqual(messageCount, counter.Value)` to allow `counter.Value >= messageCount - 1` when chaos is enabled. This is the least desirable option as it masks the underlying race.

**Recommended**: Option A or B. Option B is the smallest-risk fix. Option A is more correct.

## Verification

After applying the fix:
1. Run `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/..." --filter "FullyQualifiedName~MultiConsumerAsync.Run"` repeatedly (50+ times) on the Linux/Docker CI environment.
2. Confirm no `Assert.AreEqual` failures on `CommitMessage.CommitCounter`.
3. Confirm the test still passes on Windows/net48 (the fix must be timing-safe on both platforms).
4. Confirm that the same fix pattern is applied to the equivalent `MultiConsumerAsync` tests for PostgreSQL, SQLite, and Redis transports, which share the same shared infrastructure.

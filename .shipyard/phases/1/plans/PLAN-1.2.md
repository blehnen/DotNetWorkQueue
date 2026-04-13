---
phase: quick-wins
plan: 2
wave: 1
dependencies: []
must_haves:
  - ActivityListener always active in CreateTrace() for CI coverage
  - Collected activities accessible for test assertions
  - At least one existing Memory integration test verifies trace activity collection
files_touched:
  - Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs
  - Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs
tdd: false
risk: medium
---

# Plan 1.2 -- In-Memory Trace Exporter for CI Integration Tests

## Context

All `TraceExtensions` files across 6 transports show 0% code coverage. The root cause: `SharedSetup.CreateTrace()` creates an `ActivitySource` but no `ActivityListener` is registered (unless `TraceSettings.Enabled` is true, which it is not in CI). Without a listener, `ActivitySource.StartActivity()` returns `null`, and every trace decorator short-circuits immediately.

The fix: embed an `ActivityListener` inside `ActivitySourceWrapper` that is ALWAYS active (not gated by `TraceSettings.Enabled`). This listener collects activities in a `ConcurrentBag<Activity>` so tests can assert on recorded spans. The existing OTLP exporter setup (gated by `TraceSettings.Enabled`) remains unchanged for optional Jaeger usage.

### Span names emitted by the Memory transport produce/consume path

From `DataStorageSendMessageDecorator`: `"SendMessage"`
From `ReceiveMessagesDecorator`: `"ReceiveMessage"`
From `CommitMessageDecorator`: `"Commit"`
From `RemoveMessageDecorator`: `"Remove"`
From `MessageHandlerDecorator`: `"MessageHandler"`

## Tasks

<task id="1" files="Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs" tdd="false">
  <action>Modify `ActivitySourceWrapper` class and `CreateTrace()` method in `SharedSetup.cs`:

1. Add `using System.Collections.Concurrent;` to the file's imports.

2. Extend the `ActivitySourceWrapper` class:
   - Add a `private readonly ActivityListener _listener;` field.
   - Add a public property `public ConcurrentBag<Activity> CollectedActivities { get; } = new();`
   - Change the constructor to accept `ActivitySource source` and create an `ActivityListener`:
     ```
     _listener = new ActivityListener
     {
         ShouldListenTo = s => s.Name == source.Name,
         Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
         ActivityStarted = activity => CollectedActivities.Add(activity)
     };
     ActivitySource.AddActivityListener(_listener);
     ```
   - In `Dispose()`, add `_listener?.Dispose();` BEFORE `Source?.Dispose()`. Keep the existing `TraceSettings.Enabled` sleep for Jaeger backward compatibility.

3. No changes to `CreateTrace()` method signature or the OTLP exporter setup. The listener is always active regardless of `TraceSettings.Enabled` because it is created in the `ActivitySourceWrapper` constructor.

Key design points:
- `ActivityListener` is registered globally via `ActivitySource.AddActivityListener()`, so it captures activities from all `StartActivity()` calls on matching sources.
- Using `ActivityStarted` callback (not `ActivityStopped`) ensures activities are collected even if the decorator's `using` block hasn't exited yet. Tests will see activities immediately.
- `ConcurrentBag<Activity>` is thread-safe for the parallel producer scenarios.
- Disposing the listener in `Dispose()` before the source ensures clean teardown.</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug --no-restore 2>&1 | tail -5</verify>
  <done>`DotNetWorkQueue.IntegrationTests.Shared` builds with 0 errors. `ActivitySourceWrapper` has a public `CollectedActivities` property of type `ConcurrentBag<Activity>` and creates an `ActivityListener` in its constructor that samples all data from the matching source name.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs" tdd="false">
  <action>Add a new test method to the existing `SimpleProducer` test class in `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs` that verifies trace activities are collected.

1. Add the following `using` directives if not already present:
   - `using System.Linq;`

2. Add a new test method `RunWithTraceVerification` with `[TestMethod]` attribute (no `[DataRow]` -- use fixed small values for speed):
   ```
   [TestMethod]
   public void RunWithTraceVerification()
   {
       using (var connectionInfo = new IntegrationConnectionInfo())
       {
           var queueName = GenerateQueueName.Create();
           var logProvider = LoggerShared.Create(queueName, "SimpleProducerTrace");

           using (var queueCreator = new QueueCreationContainer<MemoryMessageQueueInit>(
               serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
           {
               using (var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(
                   new QueueConnection(queueName, connectionInfo.ConnectionString)))
               {
                   var result = oCreation.CreateQueue();
                   Assert.IsTrue(result.Success, result.ErrorMessage);

                   using (var trace = SharedSetup.CreateTrace("producer"))
                   {
                       using (var metrics = new Metrics.Metrics(queueName))
                       {
                           using (var creator = SharedSetup.CreateCreator<MemoryMessageQueueInit>(
                               InterceptorAdding.No, logProvider, metrics, false, false,
                               oCreation.Scope, trace.Source))
                           {
                               using (var queue = creator.CreateProducer<FakeMessage>(
                                   new QueueConnection(queueName, connectionInfo.ConnectionString)))
                               {
                                   queue.Send(GenerateMessage.Create<FakeMessage>());
                               }
                           }
                       }

                       // Verify trace activities were collected
                       Assert.IsTrue(trace.CollectedActivities.Count > 0,
                           "Expected at least one trace activity to be collected, but none were recorded. " +
                           "This indicates the ActivityListener is not capturing spans from the producer path.");

                       var activityNames = trace.CollectedActivities.Select(a => a.OperationName).ToList();
                       CollectionAssert.Contains(activityNames, "SendMessage",
                           $"Expected a 'SendMessage' span in collected activities. Found: [{string.Join(", ", activityNames)}]");
                   }

                   oCreation.RemoveQueue();
               }
           }
       }
   }
   ```

3. Add any required `using` statements for types referenced:
   - `using DotNetWorkQueue.IntegrationTests.Shared;` (already present)
   - `using DotNetWorkQueue.IntegrationTests.Shared.Producer;` (already present)
   - `using DotNetWorkQueue.Logging;` (for `LoggerShared`)
   - `using DotNetWorkQueue.Queue;` (for `QueueCreationContainer`)
   - `using DotNetWorkQueue.Messages;` (for `GenerateMessage`)
   - `using System.Linq;`

This test deliberately uses a low message count (1) and exercises only the producer path. It proves that:
- `ActivityListener` is active and collecting spans
- The `SendMessage` span from `DataStorageSendMessageDecorator` is recorded
- `CollectedActivities` on `ActivitySourceWrapper` is accessible for assertions</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --filter "FullyQualifiedName~SimpleProducer.RunWithTraceVerification" --no-restore 2>&1 | tail -10</verify>
  <done>The `RunWithTraceVerification` test passes. It asserts that `trace.CollectedActivities.Count > 0` and that at least one activity has `OperationName == "SendMessage"`. This confirms the `ActivityListener` is working and trace decorator code paths are now exercised, which will show as covered lines in code coverage reports.</done>
</task>

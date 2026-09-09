# Async receive path — PR 2: `IReceiveMessages`

> ⚠️ **Tasks 2-7 of this plan are superseded.** See
> `docs/superpowers/specs/2026-09-08-async-receive-path-design-revision-1.md`. Task 2 as written
> here causes an unbounded de-queue runaway (measured: 15,910,404 concurrent in-flight
> receives), and the task 4 gate reproduces starvation on the send path, which this work does
> not touch. Task 1 is done and unaffected.


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `IReceiveMessages` an async twin, make the async consumer await it instead of blocking a pool thread, and prove the result with the test that currently documents the defect.

**Architecture:** The interface gains `ReceiveMessageAsync`; the four existing decorators each gain a mirroring method (no DI changes, since the interface is the same one). The consumer loop awaits both the receive and the throttle PR 1 made awaitable. Redis is then made genuinely async, because it is the transport the acceptance test exercises.

**Tech Stack:** .NET 10 / .NET 8, MSTest 4.2.3, NSubstitute, StackExchange.Redis 3.0.7, Polly 8.

**Spec:** `docs/superpowers/specs/2026-09-08-async-receive-path-design.md`

## Scope: this plan stops at the gate

The spec calls PR 2 the go/no-go gate — *"the one that matters and the one to stop at if the starvation twin will not go green."* This plan therefore covers **tasks 1 to 4**, ending with that test. Tasks 5 to 7 — SQL Server, PostgreSQL and Memory made genuinely async — are planned separately **after** the gate passes, and the PR is not opened until they are done, so no half-async receive path reaches a consumer.

Reaching the gate in four tasks rather than seven is the point. If the async receive cannot beat a capped thread pool, the design is wrong and we will have spent four tasks finding out.

## What already exists — do not rebuild it

Verified in the tree before writing this plan:

| | |
|---|---|
| `BaseLua.TryExecuteAsync` | exists, uses `db.ScriptEvaluateAsync` |
| `DequeueLua.ExecuteAsync(long unixTime)` | exists, returns `Task<RedisValue[]>` |
| `IQueryHandlerAsync<TQuery, TResult>` | exists in `Transport.Shared`, used by Dashboard queries |
| `IWaitForEventOrCancel.WaitAsync()` | added by PR 1 |
| `IWaitForEventOrCancelThreadPool.WaitAsync(IWorkGroup)` | added by PR 1 |
| `StarvationBaselineTests` | exists, deliberately RED, excluded via `TestCategory!=StarvationBaseline` |

The async Lua path is already there for the send side. Redis needs a handler that calls it and an awaitable work-sub — not a new Lua layer.

## Global Constraints

- Target frameworks `net10.0` and `net8.0`.
- `dotnet_diagnostic.CA2012.severity = error` is active. Consume each `ValueTask` exactly once; never `.Result` or `.GetAwaiter().GetResult()` on one.
- `ConfigureAwait(false)` on every await in production code.
- `.cs` files are CRLF. Match each file's existing BOM state — compare `head -c3 <file>` against `git show HEAD:<file> | head -c3`. Adding a BOM has broken this repository before.
- MSTest 4.x: `Assert.Throws<T>` / `ThrowsExactly<T>` / `ThrowsExactlyAsync<T>`. `Assert.ThrowsException` does not exist.
- LGPL-2.1 header on new non-test `.cs` files; copy it from a sibling. Test files here do not carry one.
- **Run every build and test in the FOREGROUND.** Do not start background jobs and do not wait on one — three agents stalled that way during PR 1.
- Before each commit: `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true`. Release sets `TreatWarningsAsErrors` and requires XML docs.

## Invariants every async receive must preserve

From the spec, and the reason a passing test suite is not sufficient evidence here. **Receive is not a pure fetch.** Each async implementation must do everything its sync twin does:

- attach `context.Commit`, `context.Rollback` and `context.Cleanup` handlers, including the transactional and non-transactional variants
- store the connection on the context where the sync version does
- call `SetMessageAndHeaders` on the poison path so the context carries message id, correlation id and headers
- Redis only: keep the `while (true)` loop over expired messages, and the `workSub.Reset()` on the work signal

An async rewrite that returns a message and nothing else will compile, return messages, and pass the four `ConsumerAsync*` scenarios while having dropped a cleanup handler.

## File Structure

| file | responsibility |
|---|---|
| `Source/DotNetWorkQueue/IReceiveMessages.cs` | add `ReceiveMessageAsync` |
| `Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesHistoryDecorator.cs` | mirror in async |
| `Source/DotNetWorkQueue/Metrics/Decorator/IReceiveMessagesDecorator.cs` | mirror in async |
| `Source/DotNetWorkQueue/Policies/Decorator/IReceiveMessagesPolicyDecorator.cs` | mirror, using Polly's `ExecuteAsync` |
| `Source/DotNetWorkQueue/Trace/Decorator/ReceiveMessagesDecorator.cs` | mirror in async |
| six transport receive classes | async implementations (task 1 temporary, task 3 real for Redis) |
| `Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs` | await the receive |
| `Source/DotNetWorkQueue/TaskScheduling/SchedulerMessageHandler.cs` | await the throttle |
| `Source/DotNetWorkQueue.Transport.Redis/IRedisQueueWorkSub.cs` + impl | `WaitAsync` |
| `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryHandler/ReceiveMessageQueryHandlerAsync.cs` | new, async Lua dequeue |
| `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Concurrency/StarvationBaselineTests.cs` | the gate test |

---

### Task 1: `ReceiveMessageAsync` on the interface and the four decorators

Every transport must implement the new member for the tree to compile, so this task adds a **temporary** synchronous-inside-async implementation to all six. Task 3 replaces Redis's; tasks 5-7 replace the rest. SQLite and LiteDb keep theirs permanently — they are embedded and have no real async I/O.

**Files:**
- Modify: `Source/DotNetWorkQueue/IReceiveMessages.cs`
- Modify: the four decorators listed in File Structure
- Modify: `Source/DotNetWorkQueue/Transport/Memory/Basic/MessageQueueReceive.cs`, `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbQueueReceiveMessages.cs`, `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueReceive.cs`, `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs`, `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`, `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueReceiveMessages.cs`
- Modify: `Source/DotNetWorkQueue.Transport.Shared/Basic/ReceiveErrorMessage.cs` and `Source/DotNetWorkQueue/Transport/Memory/Basic/ReceiveErrorMessage.cs` **only if** they implement `IReceiveMessages` — check with `grep -n "IReceiveMessages" <file>`; if they implement `IReceiveMessagesError` instead, leave them alone, that interface is PR 4.

**Interfaces:**
- Consumes: nothing from earlier tasks
- Produces: `ValueTask<IReceivedMessageInternal> IReceiveMessages.ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)` — returns the message, or `null` when none is available, exactly as the sync twin does.

- [ ] **Step 1: Add the interface member**

In `Source/DotNetWorkQueue/IReceiveMessages.cs`, after `ReceiveMessage`:

```csharp
        /// <summary>
        /// Returns a message to process, without blocking a thread while the transport is queried.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <param name="cancellation">Cancels the receive; the queue is stopping.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation);
```

Add `using System.Threading;` and `using System.Threading.Tasks;` if absent.

- [ ] **Step 2: Build to enumerate every implementation**

Run: `dotnet build "Source/DotNetWorkQueue.sln" -c Debug`

Expected: FAIL, one `CS0535` per class that implements `IReceiveMessages`. **Record that list** — it is the authoritative set of files this task must touch, and it is more reliable than the list above.

- [ ] **Step 3: Mirror the four decorators**

Each decorator's async method does what its sync one does. Add `using System.Threading;` / `using System.Threading.Tasks;` to each.

`ReceiveMessagesHistoryDecorator`:

```csharp
        /// <inheritdoc />
        public async ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            var result = await _handler.ReceiveMessageAsync(context, cancellation).ConfigureAwait(false);
            if (result != null && _options.EnableHistory && _options.HistoryOptions.TrackProcessing && context.MessageId != null && context.MessageId.HasValue)
            {
                try
                {
                    _history.RecordProcessingStart(context.MessageId.Id.Value.ToString());
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to record history for processing start of message {MessageId}", context.MessageId.Id.Value);
                }
            }
            return result;
        }
```

`Metrics.Decorator.ReceiveMessagesDecorator` — a straight mirror. **No timer.** An earlier
draft of this plan said to add an async timer beside the existing one; that was wrong. The
synchronous `ReceiveMessage` in this decorator does not time anything either — the class's
`_waitTimer` belongs to a different member — so adding one to the async path would make the
two paths report different metrics for the same operation:

```csharp
        /// <inheritdoc />
        public async ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            var result = await _handler.ReceiveMessageAsync(context, cancellation).ConfigureAwait(false);
            if (result != null)
            {
                ProcessResult(result);
            }
            return result;
        }
```

`ReceiveMessagesPolicyDecorator` — Polly 8 has `ExecuteAsync`; follow the shape `ISendMessagesPolicyDecorator` already uses:

```csharp
        /// <inheritdoc />
        public async ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            if (_policies.Registry.TryGetPipeline(_policies.Definition.ReceiveMessageFromTransport, out var pipeline))
            {
                return await pipeline.ExecuteAsync(async _ =>
                    await _handler.ReceiveMessageAsync(context, cancellation).ConfigureAwait(false)).ConfigureAwait(false);
            }
            return await _handler.ReceiveMessageAsync(context, cancellation).ConfigureAwait(false);
        }
```

If `ExecuteAsync`'s delegate signature does not accept that lambda, read `Source/DotNetWorkQueue/Policies/Decorator/ISendMessagesPolicyDecorator.cs` lines 78-98 and copy its exact shape — it is already doing this against the same Polly version.

`Trace.Decorator.ReceiveMessagesDecorator`:

```csharp
        /// <inheritdoc />
        public async ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            //we can't attach the span since we don't have the parent until after we get a message
            //so save off the start and end times, and replace those in the child span below
            var start = DateTime.UtcNow;
            var message = await _handler.ReceiveMessageAsync(context, cancellation).ConfigureAwait(false);
            var end = DateTime.UtcNow;
            if (message == null) return null;
            var activityContext = message.Extract(_tracer, _headers.StandardHeaders);

            //blocking operations can last forever for queues that can signal for new messages
            //so, we will treat this is a 0 ms operation, rather than have it possibly last for N
            if (IsBlockingOperation)
                start = end;

            using (var scope = _tracer.StartActivity("ReceiveMessageAsync", ActivityKind.Internal, activityContext, startTime: start))
            {
                scope?.AddMessageIdTag(message);
                scope?.SetTag("IsBlockingOperation", IsBlockingOperation);
                scope?.SetEndTime(end);
                return message;
            }
        }
```

- [ ] **Step 4: Give every transport a temporary implementation**

For each transport class the build named in Step 2, add this beside its `ReceiveMessage`, substituting nothing but the comment's transport name:

```csharp
        /// <inheritdoc />
        public ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            //TEMPORARY - replaced with a genuinely asynchronous implementation later in this
            //PR. Deliberately NOT Task.Run: that would move the block to a different pool
            //thread while looking like a fix.
            return new ValueTask<IReceivedMessageInternal>(ReceiveMessage(context));
        }
```

For **SQLite** and **LiteDb** this is the final implementation, so write their comment differently — it must not read as unfinished:

```csharp
        /// <inheritdoc />
        public ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            //Correct as written, not unfinished. This is an embedded database with no real
            //asynchronous I/O - System.Data.SQLite's *Async methods are synchronous work
            //behind a task - and a de-queue costs about 6 microseconds, so there is nothing
            //to release the thread for. Deliberately NOT Task.Run, which would only move the
            //work to another pool thread.
            return new ValueTask<IReceivedMessageInternal>(ReceiveMessage(context));
        }
```

- [ ] **Step 5: Build and test**

```bash
dotnet build "Source/DotNetWorkQueue.sln" -c Debug
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" -f net10.0
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true
```

Expected: all green. Nothing calls the new method yet, so behaviour is unchanged.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: async receive on IReceiveMessages and its decorators"
```

---

### Task 2: The async consumer awaits instead of blocking

**Files:**
- Modify: `Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs` (`DoTryAsync`, around line 201)
- Modify: `Source/DotNetWorkQueue/TaskScheduling/SchedulerMessageHandler.cs` (the two `WaitForFreeThread.Wait` sites, around lines 107 and 120)

**Interfaces:**
- Consumes: `ReceiveMessageAsync` from Task 1; `IWaitForEventOrCancelThreadPool.WaitAsync(IWorkGroup)` from PR 1
- Produces: an async consumer that holds no pool thread across a de-queue

- [ ] **Step 1: Await the receive**

In `MessageProcessingAsync.DoTryAsync`, replace:

```csharp
            //this call must block; otherwise, the limitations enforced by the task scheduler will be ignored
            //calling the async method here may result in hundreds of de-queues at once, instead of the N set in the configuration
            var transportMessage = receiveMessage.ReceiveMessage(context);
```

with:

```csharp
            //The de-queue no longer blocks. What used to stop the loop running ahead of the
            //scheduler was this thread parking here; that cap is now enforced by the awaitable
            //throttle in SchedulerMessageHandler, which yields its thread instead of holding it.
            var transportMessage = await receiveMessage.ReceiveMessageAsync(context, _cancelWork.StopWorkToken).ConfigureAwait(false);
```

`_cancelWork` may not be a field on this class. Check first with `grep -n "ICancelWork\|_cancelWork\|StopWorkToken" Source/DotNetWorkQueue/Queue/MessageProcessingAsync.cs`. If it is absent, inject `ICancelWork` through the constructor exactly as `QueueWait` receives it (see `Source/DotNetWorkQueue/Factory/QueueWaitFactory.cs`), and register nothing new — the container already resolves it.

- [ ] **Step 2: Await the throttle**

In `SchedulerMessageHandler`, both call sites currently read:

```csharp
                            _waitingOnFreeThreadCounter.Increment();
                            taskFactory.Scheduler.WaitForFreeThread.Wait(workGroup);
```

Change each to await, and make the enclosing method `async` up the chain as the compiler requires:

```csharp
                            _waitingOnFreeThreadCounter.Increment();
                            await taskFactory.Scheduler.WaitForFreeThread.WaitAsync(workGroup).ConfigureAwait(false);
```

**If making the enclosing method async requires changing a public signature, stop and report it as a BLOCKED status rather than changing it.** That is a design decision, not an implementation one — the controller will rule.

- [ ] **Step 3: Verify the throttle still caps in-flight work**

This is the property the deleted comment was protecting, and the whole reason PR 1 existed. Run the async consumer scenarios, which exercise worker-count limits:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" -f net10.0
```

Expected: all green. `MultiConsumerAsync` and `SimpleConsumerAsync` assert processed counts against configured worker counts; if the cap were lost, they would over-dequeue and fail.

- [ ] **Step 4: Full verification**

```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" -f net10.0
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -f net10.0
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: the async consumer awaits the de-queue and the throttle"
```

---

### Task 3: Redis genuinely async

Redis is the transport the acceptance test exercises, and the one whose synchronous Lua path #161 recorded starving the pool.

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis/IRedisQueueWorkSub.cs`
- Modify: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueWorkSub.cs`
- Create: `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryHandler/ReceiveMessageQueryHandlerAsync.cs`
- Modify: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` (register the new handler)
- Modify: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueReceiveMessages.cs`

**Interfaces:**
- Consumes: `DequeueLua.ExecuteAsync(long unixTime)` and `BaseLua.TryExecuteAsync`, both of which already exist
- Produces: a Redis receive that holds no pool thread across the Lua call or the work-signal wait

- [ ] **Step 1: Make the work-sub awaitable**

`RedisQueueWorkSub.Wait()` blocks on a `ManualResetEventSlim` (field `_waitHandle`, created in `Setup()`). Add an awaitable twin using the **same pattern PR 1 established** in `Source/DotNetWorkQueue/Queue/WaitForEventOrCancel.cs` — read that file first; it is the reference implementation and it took three review rounds to get right.

Specifically, and these are the parts that were got wrong the first time:
- a `TaskCompletionSource<bool>` created with `TaskCreationOptions.RunContinuationsAsynchronously`
- **one lock around every transition** of the wait handle and the source together
- `Reset` replaces the source **only when it is already completed**, or waiters are orphaned
- the disposal and cancellation checks live **inside** that lock, not before it

Add to `IRedisQueueWorkSub`:

```csharp
        /// <summary>
        /// Waits until a notification is received, without blocking a thread.
        /// </summary>
        /// <returns><c>true</c> if notified; <c>false</c> if cancelled.</returns>
        ValueTask<bool> WaitAsync();
```

- [ ] **Step 2: Write the async query handler**

Create `ReceiveMessageQueryHandlerAsync`, mirroring `ReceiveMessageQueryHandler` in the same folder — read it first and copy its structure, changing only the execution call:

```csharp
    internal class ReceiveMessageQueryHandlerAsync : IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage>
```

The interface is `Task<TResult> HandleAsync(TQuery query)` — note it takes **no
cancellation token**, so the method signature is:

```csharp
        public async Task<RedisMessage> HandleAsync(ReceiveMessageQuery query)
```

`Handle` is about 80 lines and **exactly one of them changes.** Copy the whole body
verbatim from `ReceiveMessageQueryHandler.Handle`, then change only this line:

```csharp
                    RedisValue[] result = _dequeueLua.Execute(unixTimestamp);
```

to:

```csharp
                    RedisValue[] result = await _dequeueLua.ExecuteAsync(unixTimestamp).ConfigureAwait(false);
```

Everything else is identical and must stay identical: the poison-message detection, the
`SetMessageAndHeaders` calls (there are three, and they are the context invariants this
plan opens with), the expired-message removal, the two `PoisonMessageException` throws and
the `ReceiveMessageException` wrapper. Do not reimplement or tidy any of it — a difference
between the two handlers is a bug that only shows up on one code path.

The constructor takes the same dependencies as the sync handler; copy it unchanged.

- [ ] **Step 3: Register the handler**

In `RedisQueueInit.cs`, find the line registering `IQueryHandler<ReceiveMessageQuery, RedisMessage>` and add the async registration beside it, following the exact pattern used for whichever async query handler is already registered there. `grep -n "ReceiveMessageQuery" Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` finds the site.

- [ ] **Step 4: Write the async receive loop**

In `RedisQueueReceiveMessages`, add `ReceiveMessageAsync` mirroring `ReceiveMessage` exactly — the same `while (true)`, the same expired-message handling, the same `workSub.Reset()` — replacing only the two blocking calls:

- `GetMessage(context)` becomes an awaited `GetMessageAsync(context)`, a private method mirroring `GetMessage` but awaiting the async handler
- `workSub.Wait()` becomes `await workSub.WaitAsync().ConfigureAwait(false)`

**The three context handlers at the top of the method must be attached exactly as the sync version attaches them:**

```csharp
            context.Commit += _cachedCommit ??= ContextOnCommit;
            context.Rollback += _cachedRollback ??= ContextOnRollback;
            context.Cleanup += _cachedCleanup ??= Context_Cleanup;
```

Omitting these compiles and returns messages, and breaks commit and rollback silently.

Delete the temporary implementation Task 1 added to this class.

- [ ] **Step 4a: Be explicit about what the cancellation token does here**

`ReceiveMessageAsync` takes a `CancellationToken`, but on the Redis path **nothing
downstream accepts one**: `IQueryHandlerAsync.HandleAsync(query)` takes no token,
`DequeueLua.ExecuteAsync(long unixTime)` takes no token, and SE.Redis's
`ScriptEvaluateAsync` is not given one either.

Do **not** plumb a token through those signatures to make it look wired. Redis cancels the
way it already does synchronously, and both mechanisms must survive:

- the `while (true)` loop keeps its `_cancelWork.AnyCancellationRequested()` checks — there
  are two in the sync version, at the top of each iteration and again before the second
  `GetMessage`; keep both
- `workSub.WaitAsync()` observes the work-sub's own cancellation and returns `false`,
  exactly as `Wait()` does today via `_waitHandle.Wait(cts.Token)`

Add a comment on the async method saying this, so the parameter does not read as an
oversight to the next person. The parameter still earns its place: SQL Server and
PostgreSQL take real tokens on their async ADO calls in tasks 5 and 6.

- [ ] **Step 5: Verify against a real Redis**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj" -f net10.0 --filter "TestCategory!=StarvationBaseline"
```

Expected: all green. These cover the consumer, rollback, error and poison paths — the invariants listed at the top of this plan.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: Redis receives asynchronously, Lua call and work signal both"
```

---

### Task 4: The gate

`StarvationBaselineTests` caps the global worker pool to 6, floods 50 concurrent senders, and documents its own expected outcome as **RED** — a permanent diagnostic of synchronous Redis EVAL under pool starvation. An async twin under the identical cap must go **GREEN**.

Differential, same harness, one variable. If the async path still blocks anywhere on the receive chain, the cap starves it exactly as it starves the sync path.

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Concurrency/StarvationBaselineTests.cs`

**Interfaces:**
- Consumes: everything from tasks 1 to 3

- [ ] **Step 1: Read the existing test in full**

Read `StarvationBaselineTests.cs` end to end. Note its `WorkerCap = 6`, `ConcurrentSenders = 50`, `MessagesPerSender = 10`, and that it always restores the original thread-pool settings in a `finally` because those settings are process-global.

- [ ] **Step 2: Add the async twin**

**The existing test genuinely fails.** Its last statement is:

```csharp
Assert.IsNull(caughtException, $"Thread-pool starvation reproduced ...");
```

Starvation reproduces, so `caughtException` is non-null and the assertion fails. It is a
red diagnostic, not a test that passes by expecting an exception. That matters for how the
gate is run — see Step 3.

Add a second test method in the same class with the same pool cap, the same sender flood,
and the same restore-in-`finally`. Two differences from the existing one:

1. It drives the **asynchronous** consumer path rather than the synchronous one.
2. **It gets its own category — `[TestCategory("StarvationAsync")]`, not
   `StarvationBaseline`.** This is load-bearing. The Jenkinsfile runs Redis with
   `--filter "TestCategory!=StarvationBaseline"`, so reusing that category would exclude
   the new test from CI permanently — and the spec's plan is to promote it to a CI gate. A
   separate category is what makes it selectable on its own.

Its assertion is the inverse of the existing one: the flood completes and the messages are
consumed, with no `RedisTimeoutException` anywhere.

Name it `AsyncPath_CappedPool_ConcurrentFlood_DoesNotStarve`, and comment that the two are
a matched pair — the synchronous one staying red is what makes the asynchronous one mean
anything.

- [ ] **Step 3: Run each separately — one command, one expected exit code**

Running both together gives an ambiguous result: `dotnet test` exits non-zero either way,
because the synchronous test fails by design. A single run cannot distinguish "the async
path regressed" from "the baseline is red as intended". So run them apart, and read the
exit codes:

```bash
PROJ="Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj"

# The baseline. MUST fail - exit code non-zero. A pass here means the pool cap
# stopped reproducing starvation, and the gate below proves nothing.
dotnet test "$PROJ" -f net10.0 --filter "TestCategory=StarvationBaseline"
echo "baseline exit=$?   (expected: NON-ZERO)"

# The gate. MUST pass - exit code zero.
dotnet test "$PROJ" -f net10.0 --filter "TestCategory=StarvationAsync"
echo "gate exit=$?       (expected: 0)"
```

**The gate is passed only when the first is non-zero and the second is zero.** Report both
numbers literally, not a summary.

If the *baseline* passes, stop and report that too — it means the harness is no longer
reproducing the condition the gate is measured against, and a green async test would be
meaningless rather than good news.

- [ ] **Step 4: If the async test is RED, stop**

Do not attempt fixes beyond obvious mistakes in the test itself. Report status **BLOCKED** with the full exception and the SE.Redis diagnostic line (the `WORKER: (Busy=…, Min=…)` part is the informative half). A red async test means a blocking call remains on the receive chain, and finding it is a diagnosis job, not an implementation one.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: async twin of the starvation baseline, and it passes"
```

---

## Done means

- `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` clean, and Release with `-p:CI=true` clean
- the full `DotNetWorkQueue.Tests` suite green
- Redis integration tests green excluding the starvation category
- **the async starvation test green while its synchronous twin stays red**
- no production code calls `IReceiveMessages.ReceiveMessage` on the async consumer path any more

## Not in this plan

- SQL Server, PostgreSQL and Memory made genuinely async — tasks 5 to 7, planned after the gate passes
- `ICommitMessage` and the failure paths — PRs 3 and 4
- `IQueueWait` idle back-off — PR 6
- Restoring Redis to `Workers = 4` — PR 7
- Opening the pull request. The PR covers all seven tasks; it is not opened at the end of this plan.

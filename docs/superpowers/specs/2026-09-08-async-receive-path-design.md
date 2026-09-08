# Async receive path — design

Issue: #256. Date: 2026-09-08. Status: approved, not started.

## The problem

`IReceiveMessages` is synchronous only, and `MessageProcessingAsync` calls it. An
"async" consumer is therefore synchronous receive work wrapped in a task, on every
transport.

The cost is not slowness, it is a deadlock shape. #161 recorded
`WORKER: (Busy=215, Free=32552, Min=200, Max=32767)` — every .NET pool thread blocked
in a synchronous Redis call, waiting on completions that themselves needed a pool
thread to run. Past `MinThreads` the pool adds threads at one or two a second, so the
two only unwind when injection catches up, which is past `syncTimeout`. Redis is
currently capped at `Workers = 2` in the integration suite as a workaround for exactly
this.

## What the issue understated

**The synchronous call is deliberate.** `MessageProcessingAsync.DoTryAsync` carries:

```
//this call must block; otherwise, the limitations enforced by the task scheduler will be ignored
//calling the async method here may result in hundreds of de-queues at once, instead of the N set in the configuration
```

A single dequeue thread parked in `WaitForFreeThread.Wait(workGroup)` is what caps
in-flight work at N. Making receive non-blocking removes that cap unless something
replaces it. The interface is the easy part; **the backpressure model is the problem.**

**Receive is not the only blocking call.** Three of these are on the per-message happy
path (throttle, receive, commit); the rest are conditional but land on the same pool:

| where | what blocks |
|---|---|
| `SchedulerMessageHandler` | `WaitForFreeThread.Wait(group)` |
| `MessageProcessingAsync.DoTryAsync` | `ReceiveMessage(context)` |
| `QueueWait.WaitInternal` (idle) | `StopWorkToken.WaitHandle.WaitOne(t)` |
| `ProcessMessageAsync` | `Commit(context)` |
| failure paths | `Rollback`, `MessageFailedProcessing`, poison |
| `HeartBeatWorker` | `Send(context)` |

**The heartbeat shares the .NET pool.** `SmartThreadPoolTaskScheduler.QueueTask` uses
`Task.Factory.StartNew` — the default scheduler. Despite the name it owns no threads.
`HeartBeatThreadPoolConfiguration.ThreadsMax` bounds how many heartbeat tasks are in
flight, not which pool runs them, so a blocking heartbeat consumes a pool thread exactly
as a blocking receive does.

**The idle back-off is the worst of them.** `WaitOne` blocks, so an idle consumer parks
one pool thread per worker doing nothing, for the whole back-off interval.

## Decisions

| decision | choice | why |
|---|---|---|
| goal | genuine end-to-end async I/O | the only option that fixes the deadlock; an async surface over sync work moves the wrapper rather than removing it |
| scope | all six transport interfaces plus `IQueueWait` | they all block the same pool; "touch every transport" is paid once either way |
| backpressure | `IWaitForEventOrCancelThreadPool.WaitAsync` | preserves work groups, which are a required feature; a semaphore would reimplement them and a Channels rewrite would redesign them |
| API shape | extend the existing interfaces | matches `ISendMessages`, which already carries `Send` and `SendAsync` together; compile errors over runtime surprises; breaking change accepted |
| return type | `ValueTask<T>` | an idle consumer polls constantly, allocating four `Task` objects per poll through the decorator chain |
| `ValueTask` safety | `dotnet_diagnostic.CA2012.severity = error` | verified: CA2012 does **not** fire by default here; with it set, both double-consume and `.Result` become build errors. Tests would not catch this — a `ValueTask` over a completed result tolerates misuse |

`CA2012` goes in first, before any `ValueTask` is written, so the twenty decorators are
written under the analyzer rather than audited after.

## Architecture

Three moving parts.

1. **Interfaces gain async twins.** `ValueTask<IReceivedMessageInternal>
   ReceiveMessageAsync(IMessageContext, CancellationToken)` and equivalents. Sync methods
   are untouched; the sync consumer keeps working and deprecating it stays future work.
   Because these are the same interfaces, `ComponentRegistration` is not modified — the
   twenty decorator classes each gain a method.
2. **The throttle becomes awaitable.** `WaitAsync(IWorkGroup, CancellationToken)` beside
   `Wait`. Work-group semantics and the counting in `SmartThreadPoolTaskScheduler` stay
   where they are; only the parking mechanism changes from blocking a thread to yielding
   one. This is what preserves the N-in-flight cap.
3. **The consumer loop stops blocking.** `MessageProcessingAsync`, `ProcessMessageAsync`
   and `SchedulerMessageHandler` await rather than block. The "must block" comment is
   deleted because the thing it protects is now enforced by the awaitable throttle.

## Components

| what | count | change |
|---|---|---|
| core interfaces | 6 | one async method each |
| `IQueueWait` | 1 | `WaitAsync` — see the cancellation contract below |
| decorator classes | 20 | one async method each; **zero DI changes** |
| throttle | 1 + impl | `WaitAsync` |
| consumer loop | 3 classes | await rather than block |
| transports | 6 | implement the async methods |

**Not every transport can be genuinely async, and that is correct rather than
unfinished.** Microsoft.Data.SqlClient, Npgsql and StackExchange.Redis have real async
I/O. SQLite and LiteDb are embedded and do not — `System.Data.SQLite`'s `*Async` methods
are sync work behind a task. Those two do the work synchronously inside the async method
and return a completed result. Explicitly **not** `Task.Run`, which only moves the block
to another pool thread while looking like a fix. SQLite's dequeue is about 6 microseconds
after the perf work, so there is nothing to release the thread for. This must carry a
comment, or someone will "fix" it later.

The measurable win is concentrated in Redis, SQL Server and PostgreSQL — which is where
the evidence of a problem is.

## The `IQueueWait.WaitAsync` contract

`WaitAsync` must **not throw on stop**. Today `WaitInternal` uses
`StopWorkToken.WaitHandle.WaitOne(t)`, which returns normally when the token fires and
lets `Wait` advance the back-off index. `await Task.Delay(t, token)` throws instead, and
that difference is not cosmetic: the caller is

```
_noMessageToProcessBackOffHelper.Value.Wait();
return;
```

so a throw would land in the outer `catch (OperationCanceledException)`, which performs a
rollback and fires a rollback notification — for a poll that found no message and has
nothing to roll back.

The contract to preserve, exactly:

- bind to `ICancelWork.StopWorkToken`, **not** `CancelWorkToken`. The existing code notes
  it deliberately uses only the stop token, and that the ordering between the two is
  enforced by implementation rather than contract
- return normally when the token fires, as `WaitOne` does
- advance `_currentIndex` in both cases, so the back-off ladder behaves identically
- return immediately for a zero wait or an already-signalled token
- `QueueWaitNoOp` stays a no-op

Which means swallowing the cancellation rather than propagating it:

```csharp
try { await Task.Delay(timeToWait, _tokenWorkerCanceled.StopWorkToken).ConfigureAwait(false); }
catch (OperationCanceledException) { /* stop requested — same as WaitOne returning */ }
Interlocked.Increment(ref _currentIndex);
```

## Invariants each async implementation must preserve

Receive is not a pure fetch, and an async rewrite that treats it as one would compile,
return messages, and fail in ways the happy-path tests may not catch. Each async
implementation must preserve everything its sync twin does:

| transport | beyond returning a message |
|---|---|
| all relational | attach `context.Commit +=` / `context.Rollback +=` handlers, transactional and non-transactional variants; store the connection on the context; register the cached cleanup handler |
| Redis | the `while (true)` loop over expired messages, and `workSub.Reset()` on the work signal |
| all | `SetMessageAndHeaders` on the poison path, so the context carries message id, correlation id and headers |

**These need explicit tests**, because the four `ConsumerAsync*` scenarios exercise the
happy path and may pass with a dropped cleanup handler or a lost expired-message retry.
Per transport: commit and rollback handlers fire; headers and message id survive a poison
message; Redis retries an expired message rather than returning it; the work signal is
reset. This is the part of the work most likely to go wrong quietly.

## Error handling

- **Exception types are preserved exactly.** The async consumer already distinguishes
  eight catch sites (`CommitException`, `MessageException`, `PoisonMessageException`,
  `ReceiveMessageException`, `OperationCanceledException`, bare `Exception`). Each async
  method throws what its sync twin throws; transports' existing wrapping moves into the
  async method unchanged.
- **Cancellation already fits.** `TaskCanceledException` derives from
  `OperationCanceledException`, which the loop catches and treats as rollback-and-continue.
- **`ConfigureAwait(false)` throughout**, matching the send path. There is no
  synchronisation context to return to, and capturing one is a deadlock source.
- **Decorators are `async` throughout** rather than some returning a pre-computed value,
  so an exception thrown before the first await is captured and surfaced on await
  consistently. That is what keeps the eight catch sites behaving identically.
- **`IsBlockingOperation` is left alone** and gains no async twin. It describes the sync
  method; once receive is awaitable the question is not meaningful, and an async twin
  would invite decisions based on it.

## Testing

**The acceptance test already exists and is currently expected to fail.**
`StarvationBaselineTests` caps the global worker pool to 6, floods 50 concurrent senders,
and documents its own expected outcome as RED — a permanent diagnostic of synchronous
Redis EVAL under pool starvation, excluded from CI via `TestCategory!=StarvationBaseline`.

An async twin of that test, under the identical cap, **must go GREEN while the sync one
stays RED**. Differential, same harness, one variable. It cannot pass by accident: if the
async path still blocks anywhere on the receive chain, the cap starves it exactly as it
starves the sync path today.

**Most regression coverage is free.** `ConsumerAsyncShared`, `ConsumerAsyncErrorShared`,
`ConsumerAsyncPoisonMessageShared` and `ConsumerAsyncRollBackShared` already drive the
async consumer across all six transports, and already cover the error, poison and
rollback paths that must keep behaving identically.

**New unit tests** are needed for the twenty decorators' async methods and for the
awaitable throttle — cancellation mid-wait, work-group limits still enforced, no lost
wake-ups. Only 7 decorator tests exist today against 20 decorators, so this closes a gap
rather than mirroring one. The Codecov patch gate applies.

**Proposal beyond the issue:** promote the starvation test from manual diagnostic to CI
gate. It is excluded because it caps the process-global thread pool, which would wreck the
parallel suite — an argument for isolation, not exclusion. A Jenkins stage running only
`TestCategory=StarvationBaseline` in its own container takes about a minute and sits far
below the 10-minute critical path. Without it the async path could silently regress to
sync-over-async and every existing test would still pass.

## Delivery

Extending the interfaces means each one lands complete — the moment `ReceiveMessageAsync`
appears, all six transports stop compiling. That rules out core-first, but it splits
cleanly by interface.

| PR | surface |
|---|---|
| 1 | CA2012 + awaitable throttle. No interface changes, breaks nothing — but see below; this is the subtlest PR, not the warm-up |
| 2 | **`IReceiveMessages`** — 4 decorators, 6 transports, consumer loop. The starvation twin goes green here |
| 3 | `ICommitMessage` — 3 decorators, 6 transports |
| 4 | failure paths — `IRollbackMessage`, `IReceiveMessagesError`, `IReceivePoisonMessage` — 10 decorators |
| 5 | `ISendHeartBeat` — 3 decorators |
| 6 | `IQueueWait` idle back-off — core only |
| 7 | Redis `Workers = 4` restored, reverting the cap shipped as a workaround |

**PR 2 is the one that matters and the one to stop at if it fails.** If the starvation
twin cannot be made green there, the design is wrong and one PR found out rather than
seven.

**One release, gated.** Nothing ships until PR 7 lands, following #162's precedent of
holding three relational transports for a single release. A consumer should never see a
half-async receive path.

**Breaking change.** Added interface members break any external transport. The changelog
needs a ⚠️ bullet: one line, consumer-facing.

## PR 1 is harder than its size suggests

`WaitForEventOrCancel` wraps `ManualResetEventSlim`, and **`ManualResetEventSlim` has no
`WaitAsync`**. Making the throttle awaitable is therefore not a signature change; it needs
an async manual-reset-event built on `TaskCompletionSource` with
`RunContinuationsAsynchronously`, where `Reset()` swaps in a fresh TCS.

The hazard is the Set/Reset interleaving. Reset between a waiter's check and its await and
that waiter sleeps until the *next* Set — which here means a worker that stops dequeuing
until something else happens to free a thread. That is a hang, and an intermittent one,
which is the worst kind to find in an integration suite.

It belongs first regardless: small, self-contained, no interface changes, and testable in
isolation with no transport involved. Tests must cover the interleavings specifically —
Set during a pending wait, Reset during a pending wait, Set-then-Reset before the waiter
resumes, cancellation mid-wait, and many waiters released by one Set — rather than only
the happy path.

**Work groups themselves are not being reworked and need no changes.** The per-group
dictionary, the counting in `WorkGroupWithItem`, and the configured concurrency level are
sound and stay as they are. Only the wait primitive underneath gains an async path.

## Done means

- the async starvation test green while its sync twin stays red
- the four `ConsumerAsync*` scenarios green on all six transports
- Redis back to `Workers = 4` without timeouts
- release notes carrying the breaking-change warning

## Non-goals

- Deprecating or removing the synchronous API. This unblocks that work; it does not do it.
- Raising the analyzer level globally. One rule is enabled, not a cleanup project.
- Reworking work groups. They are a required feature and are preserved as-is.

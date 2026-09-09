# Async receive path — design, revision 1

Issue: #256. Date: 2026-09-08. Status: **supersedes the threading analysis in
`2026-09-08-async-receive-path-design.md`.** Read this first; that document's "The problem"
and "What the issue understated" sections contain a false premise, corrected below.

PR 1 (`WaitForEventOrCancel.WaitAsync`) is merged. PR 2 task 1 (`ReceiveMessageAsync` on the
interface and its four decorators) is committed on `feat-256-async-receive` and stands — it is
purely additive and correct regardless of what the consumer does with it. Tasks 2-7 are **not
started** and are re-scoped here.

## Why this revision exists

The original spec was written from reading, not measurement, and two of its load-bearing claims
are wrong. Both were caught only after an implementation attempt OOM-killed the machine. Every
claim below is either measured or cited to a file and line.

## Correction 1 — the receive does not run on a thread-pool thread

The original spec says a blocking receive "consumes a pool thread exactly as a blocking heartbeat
does", and builds a six-row table of blocking calls "on the same pool". That is false for the
first three rows.

`Worker.cs:79` and `PrimaryWorker.cs:85` are both:

```csharp
WorkerTask = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);
```

`LongRunning` gives each worker a **dedicated** thread. `MainLoop` calls
`MessageProcessing.Handle()`, and because `Handle()` runs synchronously until its first suspending
await, the receive, the idle back-off (`_noMessageToProcessBackOffHelper.Value.Wait()`) and the
scheduler throttle (`WaitForFreeThread.Wait(group)`, reached through the user lambda) all execute
on that dedicated thread.

**Measured.** A probe on the sync `MessageQueueReceive.ReceiveMessage` recording
`Thread.CurrentThread.IsThreadPoolThread`, run under the Memory `SimpleConsumerAsync` scenarios:

```
      7 pool=False id=25 name=.NET Long Running Task
      7 pool=False id=24 name=.NET Long Running Task
      6 pool=False id=26 name=.NET Long Running Task
      5 pool=False id=22 name=.NET Long Running Task
      2 pool=False id=23 name=.NET Long Running Task
   pool=True count: 0 / 27
```

Zero of 27 receives on a pool thread. Five distinct dedicated threads, one per worker.

### What *is* on the pool

| where | on the pool? | why |
|---|---|---|
| `MessageProcessingAsync.DoTryAsync` → `ReceiveMessage` | **no** | dedicated `LongRunning` worker thread (measured) |
| `QueueWait.WaitInternal` idle back-off | **no** | same call stack, same dedicated thread |
| `SchedulerMessageHandler` → `WaitForFreeThread.Wait` | **no** | same call stack, same dedicated thread |
| message processing work | **yes** | `taskFactory.TryStartNew` → scheduler → `Task.Factory.StartNew` |
| `HeartBeatWorker.Send` | **yes** | `SmartThreadPoolTaskScheduler.QueueTask` → `Task.Factory.StartNew` |
| `Commit` / `Rollback` | **yes, conditionally** | they run in the continuation after the user's async handler suspends |

So the pool pressure in `WORKER: (Busy=215, Min=200)` comes from processing tasks, heartbeats and
neighbouring Jenkins stages — not from receives.

### What the async receive is therefore worth

**Not** "it frees a pool thread on our side" — there was no pool thread to free.

What it is worth: a synchronous `IDatabase.ScriptEvaluate` in StackExchange.Redis is
sync-over-async internally, and **needs a pool thread to run the completion** that satisfies its
own blocking wait. Under pool pressure that completion is delayed past `syncTimeout`, which is the
timeout recorded in #161 and in the `project_redis_3x_sync_concurrency_limit` note — sends timing
out *even when the pool is not starved*, because the contention is on completions, not threads.
Awaiting `ScriptEvaluateAsync` removes the need for a pool thread to satisfy a blocking wait at
all.

This narrows the value sharply: **the win is in the transport's use of SE.Redis's async API.** The
core async plumbing exists only to make that reachable without a sync-over-async bridge of our own.

## Correction 2 — the cap lives in the worker loop, and the old comment was right

The comment the original spec quoted is load-bearing, and its estimate was conservative:

```
//this call must block; otherwise, the limitations enforced by the task scheduler will be ignored
//calling the async method here may result in hundreds of de-queues at once, instead of the N set in the configuration
```

`MainLoop` is `while (!ShouldExit) { _pauseEvent.Wait(); MessageProcessing.Handle(); }` and
`Handle()` is **`async void`**. The loop therefore iterates the moment `Handle()` hits its first
*suspending* await. Today the blocking receive is what prevents that. The scheduler throttle sits
*downstream* of the receive inside the same synchronous run, so it cannot pace the loop once the
receive suspends.

**Measured.** Task 2 step 1 as originally planned, with a Memory transport whose
`ReceiveMessageAsync` genuinely suspends (`await Task.Yield()` — the shape Redis takes after task
3), instrumented with an interlocked in-flight counter, running `SimpleConsumerAsync` at
`workerCount: 7`:

> **peak concurrent in-flight de-queues: 15,910,404**

The test host was OOM-killed and took the WSL instance with it. Three orders of magnitude worse
than the comment's "hundreds".

### Two shapes that do not fix it

- **Await a throttle before the receive.** Bounds de-queues, but `async void` means the throttle's
  own suspension also returns to the loop, which then spins allocating waiters. Unbounded memory
  and a hot loop — the same defect wearing a different hat.
- **Reuse the scheduler's free-thread event as the gate.** `WaitForEventOrCancel` starts **open**
  (`new ManualResetEventSlim(true)`, `WaitForEventOrCancel.cs:51`) and `TaskScheduler.cs:373-388`
  only `Reset`s it when the scheduler is full. The plain async consumer never starts a scheduler,
  so the gate is permanently open and caps nothing there.

### The shape that does

The **loop** must be what waits. `Handle()` has to hand `MainLoop` something that completes when
the processor is ready for the *next* de-queue — the same point at which `Handle()` returns to the
loop today — and because `MainLoop` owns a dedicated thread it can wait on that synchronously, at
zero pool cost and with an identical threading profile to today.

That means changing the public `IMessageProcessing`, which today is two events, `AsyncTaskCount`
and `void Handle()`. This PR already accepts breaking changes.

Supporting facts, verified: one `MessageProcessingAsync` per worker (`Worker.cs:74`); `IWorkGroup`
is registered by default as `WorkGroupNoOp` (`ComponentRegistration.cs:177`) and replaced
per-container for the scheduler consumer (`QueueContainer.cs:231-264`), so a work-group-aware gate
needs no new registration; the work group is per-queue, fixed at `CreateConsumerQueueScheduler`
and stored once at `Scheduler.cs:62`.

## Correction 3 — the existing starvation oracle tests the wrong path

`StarvationBaselineTests.StarvationBaseline_CappedPool_SyncEvalFlood_FailsWithTimeout` caps the
pool at 6 and floods with 50 concurrent senders calling `producer.Send(msg)`. It is a **send**-path
reproduction. It contains no consumer.

An async *receive* implementation cannot change its outcome. Task 4 as planned — run that test
against an "async twin" as the go/no-go gate — would have proved nothing about this work.

Compounding this: the send path **already has** an async implementation
(`SendMessageCommandHandlerAsync` → `EnqueueLua.ExecuteAsync` → `ScriptEvaluateAsync`), so the
oracle is measuring the sync API of a path that already has an async alternative.

The receive path already has async Lua available too — `DequeueLua.ExecuteAsync`
(`DequeueLua.cs:87`) on `BaseLua.TryExecuteAsync` → `db.ScriptEvaluateAsync`
(`BaseLua.cs:114-135`). What is missing is a path from the consumer down to it. That is precisely
the gap recorded in `project_sync_api_deprecation_blocked`, and it is the entire remaining job.

**The gate must be a receive-side reproduction, and it does not exist yet.** It has to be written
before it can gate anything.

## Revised delivery

Task 1 is done and keeps its value independently. What follows is re-scoped.

| task | scope | status |
|---|---|---|
| 1 | `ReceiveMessageAsync` on the interface + 4 decorators + 6 temporary transport bodies | **done** (`985080c6`, `1b9772a5`) |
| 2 | Move the de-queue cap into the worker loop: `IMessageProcessing` returns something `MainLoop` waits on, completing when ready for the next de-queue | **re-specified, not started** |
| 3 | Redis `ReceiveMessageQueryHandler` + work-sub genuinely async, on the existing `DequeueLua.ExecuteAsync` | unchanged in intent |
| 4 | **New:** a receive-side starvation reproduction, plus the cap assertion below. Replaces the send-side gate | **rewritten** |
| 5-7 | SqlServer / PostgreSQL / Memory async receive | **re-scope or drop — see below** |

### Task 4 must assert two things

1. **Starvation, receive-side.** A capped pool with a consumer under load, showing the sync receive
   timing out and the async receive not. Needs writing from scratch; the send-side test is not a
   template for it beyond the pool-capping technique.
2. **The cap holds.** Peak concurrent in-flight de-queues never exceeds the configured worker
   count, driven through a transport whose receive genuinely suspends. Nothing in the repo can
   currently catch this: every transport body task 1 added returns a completed `ValueTask`, so the
   15.9M runaway passes all 1109 unit tests and the full Memory integration suite.

   This test must fail *fast* rather than by exhausting memory — bound the run and abort once peak
   exceeds a small multiple of the worker count. The unbounded version is what killed the machine.

### Tasks 5-7 need justification they do not currently have

The original reasoning was "all six transports for consistency". Given correction 1, a relational
transport's sync ADO receive also runs on a dedicated thread and also frees no pool thread, so the
benefit reduces to whichever of them has SE.Redis's sync-over-async completion problem. That is a
claim about `Microsoft.Data.SqlClient` and `Npgsql`, and nobody has measured it.

Recommendation: **hold 5-7** until the task 4 gate exists and can be pointed at each transport.
Ship Redis, then extend on evidence. This also keeps the breaking-change surface smaller.

## Invariants unchanged from the original spec

The sections `The IQueueWait.WaitAsync contract`, `Invariants each async implementation must
preserve`, `Error handling` and `Non-goals` in the original document are unaffected by these
corrections and still apply.

## What this revision costs

PR 2 gets smaller in transports and larger in the consumer. The `IMessageProcessing` change is a
new breaking change beyond the interface additions already accepted, and it touches the sync
consumer path as well as the async one, so `MessageProcessing` (the sync sibling) needs a
compatible shape even though nothing about it changes behaviourally.

## Process note

Two rulings on task 2 were wrong, and both failed the same way: I asserted a property of code I had
not fully read, then reasoned forward from it. What held up in this project was measured —
the 15.9M counter, the 0/27 thread probe, the mechanical decorator diff. What did not hold up was
reasoned. For the remaining tasks, the threading and backpressure claims get a probe before they
get a design.

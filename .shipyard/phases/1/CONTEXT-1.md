# Phase 1 Context: Design Decisions

Captured during `/shipyard:plan 1` discussion. These are the user's locked-in decisions for the TaskScheduler lock contention fix; downstream agents (researcher, architect, builder) must honor them.

## Repository

**Working directory for Phase 1:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (sibling repo, not this DNQ repo).

**File of primary focus:** `Source/TaskSchedulerJobCountSync.cs` (plus any new unit test project files).

## Key Decisions

### 1. Outbound message queue shape

**Decision:** Use `NetMQQueue<SetCountMsg>` where `SetCountMsg` is a `readonly record struct SetCountMsg(int Port, long Count)`.

**Why:** Type-safe, minimal allocation, readable. The poller callback formats frames from the struct rather than shuffling pre-formatted string arrays.

**Implementation note:** The struct lives in the same file as `TaskSchedulerJobCountSync` (or a small sibling file — architect's call). Keep it `internal`.

### 2. Concurrency regression test strategy

**Decision:** Real JobCountSync + real NetMQPoller; N producer threads (8–16) hammer `Increase`/`Decrease` for a few seconds. Assert no deadlock within generous timeout and final count equals expected delta.

**Why:** The bug is a real concurrency bug — a mock-based test would validate an abstraction that doesn't exist in production. We want the test to fail loudly if someone reintroduces lock contention.

**Implementation notes:**
- Test must run on an ephemeral NetMQ bus (loopback / inproc / free TCP port, no external beacons required if avoidable).
- Use a generous timeout (e.g., 30 seconds) — the test should never approach it under normal operation. The timeout is a deadlock detector, not a performance assertion.
- Final count assertion: `expectedDelta = increments - decrements`. Use `Interlocked` counters in the test harness.
- Run the test 5 times in a loop locally before declaring Phase 1 done — flakes here are blockers.

### 3. Start() semantics — async-friendly refactor

**Decision:** Refactor `Start()` to kick off the poller on a background thread and return quickly, rather than blocking forever (current behavior).

**Why:** Callers currently need to dedicate a thread to `Start()` or wrap it in `Task.Run`. An async-friendly `Start()` that returns after the bus is ready and the poller is running is easier to consume and aligns with .NET conventions.

**Behavioral change acknowledgement:** This IS a behavioral change. Existing callers that launch `Start()` on a dedicated thread will still work (the method just returns sooner), but callers that depend on `Start()` blocking until `Dispose` will break. Search the sibling repo and DNQ repo for existing usages and document them in RESEARCH.md.

**Compatibility approach:**
- Keep the `Start()` method name and signature (`void Start()`).
- Internal behavior: after bus startup + beacon wait + initial broadcast publish + poller launch on a dedicated thread, return.
- The dedicated poller thread owns `_actor` for its entire lifetime; it is joined in `Dispose`.
- Remove the `while(_running) Sleep(100)` hot-wait in `Dispose` in favor of `Thread.Join(timeout)` on the poller thread (or `NetMQPoller.Stop()` → thread exit).
- Preserve the 1100ms `Thread.Sleep` beacon grace period for now — the user considered removing it and chose not to.

### 4. Public API surface

**Decision:** Do not change `ITaskSchedulerJobCountSync` signatures, return types, or the `RemoteCountChanged` event. Only internal implementation changes. `Start()` semantic change (above) is the one exception — it's behavioral, not a type signature change.

### 5. Initial broadcast routing (PROJECT.md deviation, resolved during plan verification)

**Decision:** The initial `BroadCast` message sent once during `Start()` does NOT need to go through the `NetMQQueue` path. It is sent directly via `_actor.SendMoreFrame(...)` before the poller assumes ownership of the socket.

**Why:** PROJECT.md said *"Start publishes the initial broadcast through the queue path as well,"* but that was written before we knew the queue would be `NetMQQueue<SetCountMsg>` (a count-specific type). The initial broadcast runs exactly once at startup, in a single-threaded context, before there is any contention to eliminate. Routing it through the queue would require widening the struct to a discriminated-union `OutboundMsg`, adding complexity for zero concurrency benefit.

**Builder instruction:** Send the initial `BroadCast` directly; then start the poller; then `Increase`/`Decrease` go through the `NetMQQueue<SetCountMsg>` as planned. The `_lockSocket` still disappears entirely — this resolution does not resurrect it.

### 6. Out-of-scope for Phase 1 (from PROJECT.md non-goals)

- Beacon / discovery protocol changes.
- `ITaskSchedulerBus` redesign.
- Removing the 1100ms beacon sleep.
- Public API type changes.
- Throughput benchmarks.

## Verification Approach for Phase 1

1. `dotnet build -c Debug` and `dotnet build -c Release -p:CI=true` clean in the TaskScheduler repo.
2. `dotnet test` — concurrency regression test + state consistency test + lifecycle test all green.
3. Run the concurrency regression test 5 times in a loop locally to shake out flakiness.
4. Phase 1 is NOT done until all three tests pass consistently and `_lockSocket` has been removed from the class (grep to confirm).

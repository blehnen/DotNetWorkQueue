# Review: Plan 1.2 (Poller infrastructure refactor)

Two-stage review by separate reviewer agent dispatches.

## Verdict: PASS (with minor suggestions)

Stage 1 (spec compliance) verdict: **PASS**. Stage 2 (code quality) verdict: **MINOR_ISSUES** — 0 critical, 3 important (advisory, non-blocking), 6 minor. Nothing blocks Wave 3.

## Stage 1 — Spec Compliance

### Checklist results

**Task 1 — Scaffold**
- `_poller` field near `_actor`: PASS
- Poller construction in `Start()` after `GetHostAddress` round-trip: PASS (Task 3 subsumed the empty-ctor form, expected)
- `_poller?.Dispose()` in `Dispose(bool)` before `_actor?.Dispose()`: PASS

**Task 2 — Inbound on poller**
- `OnActorReady(object, NetMQActorEventArgs)` — signature corrected from plan's `NetMQSocketEventArgs`. Acceptable deviation because `NetMQActor.ReceiveReady` is actually typed that way.
- Body reads first frame via `_actor.ReceiveFrameString(Encoding.ASCII)`: PASS
- `int.TryParse`/`long.TryParse` guards preserved: PASS
- `_actor.ReceiveReady += OnActorReady` subscribed: PASS
- `_poller.Run()` replaces old `while(!_stopRequested) ProcessMessages()`: PASS
- Initial `BroadCast` sent directly on caller thread before poller construction (CONTEXT-1.md #5): PASS
- `_stopRequested`, `_running`, `ProcessMessages()` all deleted: PASS
- `while(_running) Sleep(100)` hot-wait gone: PASS
- `_poller?.Stop()` called before `_actor?.Dispose()`: PASS
- `SocketException` 10035/10054 swallow preserved: PASS

**Task 3 — Outbound queue + `_lockSocket` removal**
- `_outbound` field, construction, and `ReceiveReady` wiring: PASS
- Poller initializer `{ _actor, _outbound }`: PASS
- `OnOutboundReady` drains via `TryDequeue(out, TimeSpan.Zero)` and emits 4-frame wire format: PASS
- `CultureInfo.InvariantCulture` used for numeric formatting: PASS
- `IncreaseCurrentTaskCount` lock-free with `Interlocked.Increment` + `_outbound?.Enqueue`: PASS
- `DecreaseCurrentTaskCount` symmetric: PASS
- `GetCurrentTaskCount` lock-free `Interlocked.Read + Values.Sum()`: PASS
- `_outbound?.Dispose()` positioned between `_poller.Dispose()` and `_actor.Dispose()`: PASS
- **`_lockSocket` field and all `lock(_lockSocket)` blocks deleted**: PASS — `grep -c _lockSocket = 0`

**Cross-task**
- Public API on `ITaskSchedulerJobCountSync` untouched: PASS
- `SetCountMsg` declared as `internal readonly record struct`: PASS
- Only `TaskSchedulerJobCountSync.cs` modified (no new files): PASS

### Headline regression sentinel

**`_lockSocket` = 0** — Phase 1 success criterion #1 met.

### Stage 1 deviations

Only the `NetMQActorEventArgs` signature correction, acknowledged and acceptable (the plan text was wrong about the NetMQActor API; the actual NetMQ type is what compiled). Captured as micro-lesson so PLAN-1.3 doesn't rehash it.

## Stage 2 — Code Quality

### Critical findings

None.

### Important findings (advisory, non-blocking)

1. **`Dispose()` cross-thread expectation.** `Start()` blocks on `_poller.Run()`, so `Dispose()` must be called from a different thread. `NetMQPoller.Stop()` is thread-safe by NetMQ contract and waits for `Run()` to return. The design is correct, but a one-line maintainer comment on `Dispose` noting the cross-thread expectation would prevent future "simplifications" that break it. Deferred as optional polish.

2. **`OnActorReady` exception scope.** The try/catch wrapper is present and mirrors the old `ProcessMessages` wrapper. An exception thrown from a `RemoteCountChanged?.Invoke` user handler would be caught and logged as "Failed to handle NetMCQ commands", which is misleading. **This is pre-existing** — the old code had the same flaw — not introduced by this refactor.

3. **`_outbound?.Enqueue` null-guard.** Genuine pre-Start protection. If `Increase`/`Decrease` is called before `Start()` completes initialization, the wire broadcast is silently dropped while `_currentTaskCount` still updates via `Interlocked`. The next successful broadcast carries the accumulated value. Works as designed; a one-line comment explaining why the guard is there would prevent someone "fixing" it with a throw. Deferred as optional polish.

### Minor findings

- **`GetCurrentTaskCount` snapshot semantics.** Two-read non-atomicity exists in both old and new code. Old `lock(_lockSocket)` didn't provide a true snapshot across `_currentTaskCount` + dictionary either. Observationally equivalent — no regression.
- `ContainsKey` + indexer assignment in `OnActorReady` SetCount handler is a double-lookup. Could be simplified to `_otherProcessorCounts[key] = value;` (atomic AddOrUpdate via the concurrent indexer). Pre-existing code pattern not introduced here.
- `out var temp` could be inlined in the RemovedNode handler.
- Class XML doc could add `<remarks>` noting that `Start()` is a blocking call.
- No `GC.SuppressFinalize(this)` in `Dispose()` — pre-existing, not a refactor regression.
- `_poller.Dispose()` correctly waits for `Run()` to exit before releasing (NetMQ contract), so the subsequent `_outbound`/`_actor` disposal is safe. Ordering is correct.

### Positive findings

- **Clean elimination of `_lockSocket`, `_stopRequested`, `_running`, and the `Sleep(100)` spin.** The poller thread model is materially simpler and correct.
- `NetMQQueue<SetCountMsg>` as the producer→poller handoff is idiomatic NetMQ and removes cross-thread socket access entirely.
- `SetCountMsg` record struct is a lightweight value type that fits the queue use case perfectly.

## Decision

PLAN-1.2 is **APPROVED**. All findings are minor or advisory. The headline deliverable (`_lockSocket = 0`) is in place. Wave 3 (PLAN-1.3) is unblocked — it will add the async-friendly `Start()` and bounded `Dispose` cleanup, which may naturally address the Stage-2 "important" finding #1 (cross-thread expectation comment).

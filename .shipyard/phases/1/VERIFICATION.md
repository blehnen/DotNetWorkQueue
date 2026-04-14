# Verification Report — Phase 1 (Plan Review)

**Phase:** 1 — TaskScheduler Lock Contention Fix + Unit Tests
**Date:** 2026-04-14
**Type:** plan-review (Mode A — pre-execution)
**Sibling repo under plan:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`

## Scope Under Review

| Plan | Wave | Dep | Tasks | Files Touched |
|---|---|---|---|---|
| PLAN-1.1 | 1 | — | 2 | `TaskSchedulerJobCountSync.cs`, new `NetMqQueueApiProbeTests.cs`, new `SetCountMsgTests.cs` |
| PLAN-1.2 | 2 | 1.1 | 3 | `TaskSchedulerJobCountSync.cs` (all 3 tasks, sequential) |
| PLAN-1.3 | 3 | 1.2 | 2 | `TaskSchedulerJobCountSync.cs`, `TaskSchedulerMultiple.cs` |
| PLAN-2.1 | 4 | 1.3 | 3 | 3 new test files in Tests project |

Plan count per task policy (≤3): all plans compliant. Total tasks = 10.

## Results

### Coverage of Phase 1 requirements (ROADMAP.md §Phase 1 + PROJECT.md Workstreams 1–2)

| # | Requirement (source) | Status | Evidence (plan + file) |
|---|---|---|---|
| 1 | Replace `_lockSocket` + `TryReceiveFrameString(10ms)` with `NetMQPoller` that owns `_actor` on one dedicated thread | PASS | PLAN-1.2 Task 2 removes `ProcessMessages`/`while(!_stopRequested)` and wires `_actor.ReceiveReady += OnActorReady` onto `_poller`. PLAN-1.3 Task 1 moves `_poller.Run()` onto a dedicated `Thread _pollerThread`. |
| 2 | Outbound `SetCount` routed through `NetMQQueue<SetCountMsg>` (struct tuple) | PASS | PLAN-1.1 Task 2 introduces `internal readonly record struct SetCountMsg(int Port, long Count)`. PLAN-1.2 Task 3 adds `_outbound = new NetMQQueue<SetCountMsg>()`, `OnOutboundReady` drain, `Enqueue` from Increase/Decrease. Honors CONTEXT-1.md decision #1 exactly. |
| 3 | `GetCurrentTaskCount` = `Interlocked.Read(ref _currentTaskCount) + _otherProcessorCounts.Values.Sum()`, no lock | PASS | PLAN-1.2 Task 3 step 6 rewrites `GetCurrentTaskCount` to that exact form and strips the `lock`. |
| 4 | `Interlocked.Increment/Decrement` synchronous in caller; wire publish deferred to poller | PASS | PLAN-1.2 Task 3 steps 4–5 — `Interlocked.Increment` returns the new value, then `_outbound.Enqueue`. No lock. Return value preserved. |
| 5 | `Start` publishes initial broadcast through queue path | PARTIAL | PLAN-1.2 Task 2 step 2 and PLAN-1.3 Task 1 Phase B both perform the initial AddedNode broadcast via **direct** `_actor.SendMoreFrame(...).SendFrame(_hostAddress);` on the caller thread BEFORE the poller owns the actor, not via `_outbound.Enqueue`. See Gap A. PROJECT.md says "publishes the initial broadcast through the queue path as well." |
| 6 | `Dispose` stops poller cleanly, disposes `_actor`; remove `while(_running) Sleep(100)` hot-wait | PASS | PLAN-1.2 Task 2 step 3 deletes the hot-wait. PLAN-1.3 Task 2 step 1 adds `_poller?.Stop(); _pollerThread?.Join(TimeSpan.FromSeconds(5)); _outbound?.Dispose(); _actor?.Dispose(); _poller?.Dispose();` with bounded timeout. |
| 7 | Preserve public API: `ITaskSchedulerJobCountSync` signatures / return values / `RemoteCountChanged` unchanged | PASS | No plan edits `ITaskSchedulerJobCountSync.cs`. Increase/Decrease return `newValue` (post-increment) — matches current semantics per RESEARCH.md 2.5. `RemoteCountChanged` is untouched. |
| 8 | Concurrency regression test: N threads, no deadlock, final count == expected delta | PASS | PLAN-2.1 Task 1 — real `TaskSchedulerJobCountSync` + real `TaskSchedulerBus`, 12 threads × 5000 iters, 30 s deadlock detector, asserts `expectedDelta == sync.GetCurrentTaskCount()`. Honors CONTEXT-1.md decision #2. |
| 9 | State consistency test (scripted remote `SetCount` → `GetCurrentTaskCount` aggregate) | PASS | PLAN-2.1 Task 2 — 2-node bus, scripted A-side Increase/Decrease, deadline-poll B until converged. Acceptable variant of the fake-bus option. |
| 10 | Lifecycle test: Start → operate → Dispose completes without hanging | PASS | PLAN-2.1 Task 3 — 10 s `Task.WhenAny` deadline on `Dispose()`. |
| 11 | All pre-existing tests in TaskScheduler repo continue to pass | PASS | Every PLAN-1.2 / PLAN-1.3 task's `<done>` requires the full existing test project green. |
| 12 | `dotnet build -c Debug` and `dotnet build -c Release -p:CI=true` both clean | PASS | PLAN-1.3 Task 2 verify command runs both Debug and Release CI builds; PLAN-2.1 Task 3 verify re-runs Release CI. |
| 13 | `_lockSocket` fully removed (CONTEXT-1.md verification #4) | PASS | PLAN-1.2 Task 3 step 8 deletes the field and every remaining `lock(_lockSocket)`; `<done>` requires zero grep hits. |
| 14 | Async-friendly `Start()` returns quickly (CONTEXT-1.md decision #3) | PASS | PLAN-1.3 Task 1 spawns `_pollerThread` and returns; preserves 1100 ms beacon sleep per the decision note. |
| 15 | 1100 ms beacon grace sleep preserved | PASS | PLAN-1.3 Task 1 sketch contains `Thread.Sleep(1100); // beacon grace — kept per CONTEXT-1.md decision #3`. |

### Wave / dependency ordering

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 16 | PLAN-W.N depends only on earlier waves | PASS | 1.1 deps=[]; 1.2 deps=[1.1]; 1.3 deps=[1.2]; 2.1 deps=[1.3]. Strict linear chain. |
| 17 | No intra-wave file collisions | PASS | Every wave contains exactly one plan. No parallel plans in any wave → no collision possible. |
| 18 | No forward references within a plan | PASS | PLAN-1.2 Task 3 consumes `SetCountMsg` which exists after PLAN-1.1 Task 2. PLAN-1.3 references `_outbound` and `_poller` established in PLAN-1.2. |
| 19 | Tasks within a single plan sequenced so later tasks see earlier task state | PASS | PLAN-1.2 is the tightest case: 3 tasks all edit `TaskSchedulerJobCountSync.cs` in order field-scaffold → inbound-refactor → outbound-refactor. Legal because same-plan tasks run sequentially. |

### Acceptance criteria testability

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 20 | Every `<verify>` is a runnable `dotnet` command | PASS | All 10 verify blocks are `dotnet build ...` or `dotnet test ... --filter ...`. Syntactically well-formed. |
| 21 | `<done>` gates are objective and grep/build measurable | PASS | "zero `_lockSocket` grep hits", "0 errors, 0 warnings", "test passes 5 consecutive times", "no `while` loop references `_running`". |
| 22 | CONTEXT-1.md decisions honored | PASS | Decision #1 (`NetMQQueue<SetCountMsg>` struct): PLAN-1.1 Task 2. Decision #2 (real-poller concurrency test): PLAN-2.1 Task 1 uses real bus + real sync. Decision #3 (async-friendly Start): PLAN-1.3 Task 1. Decision #4 (public API unchanged): verified above. Decision #5 (out-of-scope list): no plan touches beacon protocol / ITaskSchedulerBus / beacon sleep duration / API types / benchmarks. |

## Gaps

**Gap A — Initial broadcast does not go through the queue path.** PROJECT.md requirement explicitly states: *"`Start` publishes the initial broadcast through the queue path as well."* PLAN-1.2 Task 2 step 2 and PLAN-1.3 Task 1 Phase B both do the AddedNode broadcast as a direct `_actor.SendMoreFrame(...).SendFrame(_hostAddress)` on the caller thread, not via `_outbound.Enqueue`. This is a **minor requirement deviation**, not a correctness failure — the direct send is safe because the poller does not yet own `_actor` at that moment. However, the plans should either (a) enqueue the initial broadcast onto `_outbound` and let `OnOutboundReady` drain it once the poller starts, or (b) document the deviation in CONTEXT-1.md as an intentional "broadcasts are simple enough to send directly pre-poller-handover". **Severity: LOW.**

**Gap B — Initial AddedNode broadcast frame shape does not match `SetCountMsg`.** Related to Gap A: the initial broadcast is an `AddedNode` + `_hostAddress` string, not a `SetCount` + port/count pair. The `NetMQQueue<SetCountMsg>` queue path cannot literally carry an AddedNode broadcast because `SetCountMsg` only models `(Port, Count)`. To honor the PROJECT.md wording, the queue would need to be `NetMQQueue<OutboundMsg>` where `OutboundMsg` is a discriminated union — that's a **scope expansion** beyond CONTEXT-1.md decision #1. **Recommendation: architect should clarify whether PROJECT.md's wording is normative (requires widening the struct) or aspirational (direct send is acceptable).**

**Gap C — `OnActorReady` single-frame-per-callback risk.** PLAN-1.2 Task 2 step 1 says the callback does `_actor.ReceiveFrameString(Encoding.ASCII)` and processes ONE message per `ReceiveReady` event. NetMQ `ReceiveReady` fires when the socket becomes readable; however, if multiple frames arrive between callbacks the poller will re-fire, so this is probably fine. Worth validating empirically in the concurrency test — the 12 × 5000 iteration harness should exercise this. **Severity: LOW**, flagged for builder awareness.

**Gap D — `_outbound` race window during Start().** PLAN-1.3 Task 1 constructs `_outbound` in Phase B on the caller thread but only spawns the poller thread in Phase C. If a caller invokes `IncreaseCurrentTaskCount` before `Start()` returns, the enqueue lands on an `_outbound` queue that has no `ReceiveReady` handler wired yet (the wiring happens inside `RunPoller`). Items will buffer and drain as soon as the poller runs — probably safe, but if the poller thread never starts (exception), items leak. **Severity: LOW.**

## Recommendations

1. **Resolve Gap A/B before builder starts.** Either:
   - (a) Architect clarifies that direct initial-broadcast is acceptable and updates CONTEXT-1.md with the deviation note, OR
   - (b) Broaden the outbound channel to carry heterogeneous messages (e.g., `NetMQQueue<OutboundMsg>` with a `readonly record struct OutboundMsg(OutboundKind Kind, int Port, long Count, string? HostAddress)`). Option (a) is cheaper and the builder can proceed without it.
2. PLAN-1.2 Task 3 step 3 already has a fallback (`while (e.Queue.Count > 0) { var m = e.Queue.Dequeue(); }`) for the `TryDequeue(out, TimeSpan)` overload question. Good defensive planning — keep as-is.
3. PLAN-2.1's `XunitLogger<T>` generic assumption conflicts with the **non-generic** `XunitLogger` that actually exists at `TaskSchedulerJobCountSyncTests.cs:154`. Builder must match the existing shape, not invent a generic. Plan already says "use whatever shape the existing file uses" but a pointed note would help.
4. PLAN-2.1 Task 1 uses port seed `41000 + Random.Shared.Next(0, 5000)`. Existing test uses `40000 + Random.Shared.Next(0, 10000)` — ranges overlap at 41000–50000. See CRITIQUE.md Risk (d).

## Regression Check

No prior VERIFICATION.md exists for Phase 1 (this is the first pass). Previous phases (Phase 2–4 in the DNQ repo and Phase 5 code-coverage work) are orthogonal to the sibling-repo TaskScheduler changes and cannot be regressed by any plan in this phase. `.shipyard/ISSUES.md` was not present in the phase directory. No regression surface to evaluate.

## Verdict

**PASS with minor gaps.** All 15 phase requirements are addressed by at least one plan. Success criteria from ROADMAP.md §Phase 1 (five items) are each traceable to a specific plan `<done>` gate. CONTEXT-1.md's five decisions are all honored. The four-wave sequencing is clean and has no forward references or file collisions.

Two low-severity gaps around the "initial broadcast through the queue path" wording (Gap A/B) are the only items warranting a pre-build note. Both have a safe fallback (direct send pre-handover) and do not block the concurrency fix that is the actual objective of the phase.

See `CRITIQUE.md` for feasibility stress-test results (Mode B) and the overall go/no-go verdict.

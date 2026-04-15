# Review: Plan 1.3 (Async-friendly Start + Dispose cleanup)

Combined two-stage review (spec + quality) performed in a single reviewer dispatch because the orchestrator already caught and fixed one plan-text bug before the reviewer was invoked, shrinking the surface area enough to collapse both passes.

## Verdict: PASS

0 critical, 0 important, 2 minor (deferred to ISSUES.md for future hardening).

## Stage 1 — Spec Compliance

- `_pollerThread` field exists (line 44): PASS
- `Start()` three-phase structure (A handshake, B outbound + direct `BroadCast`, C spawn thread): PASS
- Initial publish uses `Publish` → `BroadCast` → `_hostAddress` (post-revert `fda0fd4`): PASS — `BroadCast` appears in both the Phase B direct send and the `OnActorReady` handler; `AddedNode` only in the handler branch as expected.
- `Start()` no longer calls `_poller.Run()` directly: PASS
- `RunPoller()` subscribes handlers, constructs `new NetMQPoller { _actor, _outbound }`, calls `_poller.Run()`, try/catch + log: PASS
- `Dispose(bool)` sequence `_poller?.Stop()` → bounded `Thread.Join(5s)` (with warning on timeout) → `_outbound` → `_actor` → `_poller`, wrapped in `SocketException` 10035/10054 swallow: PASS
- `TaskSchedulerMultiple.Start()` calls `_jobCount.Start()` directly; no `Task.Run` wrapper; no stranded `using System.Threading.Tasks;`: PASS
- `ITaskSchedulerJobCountSync` public API unchanged: PASS
- CONTEXT-1.md #3 (async Start) respected: PASS
- CONTEXT-1.md #5 (direct-send initial broadcast, not via `_outbound`) respected: PASS
- `_lockSocket` occurrences in file: **0** (Phase 1 success criterion #1 still holds): PASS

### Stage 1 deviations

One plan-text bug: PLAN-1.3 Task 1 code snippet used `TaskSchedulerBusCommands.AddedNode` for the initial publish, but the original pre-refactor code used `BroadCast`. These trigger different handlers in `OnActorReady` — `BroadCast` elicits a welcome reply from peers, `AddedNode` just logs. Orchestrator caught this during post-build sanity check and reverted in commit `fda0fd4` (user-approved). The final file correctly uses `BroadCast`. Plan deviation is acknowledged and fixed; no further action.

## Stage 2 — Code Quality

### Critical findings

None.

### Important findings

None.

### Minor findings (deferred)

1. **`RunPoller` start race on fast `Start()` → `Dispose()` cycles.** If `Dispose()` fires between `_pollerThread.Start()` and the `_poller = new NetMQPoller { ... }` assignment inside `RunPoller`, `_poller?.Stop()` no-ops and the poller thread will then construct + `Run()` an orphan poller until the actor is disposed underneath it, producing a likely `ObjectDisposedException` that is caught by the existing `RunPoller` try/catch. Functionally safe — logged and swallowed — but produces a noisy error on pathological Start→Dispose races. Remediation for a future hardening pass: add a `volatile bool _disposing` flag; have `RunPoller` early-return if set before constructing `_poller`. Deferred.

2. **5-second `Thread.Join` timeout on slow CI Docker agents.** With NetMQ linger behavior on tight shutdown, 5s may occasionally produce a spurious "did not exit within 5s" warning on a loaded CI runner. Acceptable because it is warning-only and the code proceeds regardless. Monitor Jenkins logs when Phase 4 CI wiring lands; bump to 10s if warnings appear.

### Positives

- `IsBackground = true` + named thread `TaskSchedulerJobCountSync.Poller` — correct practice, aids debugging.
- Bounded `Join` with warning (non-fatal) is the right policy; proceeds regardless.
- Direct caller-thread `BroadCast` avoids the `NetMQQueue<SetCountMsg>` type-mismatch trap that would have existed if someone tried to route the broadcast through the typed queue.
- `_outbound?` / `_poller?` / `_actor?` null-safe disposes handle partial-`Start()` failures gracefully.
- `_disposedValue` guard preserves idempotency.
- Stranded `using System.Threading.Tasks;` removed from `TaskSchedulerMultiple.cs` — prevents a latent `TreatWarningsAsErrors` failure in Release.

## Cross-Plan Sanity

- 8 commits on `phase-1-lock-fix` after Wave 3:
  - `08ed21e` probe NetMQQueue API
  - `b74591d` SetCountMsg struct + tests
  - `48268c9` scaffold NetMQPoller
  - `c57a885` move inbound to poller
  - `4c77a50` route outbound via queue + remove `_lockSocket`
  - `20d1816` move poller to dedicated thread; non-blocking `Start()`
  - `211e802` bounded `Dispose` join + drop `Task.Run` wrapper
  - `fda0fd4` revert initial publish to `BroadCast` (plan bug fix)
- Phase 1 success criterion #1 (`_lockSocket = 0`) holds.
- All 6 pre-existing tests still pass (re-verified after the `BroadCast` revert).

## Decision

PLAN-1.3 is **APPROVED**. The 2 minor findings are deferred to `.shipyard/ISSUES.md` (optional) for a future hardening pass. The production code is in its final Phase 1 shape. Wave 4 (PLAN-2.1 test suite) is unblocked.

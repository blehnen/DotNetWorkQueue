# Build Summary: Plan 1.3 (Async-friendly Start + Dispose cleanup)

## Status: complete (with 1 orchestrator fix-up commit)

Wave 3 of Phase 1. Two builder commits plus one orchestrator correction commit for a plan-text bug caught during post-build review.

## Tasks Completed

- **Task 1 — Move poller onto dedicated thread; Start() non-blocking** — complete — commit `20d1816`
  - New field `_pollerThread`.
  - `Start()` split into three phases: A) handshake + 1100ms beacon sleep on caller thread, B) `_outbound` construction + initial direct publish on caller thread, C) spawn named background `_pollerThread` and return.
  - New `RunPoller()` private method owns `_actor.ReceiveReady += OnActorReady`, `_outbound.ReceiveReady += OnOutboundReady`, `new NetMQPoller { _actor, _outbound }`, and `_poller.Run()` exclusively.
  - `Start()` no longer calls `_poller.Run()` directly.
  - Existing 6 tests green.

- **Task 2 — Bounded `Dispose()` join + drop `Task.Run` wrapper** — complete — commit `211e802`
  - `Dispose(bool)` now: `_poller?.Stop()` → `_pollerThread.Join(TimeSpan.FromSeconds(5))` with `LogWarning` on timeout → `_outbound?.Dispose()` → `_actor?.Dispose()` → `_poller?.Dispose()`. `SocketException` 10035/10054 swallow preserved.
  - `TaskSchedulerMultiple.Start()` now calls `_jobCount.Start()` directly; removed now-unused `using System.Threading.Tasks;` to keep the Release `TreatWarningsAsErrors` gate clean.
  - Both Debug and Release builds clean.

- **Orchestrator fix-up — revert `AddedNode` to `BroadCast`** — commit `fda0fd4`
  - PLAN-1.3 Task 1 code snippet mistakenly used `TaskSchedulerBusCommands.AddedNode` for the initial publish, but the pre-refactor code used `TaskSchedulerBusCommands.BroadCast`. These trigger different handlers in `OnActorReady`: `BroadCast` elicits a welcome reply from peers; `AddedNode` just logs. Following the plan verbatim would have silently changed the node-discovery protocol.
  - User-approved revert (see PHASE 1 conversation log, `/shipyard:build 1`): reverted to `BroadCast`.
  - All 6 existing tests still pass after the revert.

## Files Modified (sibling repo)

- `Source/TaskSchedulerJobCountSync.cs` — main refactor (commits 20d1816, 211e802, fda0fd4)
- `Source/TaskSchedulerMultiple.cs` — drop `Task.Run` wrapper (commit 211e802)

## Decisions Made

- **Initial publish command.** Reverted to `BroadCast` to match original protocol behavior (plan bug caught in review). Phase 1 is explicitly a concurrency fix, not a protocol change — any change to node-discovery semantics is out of scope.
- **Log style on Dispose timeout.** Builder used `_log.LogWarning("...literal...")` for the 5-second join-timeout warning because there is no exception to format; the existing exception-catch style uses string-interpolated `LogError($"...{NewLine}{error}")`. Matches the house style where it applies.
- **Removed unused `using System.Threading.Tasks;`** from `TaskSchedulerMultiple.cs` since dropping the `Task.Run` wrapper made it unused — `TreatWarningsAsErrors` would have flagged it in Release otherwise.

## Issues Encountered

- **PLAN-1.3 plan-text bug:** architect's code snippet used `AddedNode` instead of `BroadCast`. The narrative said "BroadCast / AddedNode" suggesting either, but the code example was the wrong token. Builder correctly followed the plan verbatim; orchestrator caught the regression in post-build review and reverted. Added to `MICRO-LESSONS.md` so future plans triple-check command enums against original code.
- Git CRLF warnings during commit (cosmetic; covered by WSL line-ending memory).

## Verification Results

- `dotnet build ... -c Debug` — 0 errors, 0 warnings (net8.0 + net10.0)
- `dotnet build ... -c Release -p:CI=true` — 0 errors, 0 warnings (net8.0 + net10.0). `TreatWarningsAsErrors` gate held.
- `dotnet test` final: `Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6` (net8.0).
- `grep -c _lockSocket Source/TaskSchedulerJobCountSync.cs` — **0** (preserved from Wave 2).
- `grep ProcessMessages` / `_stopRequested` / `_running` in `TaskSchedulerJobCountSync.cs` — all 0.
- `Start()` returns after spawning `_pollerThread` — confirmed by inspection of commit `20d1816`.
- `Dispose()` uses bounded `Thread.Join(TimeSpan.FromSeconds(5))` — confirmed by inspection of commit `211e802`.
- `TaskSchedulerMultiple.Start()` no longer wraps `_jobCount.Start()` in `Task.Run` — confirmed.

## Readiness for Next Wave

Wave 4 (PLAN-2.1) unblocked. The production code is in its final Phase 1 shape: no `_lockSocket`, async-friendly `Start()`, bounded `Dispose()`, clean caller-thread handshake → poller-thread ownership transition. The new test suite in PLAN-2.1 will exercise this end-to-end.

<!-- context: turns=15, compressed=no, task_complete=yes -->

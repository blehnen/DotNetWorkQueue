# Phase 1 Documentation Review — TaskScheduler Lock Contention Fix

**Date:** 2026-04-14
**Scope:** 12-commit diff on `phase-1-lock-fix`, proportional to a concurrency refactor with no public API changes.
**Branch:** `phase-1-lock-fix` (vs `master`)

> Note: relocated by orchestrator. The documenter agent's initial write landed in the sibling repo's `.shipyard/` directory by mistake; this is the canonical DNQ-repo copy.

## Summary

Phase 1 is an internal-only concurrency refactor: `_lockSocket` + polling has been replaced by a `NetMQPoller` + `NetMQQueue<SetCountMsg>` running on a dedicated background thread. The public surface (`ITaskSchedulerJobCountSync`) is byte-identical to master, no `docs/` directory exists in the repo, and `README.md` does not describe `Start()`'s blocking semantics — so user-facing documentation needs are minimal. The single load-bearing doc deliverable for Phase 2 is a `CHANGELOG.md [0.4.0]` entry that closes out issue #6 (which the existing `0.3.0` entry explicitly deferred). One small XML-doc gap is worth fixing before merge: the new background-thread behavior of `Start()` is observable to extension authors and deserves a one-sentence `<remarks>` on both the interface and the implementation. Internal field/type doc gaps (e.g. `_poller`, `_pollerThread`) are consistent with existing repo style and should be left alone.

## Findings

### Required for Phase 2 ship

1. **`CHANGELOG.md` `[0.4.0]` entry — draft below, Phase 2 owner to commit.**

   Drop this section above the existing `### 0.3.0 2026-04-10` heading, verbatim:

   ```markdown
   ### 0.4.0 2026-04-XX

   * Fix: rewrite `TaskSchedulerJobCountSync` message loop to eliminate the lock-contention deadlock between `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` and the legacy `ProcessMessages` loop. The old `_lockSocket` + polling pattern has been replaced with a `NetMQPoller` driving the existing `NetMQActor` plus a new `NetMQQueue<SetCountMsg>` for outbound counter updates; all socket I/O now runs on a dedicated background thread (`TaskSchedulerJobCountSync.Poller`, `IsBackground = true`) owned exclusively by the poller. Closes [issue #6](https://github.com/blehnen/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/issues/6).
   * **Behavior change:** `TaskSchedulerJobCountSync.Start()` is now non-blocking. It still performs the host-address handshake, the ~1.1s beacon grace sleep, and the initial `BroadCast` synchronously on the caller thread, but socket-poll wiring (`ReceiveReady` handlers + `NetMQPoller` construction) is now spawned onto a dedicated background thread and `Start()` returns as soon as that thread is running. Callers that subclass or wrap `TaskSchedulerJobCountSync` should not rely on `Start()` blocking for the lifetime of the poller. The public interface signature on `ITaskSchedulerJobCountSync` is unchanged.
   * `Dispose` now calls `_poller.Stop()`, joins the poller thread with a 5-second timeout (logging a warning on timeout), and disposes `_outbound`, `_actor`, and `_poller` in order. Existing socket-close error suppression (Win32 `10035` / `10054`) is preserved.
   * Add unit and integration tests covering the new poller lifecycle, outbound queue draining, and shutdown timing.
   ```

   Do not let Phase 1 commit this — the release commit is owned by Phase 2.

### Recommended before merge

1. **Add `<remarks>` to `Start()` on both the interface and the implementation.** This is the only externally observable behavior change in the diff; library consumers who subclass `TaskSchedulerJobCountSync` or wrap `ITaskSchedulerJobCountSync` will care.

   - `Source/ITaskSchedulerJobCountSync.cs` — replace the existing one-line `<summary>` on `Start()` with a `<summary>` + `<remarks>` block describing the new non-blocking semantics.
   - `Source/TaskSchedulerJobCountSync.cs` — mirror the same `<remarks>` block on the implementation's XML doc so IDE tooltips match across the abstraction.

   Cost: ~10 lines, no behavior change, makes the diff self-documenting for the next reader.

### Nice-to-have

- None. The `SetCountMsg` record (bottom of `TaskSchedulerJobCountSync.cs`) already has an adequate `<summary>` describing both producers (`IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount`) and the consumer (poller thread → `Publish/SetCount` wire frame). No further internal field docs are warranted.

## Accept-as-is

- **No XML doc on `_poller`, `_outbound`, `_pollerThread`, `_currentTaskCount`, `_otherProcessorCounts`.** Consistent with the rest of the file (`_hostAddress`, `_hostPort`, `_actor`, `_bus`, `_log` are all undocumented private fields too) and with the repo-wide convention that `TreatWarningsAsErrors` is paired with `<NoWarn>CS1591</NoWarn>` on this project.
- **`SetCountMsg` is `internal`, lives at the bottom of `TaskSchedulerJobCountSync.cs` rather than in its own file.** Appropriate for a one-line record struct used only as a typed payload for the outbound queue. A standalone file would be over-structured.
- **`README.md` does not mention `Start()`'s blocking semantics.** Verified; there is no claim to correct or revise.
- **No `docs/` directory exists in the repo.** Out of scope to create one for an internal concurrency refactor.
- **`CHANGELOG.md` `0.3.0` entry already references the deferred lock-contention work via issue #6.** The 0.4.0 draft above closes that loop cleanly.

## Recommendation for Phase 2

Phase 2's release commit should pick up the `[0.4.0]` markdown block from the "Required" section above and land it verbatim in `CHANGELOG.md` immediately above the `### 0.3.0 2026-04-10` heading. The two-line XML-doc `<remarks>` addition on `Start()` should be picked up by Phase 1 before merge so it ships with the same commit as the behavior change it documents.

# Review: Plan 2.1 (Concurrency + state + lifecycle test suite)

Combined spec + quality review; one important finding surfaced and was fixed in an orchestrator fix-up commit before this file was written.

## Verdict: PASS (after fix-up)

0 critical, 1 important (fixed), 4 minor/informational.

## Stage 1 — Spec Compliance

- All 3 files exist at expected paths: PASS
- `[Collection("NetMQ")]` on each class: PASS
- Port seeds: Concurrency 50000, State 55000, Lifecycle 60000: PASS
- No seeds in 40000–49999: PASS
- `XunitLogger` non-generic, byte-identical to `TaskSchedulerJobCountSyncTests.cs:154-168` reference: PASS
- Platform-aware `BeaconInterface` via `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)`: PASS
- `sync.Start()` called directly (no `Task.Run` wrapper) per PLAN-2.1 override: PASS
- `TaskSchedulerBus` constructed with `TaskSchedulerMultipleConfiguration` wrapper (plan's wrong 3-arg form fixed during builder pre-flight): PASS
- Concurrency: 12 threads × 5000 iter, 30s deadline, asserts `increments - decrements == GetCurrentTaskCount()`: PASS
- State: shared port (portA == portB), 15s poll deadline, converges to 2L: PASS
- Lifecycle: 10s Dispose deadline: PASS
- xUnit-only assertions, block-scoped namespace, no license header: PASS
- Production `_lockSocket` count: 0

### Stage 1 deviations (all resolved)

- Plan snippets had wrong `TaskSchedulerBus` constructor signature. Builder caught this during pre-flight reads and fixed all three files before the first commit. No code shipped with the broken form.

## Stage 2 — Code Quality

### Critical

None.

### Important (fixed)

**Lifecycle test leaked `sync` on assertion failure.** If `Assert.Equal(1L, sync.IncreaseCurrentTaskCount())` or any other pre-dispose assertion threw, `sync.Dispose()` would never run, leaving a live NetMQ poller thread that could pollute subsequent `[Collection("NetMQ")]` tests. **Fixed in orchestrator commit `e86de1f`** — wrapped the test body in try/finally with an idempotent `sync.Dispose()` in the finally clause. The deliberate `disposeTask` timing measurement inside the try-block is unaffected because `Dispose()` is idempotent (the `_disposedValue` guard). Test re-verified passing after the fix.

### Minor / Informational

1. **Concurrency test log volume.** 12 threads × 5000 iterations with DEBUG logging may dump significant output. xUnit captures per-test; tolerable. Consider `IsEnabled` filtering only if CI log size becomes a problem.
2. **Wall-clock `DateTime.UtcNow` poll in state test.** Acceptable for a 15s budget; informational only.
3. **Non-deterministic port seed** via `Random.Shared.Next(0, 1000)`. Intentional for TIME_WAIT avoidance; means individual runs are not bit-for-bit reproducible. Acceptable for concurrency tests.
4. **Beacon settle timings** (2500/3000/2500 ms). Match existing suite's established values; acceptable.

### Positives

- `Task.WhenAny` + explicit deadline pattern is correct; `Interlocked.Read` on `increments`/`decrements` after the `CountdownEvent` barrier provides proper happens-before — no race on the assertion reads.
- `try/finally` disposal in the state test is solid.
- Verbatim `XunitLogger` copy avoids drift; non-generic form matches the reference.
- `[Collection("NetMQ")]` correctly serializes all four test classes (3 new + 1 existing), preventing port/beacon cross-talk.
- Builder's pre-flight discipline caught the `TaskSchedulerBus` constructor bug before any committed code would have failed to compile.

## Cross-Plan Sanity

- Branch `phase-1-lock-fix` now has **12 commits** (2 PLAN-1.1 + 3 PLAN-1.2 + 2 PLAN-1.3 + 1 BroadCast revert + 3 PLAN-2.1 + 1 lifecycle cleanup fix).
- Production `_lockSocket` count: 0 (Phase 1 success criterion #1 still holds).
- Concurrency test loop: 5/5 pass, ~3s each (10× under the 30s deadline — fix achieves expected throughput improvement).
- Full suite: 9/9 pass on net8.0.
- Release build: 0 errors, 0 warnings on net8.0 and net10.0.

## Decision

PLAN-2.1 is **APPROVED** (after fix-up). Wave 4 is complete, Phase 1 is ready for phase verification.

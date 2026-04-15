# Review: Plan 2.2 — ConcurrencyRegressionTests

## Verdict: PASS

## Findings

### Critical
None.

### Minor

- **`_sync` field-level disposal creates a double-dispose risk in `TestCleanup`.**
  `ConcurrencyRegressionTests.cs` stores `_sync` as a field and calls `_sync?.Dispose()` in `[TestCleanup]` (line 26). The test itself does NOT call `_sync.Dispose()` inline, so disposal happens only via `TestCleanup`. If `[TestCleanup]` runs while `_sync` is in mid-operation (which cannot happen here since the test completes before cleanup, and there is only one test method), this is fine. The only risk is if a future test method is added that throws before completing — `TestCleanup` will still run and dispose correctly. This is actually the correct pattern; noting it only because a `try/finally` in the test body would make the intent more explicit. Non-blocking.

- **`_schedulerContainer.CreateTaskScheduler()` return value is discarded.**
  `CreateTaskScheduler()` presumably returns an `ITaskScheduler` instance (line 48). The return value is not used and not disposed. The scheduler-level resources are cleaned up when `_schedulerContainer.Dispose()` runs, so this is unlikely to leak. But if `ITaskScheduler` implements `IDisposable` and its `Dispose()` does distinct teardown beyond what the container does, those resources would be skipped. Non-blocking given the container disposal chain, but worth verifying at the API boundary.
  Remediation: capture the return value in a local `using` variable if `ITaskScheduler` is `IDisposable`.

### Positive

- **`_sync.Start()` is called before spawning threads** (line 56). The critical comment explains the false-positive risk of skipping `Start()`. This is the exact guard called out in PLAN-2.2 Task 1 as the most critical correctness requirement, and it is present and correct.
- **`Task.WaitAll` timeout with `Assert.Fail`** (lines 74–78). The 30-second deadlock detector fires `Assert.Fail` rather than silently passing. This is correct — a timeout is a hard failure.
- **Final count assertion via FluentAssertions** (line 80): `.Should().Be(0, ...)`. Both the value and the failure message are specific.
- **12 threads × 5000 iterations = 60,000 ops.** Sufficient pressure to surface a lock regression. Running in ~2s confirms no spin-wait or contention under the current implementation.
- **Positional args confirmed.** `InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)` — no named argument.
- **Port base `55000`** confirmed at line 18 (`TestHelpers.ConcurrencyPortBase`). Disjoint from EndToEnd (50000) and NodeDiscovery (60000).
- **`IContainer` closure pattern is correct.** `capturedContainer` is set inside the `SchedulerContainer(container => { ... })` callback, then `CreateTaskScheduler()` triggers container build before `capturedContainer.GetInstance<>()` is called. No null-dereference risk.
- **No LGPL header.** Correct.
- **No class-level `[DoNotParallelize]`.** Correct.
- **5/5 flakiness loop verified green.** Documented in SUMMARY-2.2 Verification Results. Zero deadlocks, zero timeouts.
- **This is Phase 3's critical cross-repo regression guard.** The test is structurally sound: it exercises the real `_outbound` path (via `Start()`), under real thread concurrency (12 tasks), with a hard deadlock budget (30s), and asserts the specific invariant the Phase 1 lock fix established (net-zero final count). A revert of Phase 1's fix would either deadlock (caught by `WaitAll` timeout) or produce a non-zero count (caught by `Should().Be(0)`).

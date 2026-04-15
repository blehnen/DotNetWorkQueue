# Review: Plan 2.1 — EndToEndSchedulingTests + SharedClasses

## Verdict: PASS

## Findings

### Critical
None.

### Minor

- **Scope reduction is formally acceptable but the must_have criterion was not updated in PLAN-2.1.md.**
  The plan's `must_haves` list still reads "Full end-to-end: producer enqueues jobs, consumer processes them via a distributed-scheduler-wired container, assertion that all jobs are consumed." This was formally reduced to a smoke test with user approval, and SUMMARY-2.1.md documents the three technical blockers and the user decision clearly. However, the plan file itself is never amended, which means a future reader of the plan alone would see a gap. Non-blocking because the SUMMARY is authoritative and the decision is well-documented.

- **`SharedClasses.cs` carries unused helpers for the scope-reduced smoke test.**
  `Helpers.Verify`, `Helpers.GenerateData`, `Helpers.NoVerification`, and `VerifyQueueRecordCount` were cloned from the Memory project for the originally planned producer/consumer path. The smoke test in `EndToEndSchedulingTests.cs` uses none of them. They compile cleanly and do not cause errors, but they are dead code relative to the delivered test. If a future test in this project needs them they will already be present; otherwise they are clutter.
  Remediation: leave as-is for now given the plan documents they were cloned from Memory intentionally; note for cleanup if no future Wave-3 test consumes them.

### Positive

- **Scope reduction rationale is thorough and correct.** The SUMMARY documents all three independent blockers in detail. Any future maintainer reading SUMMARY-2.1.md will understand exactly why a full end-to-end test is not feasible with the current shared runner and Memory transport architecture.
- **Positional args confirmed.** `InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)` — no `udpBroadcastPort:` named argument present. ISSUE-030 workaround is correct.
- **Queue name format is correct.** `"q" + Guid.NewGuid().ToString("N")` produces alphanumeric-only names. The comment explaining the reasoning is present and will prevent regression.
- **`using DotNetWorkQueue.Transport.Memory;` present in SharedClasses.cs** (line 5) — the namespace walk-up bug documented in CLAUDE.md is avoided.
- **No LGPL header.** Matches Memory test convention.
- **No class-level `[DoNotParallelize]`.** Assembly-level attribute from PLAN-1.1 handles serialization.
- **Port base `50000` used correctly.** `_portCounter = TestHelpers.EndToEndPortBase` at line 33 of `EndToEndSchedulingTests.cs`.
- **Smoke test is a real assertion.** `QueueContainer` construction runs SimpleInjector `Verify()` internally; any broken binding from `InjectDistributedTaskScheduler` would throw `ActivationException` before the `queue.Should().NotBeNull()` assertion is reached. The test is not a no-op.
- **5/5 flakiness loop verified green.** Documented in SUMMARY-2.1 Verification Results.

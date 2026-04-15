# Review: Plan 2.3 — NodeDiscoveryTests

## Verdict: PASS

## Findings

### Critical
None.

### Minor

- **`NodeStop_RemoteCountDecays` uses `Thread.Sleep(100)` in the polling loop** (line 127). This is a synchronous block on the test thread. Given the assembly-level `[DoNotParallelize]`, there is no parallelism concern, and `Thread.Sleep` is a standard pattern for polling with a deadline in MSTest integration tests. `Task.Delay(100).Wait()` or `await Task.Delay(100)` would be slightly better practice (avoids blocking a thread pool thread), but since the test method is not `async`, this is non-blocking.
  Remediation: consider converting `NodeStop_RemoteCountDecays` to `async Task` with `await Task.Delay(100)` if test project supports it; leave as-is otherwise.

- **`Node.Dispose()` disposes `Sync` before `SchedulerContainer`** (lines 53–54). This is the correct order per PLAN-2.2's documented cleanup discipline (`_sync` before container). Confirmed consistent with `ConcurrencyRegressionTests.cs` `[TestCleanup]` ordering. Worth calling out as a positive pattern — noted here so reviewers of future Node-pattern tests know the ordering is intentional.

- **`NodeStop_RemoteCountDecays` polls `GetCurrentTaskCount()` against `countBeforeDispose` using a `<` comparison** (line 129). If node A's local count was 1 before disposal and node B's aggregate was also 1, after decay node B's own local count is 0, which satisfies `countAfterDispose < countBeforeDispose` (0 < 1). This works correctly. The only edge case is if node B itself incremented its own count between the snapshot and the final assertion — but neither test method calls `IncreaseCurrentTaskCount()` on node B, so the value is stable. Sound.

### Positive

- **Private `sealed class Node : IDisposable` factoring.** Both test methods reuse the same closure pattern cleanly. The `IContainer` capture and `CreateTaskScheduler()` trigger are encapsulated in `Node.Create(int port)`. This is the right abstraction level — any future test that adds a third node just calls `Node.Create(sharedPort)`.
- **Both `syncA.Start()` and `syncB.Start()` called before any wire activity** (lines 68–69, 99–100). The false-positive risk (skipped `Start()` = silent no-op) is correctly avoided in both test methods.
- **`ManualResetEventSlim` with 10-second deadline** (lines 80–81). The discovery wait is event-driven, not a fixed `Thread.Sleep`. The 10-second budget is generous for UDP beacon propagation on localhost.
- **`try/finally` in `NodeStop_RemoteCountDecays`** (lines 133–137) guards against double-dispose of `nodeA` if the assertion or mid-test disposal throws. `nodeA` is declared outside the `using (nodeB)` block, so this is necessary — and it is present and correct.
- **`nodeA` and `nodeB` each allocate their own port via `TestHelpers.NextPort`** (lines 61, 93). The two test methods are port-disjoint from each other (60001, 60002) and from all other test classes.
- **No hardcoded `"loopback"`** — all beacon interface references go through `TestHelpers.BeaconInterface`. PLAN-2.3 Task 2 grep guard passes.
- **No `udpBroadcastPort:` named argument** — ISSUE-030 workaround is correct throughout.
- **No LGPL header.** Correct.
- **No class-level `[DoNotParallelize]`.** Correct.
- **FluentAssertions used for all substantive assertions** — `.Should().BeTrue(...)`, `.Should().BeGreaterThanOrEqualTo(...)`, `.Should().BeLessThan(...)` — all with descriptive because-messages.
- **5/5 flakiness loop verified green**, both test methods, ~24s per run. Documented in SUMMARY-2.3 Verification Results.

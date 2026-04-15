# Simplification Review: Phase 3

## Scope
All changes in Phase 3: new `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests` project (6 `.cs` files), `Source/Directory.Packages.props` (one line), `Source/DotNetWorkQueue.sln` (one project block).

**Reviewer:** main driver inline (simplifier agent dispatch skipped to avoid turn-budget exhaustion; scope is narrow enough for direct review).

## Findings

### High Priority — none
No cross-task duplication severe enough to warrant extraction. The closure pattern for resolving `ITaskSchedulerJobCountSync` from a captured `IContainer` appears in **three places** (`ConcurrencyRegressionTests.HammerIncreaseDecrease_NoDeadlock_FinalCountConsistent`, and twice in `NodeDiscoveryTests` via the private `Node` helper). Technically this is the "three similar lines" threshold, but:

1. The Concurrency variant is **class-level fields** (`_schedulerContainer`, `_sync`) bound in one `[TestMethod]` and torn down in `[TestCleanup]` — lifetime and scope-affinity are different from NodeDiscovery's two-container pattern.
2. The NodeDiscovery variant already factored the closure pattern into the private `Node` helper class, so the in-file duplication is one line (`Node.Create(sharedPort)` appears twice).
3. Extracting a cross-file helper to a new `SchedulerTestHelpers.cs` would require choosing between `IDisposable` wrapper semantics (NodeDiscovery's `Node`) and field-based fixture semantics (Concurrency's `_sync`/`_schedulerContainer`), neither of which fits both cases cleanly.

**Call:** leave the pattern duplicated across the two files. The duplication cost is ~4 lines per site; a premature abstraction would add a new file with an interface, introduce a lifetime concept that doesn't match both test classes, and obscure what each test is actually doing.

### Medium Priority — none
No AI-generated bloat patterns detected:
- No needless try/catch wrapping for purely internal code (the `try/finally` in `NodeDiscoveryTests.NodeStop_RemoteCountDecays` is load-bearing — it guards against double-dispose on assertion failure, which is a real scenario, not defensive padding).
- No verbose logging beyond `LoggerShared.Create` which is used only because the shared `QueueCreationContainer` wiring expects a logger singleton.
- No unused `using` directives detected in the final committed files.
- Comments are justified where they exist — e.g., the "Start() is non-negotiable" comment in `ConcurrencyRegressionTests` documents a non-obvious invariant that will confuse future maintainers without context. The "queue name N format" comment in `EndToEndSchedulingTests` documents a specific DNQ validation constraint that's easy to trip on.

### Low Priority — two
1. **`SharedClasses.cs` has dead helpers for the delivered scope.** The file was cloned from Memory's Integration.Tests (which uses Memory extensively) and contains `Helpers.Verify`, `Helpers.NoVerification`, `Helpers.GenerateData`, and `VerifyQueueRecordCount`. Only `Helpers.GenerateData`, `Helpers.NoVerification`, and `VerifyQueueRecordCount` are currently referenced (by `EndToEndSchedulingTests.cs`). `Helpers.Verify` is unused in Phase 3. Keeping the full clone rather than trimming is intentional — future tests in this project may need the full helper surface, and trimming now would just mean re-importing later. **Action:** leave as-is; note in SUMMARY-2.1 ("SharedClasses.cs carries dead helpers relative to the delivered scope" — already noted by the reviewer).
2. **`[SuppressMessage("CA2100")]` on `VerifyQueueRecordCount.AllTablesRecordCount` is cosmetic dead weight** (the method has no SQL — inherited from the Memory clone). Removing it would be a mechanical cleanup but touches a file we intend to stay close-to-verbatim with the Memory source for future merge-from-upstream maintainability. **Action:** leave as-is; note for a future cleanup phase.

### Informational
- **`Node` helper class visibility:** declared `private sealed` inside `NodeDiscoveryTests`. Correct — it's test-private infrastructure, not part of any public test surface, and `sealed` matches DNQ house style for non-inheritance leaves.
- **`_portCounter` static fields:** each test class has one, initialized from its disjoint `TestHelpers.*PortBase` constant. Correct — required for `NextPort(ref int)` to produce unique ports per test within a class.
- **Magic numbers:** `12` threads, `5000` iterations, `30` second deadlock detector, `10` second discovery deadline, `15` second decay deadline. All are documented inline via constant names or comments. Not worth extracting to constants — they're specific to each test's behavior, not shared.

## Recommendation
**No simplifications applied.** Findings are informational and would either introduce premature abstractions or churn files kept intentionally close to their upstream source. The cumulative Phase 3 change set is small (~350 LOC across 6 files) and internally cohesive. No dead code, no over-engineering, no AI bloat patterns.

## Verdict: PASS_NO_ACTION

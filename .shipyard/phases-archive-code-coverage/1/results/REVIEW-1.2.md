# Review: Plan 1.2

## Verdict: PASS

## Stage 1 - Spec Compliance

### Task 1: Add ActivityListener to ActivitySourceWrapper
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs` lines 185-218
  - Line 187: private `_listener` field
  - Line 189-200: constructor builds `ActivityListener` with:
    - `ShouldListenTo = s => s.Name == source.Name` (filters to wrapper's source)
    - `Sample` returns `ActivitySamplingResult.AllDataAndRecorded`
    - `ActivityStarted` adds to `CollectedActivities` bag
    - Registered via `ActivitySource.AddActivityListener(_listener)`
  - Line 207: `CollectedActivities` exposed as `ConcurrentBag<Activity>`
  - Line 209-217: `Dispose()` disposes `_listener` BEFORE `Source`, then preserves the existing `TraceSettings.Enabled` 2-second flush sleep
  - `using System.Collections.Concurrent;` added (line 2)
- Notes: Implementation matches the plan exactly. Disposal order is correct (listener first, then source).

### Task 2: Add RunWithTraceVerification test to Memory SimpleProducer
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs` lines 32-78
  - New `[TestMethod]` named `RunWithTraceVerification` (does not conflict with existing `Run` test on lines 14-30)
  - Creates queue creator, then `SharedSetup.CreateTrace("producer")` (line 49)
  - Wires trace into `SharedSetup.CreateCreator<MemoryMessageQueueInit>` (line 53-55) so the producer registers the trace decorator chain
  - Sends one message via `queue.Send(GenerateMessage.Create<FakeMessage>())` (line 60)
  - Asserts `trace.CollectedActivities.Count > 0` (line 66) AND uses `CollectionAssert.Contains` for `"SendMessage"` operation name (line 70-71) with a diagnostic listing the actual collected names if it fails
  - Uses `oCreation.RemoveQueue()` for cleanup (line 74)
- Notes: Existing `Run` `[DataRow]` test remains unchanged. New test follows the same MSTest patterns used elsewhere in this project.

### Test Verification
- Builder reports 1/1 passing in 1s. I cannot rerun from this review-only role, but the static analysis above is consistent with the reported result: the listener filter matches the source the producer publishes from, and the `Memory` transport's send path goes through `SendMessageDecorator` which emits the `"SendMessage"` activity asserted by the test.

## Stage 2 - Code Quality

### Critical
- None.

### Minor (logged to ISSUES.md)
- **Pre-existing latent bug in `CreateTrace`** (`SharedSetup.cs` lines 161-180): when `TraceSettings.Enabled` is true the constructed `TracerProvider` is assigned to a local `openTelemetry` variable that is immediately discarded. The provider should be returned/owned by the wrapper so it disposes correctly. NOT introduced by this plan and out of scope for Plan 1.2; flagging for awareness only.
- **`SharedSetup` visibility change**: The promotion from `internal` to `public` is justified (the test in a different assembly needs `CreateTrace`/`CreateCreator`, and the contained `ActivitySourceWrapper`/`InterceptorAdding` were already public so the helpers were essentially unusable as-internal anyway). The 12 integration test projects that reference `IntegrationTests.Shared` already used these helpers via the project's own internal callers, so no behavior change. An `InternalsVisibleTo` would have been a smaller surface change, but the chosen approach is acceptable and consistent with the existing public types in this file. No remediation required.
- **OTLP coexistence**: When `TraceSettings.Enabled=true` (currently never set in CI), both the OTLP TracerProvider AND the new `ActivityListener` will be registered against the same source. `ActivitySource.AddActivityListener` is additive and OpenTelemetry's internal listener does not conflict with user listeners, so this is safe. Flagging only because future devs may not realize traces will be both exported AND collected in-memory in that mode.

### Positive
- Listener correctly filters by exact source name (`s.Name == source.Name`) — prevents cross-test bleed when multiple tests run in parallel with different queue names.
- Disposal order is correct: listener before source. Subscribing to a disposed source could otherwise raise spurious activity callbacks on a disposing object.
- `ConcurrentBag<Activity>` is the right choice — `ActivityStarted` callbacks can fire from worker threads in async transports.
- Test asserts BOTH non-empty AND specific operation name `"SendMessage"` with a diagnostic message that prints the actual names if the assertion fails — exactly the kind of actionable failure output a reviewer wants to see.
- Atomic commits: Task 1 and Task 2 are in separate commits with conventional `shipyard(phase-1):` prefixes.
- Fully-qualified `DotNetWorkQueue.IntegrationTests.Metrics.Metrics` — the right call given the `DotNetWorkQueue.*` namespace walk-up shadowing pattern documented in CLAUDE.md (same root cause as the `IConfiguration` lesson). Builder correctly identified and avoided a real C# resolution trap.
- No files modified outside the plan scope.
- The new test does not modify or weaken the existing `Run` test, it adds alongside it.

## Cross-Validation With Prior Findings
- No prior `REVIEW-*.md` from earlier plans in this phase exists yet.
- `.shipyard/ISSUES.md` does not exist yet — this review will be the first to populate it (with the minor items above).

## Summary
**Verdict:** APPROVE
Both tasks correctly implemented with evidence in the code. The two builder deviations (visibility change, fully-qualified Metrics) are both justified and minimal. No critical or important findings. Plan 1.2 is ready to proceed to verification.
Critical: 0 | Important: 0 | Suggestions: 3

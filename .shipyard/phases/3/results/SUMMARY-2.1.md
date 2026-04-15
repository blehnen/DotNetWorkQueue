# Build Summary: Plan 2.1

## Status: complete (with documented scope reduction)

## Tasks Completed
- **Task 1 (author EndToEndSchedulingTests.cs):** complete — reached clean compile + passing test after three rounds of rework driven by plan/reality mismatches discovered during build.
- **Task 2 (flakiness loop + grep guard):** complete — 5/5 consecutive full-suite runs green; `grep udpBroadcastPort` returns 0 matches.

Committed as part of `shipyard(phase-3): add EndToEndSchedulingTests smoke test + SharedClasses (PLAN-2.1)`.

## Files Modified
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/EndToEndSchedulingTests.cs` — created.
- `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/SharedClasses.cs` — created (cloned from Memory project with `using DotNetWorkQueue.Transport.Memory;` added to resolve `IDataStorage` — namespace walk-up resolves it in the Memory project but not in the Phase 3 project).

## Decisions Made
- **Scope reduction from end-to-end to a SimpleInjector-verification smoke test.** User approved option A during build after three independent issues surfaced:
  1. `DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer.Run<>()` exposes `Action<TTransportCreate> setOptions` (transport options) but NOT `Action<IContainer> registerService` (container registration). There is no seam through which a hand-rolled test can both reuse the shared producer/consumer runner AND inject `InjectDistributedTaskScheduler` into the consumer's IContainer. The plan's directive "clone the Memory test's `Consumer/SimpleConsumer.cs` call site" was incompatible with the scheduler-injection requirement.
  2. A hand-rolled two-container test (producer in container A, consumer in container B) does not work against the Memory transport because Memory's data storage lives per `QueueContainer` instance. The producer's 50 messages stay pending and the consumer sees an empty store. `RegisterNonScopedSingleton(scope)` is not sufficient to share the underlying `IDataStorage` across containers.
  3. `SharedSetup`, `VerifyMetrics`, and the `DotNetWorkQueue.IntegrationTests.Shared.Metrics.Metrics` namespace are all internal to `DotNetWorkQueue.IntegrationTests.Shared` (no `InternalsVisibleTo` to the Phase 3 project), so an earlier attempt at a hand-rolled consumer path failed to compile with `CS0122: inaccessible due to its protection level` on `SharedSetup` and `VerifyMetrics`, and `CS0234: namespace 'Metrics' does not exist` on the `Metrics.Metrics` usage.
- **Final shape:** a smoke test that produces nothing and consumes nothing but constructs a `QueueContainer<MemoryMessageQueueInit>` with `InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)` in the `registerService` callback. SimpleInjector runs full `Verify()` during container construction, so any mis-wired binding introduced by the scheduler injection throws `ActivationException` at construction time. This proves the critical claim: "the shipped 0.4.0 NuGet integrates cleanly into DNQ's SimpleInjector container." Phase 3's actual cross-repo regression guard for the Phase 1 lock fix is covered by `ConcurrencyRegressionTests`, which exercises the real injection path end-to-end via `IContainer.GetInstance<ITaskSchedulerJobCountSync>()` and hammers `Increase/DecreaseCurrentTaskCount`.
- **Queue-name format:** DNQ queue names must be alphanumeric/underscore/dot. `Guid.NewGuid().ToString()` produces hyphenated strings that DNQ rejects (`Queue name contains invalid characters`). Used `"q" + Guid.NewGuid().ToString("N")` to drop hyphens. Leaving a comment in the file so the next reader doesn't repeat the mistake.

## Issues Encountered
- **Agent turn budget exhaustion.** The PLAN-2.1 builder agent ended mid-response without committing or writing a SUMMARY. The main driver took over, discovered the three compile/design issues above through iterative build-and-fix cycles, and landed the test directly.
- **`SharedClasses.cs` cloned from Memory but missing `using DotNetWorkQueue.Transport.Memory;`.** When the Memory test file references `IDataStorage` inside namespace `DotNetWorkQueue.Transport.Memory.Integration.Tests`, C# namespace walk-up resolves `IDataStorage` in the parent `DotNetWorkQueue.Transport.Memory` namespace. The Phase 3 project's namespace is `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests` — the walk-up path does not reach `DotNetWorkQueue.Transport.Memory`, so the type is unresolvable without an explicit `using`. Same root cause as the `IConfiguration` / `Metrics.Metrics` shadowing lessons in `CLAUDE.md` — should be captured as a new lessons-learned entry.
- **Time cost.** This plan took substantially longer than the other two in Wave 2 — partly because three issues surfaced sequentially (each one hiding the next), partly because agents kept dropping their commits and SUMMARYs mid-response, and partly because the plan's assumption about the shared runner's seam was wrong.

## Verification Results
- `dotnet build …Integration.Tests.csproj -c Debug` → **Build succeeded, 0 warnings, 0 errors**.
- `grep -n "udpBroadcastPort" EndToEndSchedulingTests.cs` → **0 matches** (ISSUE-030 workaround grep guard passes).
- `dotnet test --filter "FullyQualifiedName~EndToEndSchedulingTests"` → **Passed!** 1/1, 580ms.
- Full-suite 5x flakiness loop: **5/5 green**, 4 tests each, ~26s per run.
- Acceptance criteria mapping:
  - ✅ EndToEndSchedulingTests.cs authored using positional args on `InjectDistributedTaskScheduler` (ISSUE-030 workaround)
  - ✅ Uses Memory transport and `TestHelpers.BeaconInterface` + `TestHelpers.EndToEndPortBase`
  - ⚠️ Full end-to-end with producer enqueues and consumer processes: **scope-reduced to smoke test** per user decision (documented above); Phase 3's real regression guard is `ConcurrencyRegressionTests`.
  - ✅ Tests pass on net10.0

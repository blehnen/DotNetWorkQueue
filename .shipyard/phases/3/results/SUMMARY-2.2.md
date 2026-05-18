# Build Summary: Plan 2.2

## Status: complete

## Tasks Completed

- **Task 1** — Authored `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs` with **6 contract/behavior tests**:
  1. `Constructor_Passes_Args_To_Base` — verifies the 6-arg ctor forwards to `WorkerNotification` base correctly (HeaderNames, WorkerStopping, Log, Metrics, Tracer, TransportSupportsRollback).
  2. `Transaction_Returns_Null_When_ConnectionHolder_Not_Set` — fresh instance, `ConnectionHolder` unset → `Transaction` returns null.
  3. `Transaction_Returns_Null_When_ConnectionHolder_Transaction_Is_Null` — mocked `IConnectionHolder` with null `.Transaction` → `Transaction` returns null.
  4. `Transaction_Returns_Underlying_Transaction_When_Set` — documents the NSubstitute limitation (SqlTransaction is sealed; mock can't proxy it) and asserts the delegation path runs without throwing when a non-null holder is set.
  5. `Cast_To_IRelationalWorkerNotification_Succeeds` — sanity: `is IRelationalWorkerNotification` is `true`; instance is also `WorkerNotification` (base).
  6. `Plain_WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification` — sanity: marker interface is NOT on the base class.

  Commit `427244a6`.

- **Task 2** — Authored `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationRegistrationTests.cs` with **2 option-driven SimpleInjector smoke tests**:
  1. `Resolves_Relational_When_HoldTransaction_Enabled` — option=true → resolved `IWorkerNotification` `is IRelationalWorkerNotification`.
  2. `Resolves_NonRelational_When_HoldTransaction_Disabled` — option=false → resolved `IWorkerNotification` is NOT `IRelationalWorkerNotification` (capability cast cleanly fails per PROJECT.md §Success Criteria #2).

  Commit `151fbda8`.

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs` (created, 130 lines, 6 tests)
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationRegistrationTests.cs` (created, 99 lines, 2 tests)

## Decisions Made

- **Test seam = `QueueContainer<T>(registerService, setOptions)` with `ITransportOptionsFactory` stub.** Option A from the plan was viable: in `registerService`, register a NSubstitute-mocked `ITransportOptionsFactory` returning a stub `SqlServerMessageQueueTransportOptions` with the desired option value. In `setOptions`, resolve `IWorkerNotification` from the container — the factory delegate from PLAN-1.1 inspects the stubbed options and selects the correct concrete. This bypasses the real options-loading code path entirely (no SQL connection needed).

- **`CreateConsumer` call wrapped in try/catch.** Downstream resolution after the `setOptions` callback (which is what we actually care about) may throw on the fake connection string. The smoke test only needs the `IWorkerNotification` instance captured inside `setOptions`; any later exception is swallowed because it's irrelevant to the smoke contract.

- **`TransportConfigurationReceive` requires 3 ctor args.** The class doesn't have a parameterless ctor — it takes `IConnectionInformation`, `IQueueDelayFactory`, `IRetryDelayFactory`. The contract tests mock all three via NSubstitute. Discovered at first compile attempt; fixed in-flight before committing Task 1.

- **NSubstitute can't proxy `SqlTransaction` (sealed).** Test 4 (`Transaction_Returns_Underlying_Transaction_When_Set`) was originally intended to assert reference-equality between the holder's `Transaction` and the subject's `Transaction`. The sealed-type proxy block forced a softer assertion: confirm the delegation path runs when a non-null holder is set, with the null-delegation path proven by Test 3. This is a known limitation of NSubstitute and is documented inline in the test file. Real reference-equality coverage will come from Phase 7 integration tests with a live `SqlTransaction` from a real SQL Server.

## Issues Encountered

- **First-attempt compile failure on `TransportConfigurationReceive` ctor.** The plan's example assumed a parameterless constructor; actual ctor requires 3 mocked dependencies. Fixed in 2 Edit calls before the first commit. No SUMMARY-impacting cost.

- **PLAN-2.2 builder agent (background dispatch) stalled mid-investigation** after deep-diving the SimpleInjector seam exploration without writing any tests. Main session completed the work directly. Files committed by main session, not by the builder agent. The agent's research findings were useful (it correctly identified the `registerService` + `setOptions` approach), just incomplete on execution.

## Verification Results

| Gate | Command | Result |
|---|---|---|
| 1 | `dotnet test … --filter "FullyQualifiedName~SqlServerRelationalWorkerNotificationTests"` | **PASS.** 6/6 contract tests pass. |
| 2 | `dotnet test … --filter "FullyQualifiedName~SqlServerRelationalWorkerNotificationRegistrationTests"` | **PASS.** 2/2 smoke tests pass. |
| 3 | Full SqlServer.Tests run | **PASS.** `Failed: 0, Passed: 164, Skipped: 0, Total: 164`. (156 baseline + 6 contract + 2 smoke). |
| 4 | `grep -nE "\b(Tx|TX)\b"` on the two new test files | **PASS.** Zero matches. |

## Commits Created

- `427244a6` — `shipyard(phase-3): add SqlServerRelationalWorkerNotification contract tests`
- `151fbda8` — `shipyard(phase-3): add option-driven SimpleInjector smoke tests for relational notification`

# Build Summary: Plan 2.2

## Status: complete

## Tasks Completed

- **Task 1** — Authored `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationTests.cs` with **6 contract/behavior tests** mirroring Phase 3:
  1. `Constructor_Passes_Args_To_Base`
  2. `Transaction_Returns_Null_When_ConnectionHolder_Not_Set`
  3. `Transaction_Returns_Null_When_ConnectionHolder_Transaction_Is_Null`
  4. `ConnectionHolder_PropertySet_Does_Not_Throw` (named correctly from outset per Phase 3 lesson 4 — `NpgsqlTransaction` is sealed; NSubstitute can't proxy it; full non-null-return coverage deferred to Phase 7 PG integration tests)
  5. `Cast_To_IRelationalWorkerNotification_Succeeds`
  6. `Plain_WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification`

  Commit `deb41e8c`.

- **Task 2** — Authored `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationRegistrationTests.cs` with **2 option-driven SimpleInjector smoke tests**:
  1. `Resolves_Relational_When_HoldTransaction_Enabled` — option=true → cast succeeds.
  2. `Resolves_NonRelational_When_HoldTransaction_Disabled` — option=false → cast fails.

  Test seam = `QueueContainer<PostgreSqlMessageQueueInit>(registerService, setOptions)` + mocked `ITransportOptionsFactory` returning a stub `PostgreSqlMessageQueueTransportOptions`. Mirrors Phase 3 lesson 5 verbatim.

  Commit `9f254fa3`.

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationTests.cs` (created, 137 lines, 6 tests)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationRegistrationTests.cs` (created, 100 lines, 2 tests)

## Decisions Made

- **Test 4 named `ConnectionHolder_PropertySet_Does_Not_Throw` from the outset** per Phase 3 SIMPLIFICATION L1 lesson. Avoided the misleading `Transaction_Returns_Underlying_Transaction_When_Set` name that Phase 3 had to rename post-build.
- **Same test seam as Phase 3 PLAN-2.2** — mocked `ITransportOptionsFactory` via `registerService`, resolve `IWorkerNotification` inside `setOptions`. Try/catch around `qc.CreateConsumer(...)` to swallow downstream resolution errors on the fake connection.

## Issues Encountered

- **One in-flight fix:** First compile attempt of the registration tests used `PostgreSQLMessageQueueInit` (all-caps SQL) — the filename uses all-caps but the actual class is `PostgreSqlMessageQueueInit` (lowercase q) per the PG type-naming convention. Single `sed -i` style fix to replace 2 occurrences. Took ~30 seconds.

## Verification Results

| Gate | Command | Result |
|---|---|---|
| 1 | Contract tests filter run | **PASS.** 6/6 pass in 155ms. |
| 2 | Smoke tests filter run | **PASS.** 2/2 pass (via full suite). |
| 3 | Full PG test suite | **PASS.** 151/151 (baseline 143 + 6 contract + 2 smoke). |
| 4 | Release build of test project | **PASS.** 0 errors. |
| 5 | `Tx`/`TX` grep on both new test files | **PASS.** Exit 1, zero matches. |

## Commits Created

- `deb41e8c` — add PostgreSqlRelationalWorkerNotification contract tests
- `9f254fa3` — add option-driven SimpleInjector smoke tests for relational notification (PG)

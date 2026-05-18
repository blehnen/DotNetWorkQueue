# Plan 3.1: SQLite Inbox Contract Tests + Option-Driven SimpleInjector Smoke Tests

## Context

SQLite counterpart of Phase 3/4 PLAN-2.2. Six contract/behavior tests for `SqLiteRelationalWorkerNotification` + two option-driven SimpleInjector smoke tests proving PROJECT.md §SC #2 for SQLite.

All Phase 3 + Phase 4 lessons baked in:
- Test 4 named `ConnectionHolder_PropertySet_Does_Not_Throw`-style for sealed-type NSubstitute limitation (Phase 3 lesson 4 — note SQLite's case: `SqliteTransaction` is sealed, same limitation applies).
- Smoke test seam = `QueueContainer<SqLiteMessageQueueInit>(registerService, setOptions)` + mocked `ITransportOptionsFactory` returning a stub `SqLiteMessageQueueTransportOptions`.

## Dependencies
PLAN-2.1 (notification class must exist).

## Tasks

### Task 1: Contract/behavior unit tests
**Files:** `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqLiteRelationalWorkerNotificationTests.cs`
**Action:** create
**Description:**

Six `[TestMethod]` tests, mirror Phase 4 PLAN-2.2 Task 1 with SQLite types substituted:

1. `Constructor_Passes_Args_To_Base`
2. `Transaction_Returns_Null_When_State_Not_Set` (or `_When_ConnectionHolder_Not_Set` — adapt to whichever pattern PLAN-1.1/PLAN-2.1 chose)
3. `Transaction_Returns_Null_When_State_Transaction_Is_Null`
4. `Context_State_Read_Does_Not_Throw` (named per Phase 3 lesson 4; the underlying `SqliteTransaction` is sealed and NSubstitute can't proxy it)
5. `Cast_To_IRelationalWorkerNotification_Succeeds`
6. `Plain_WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification`

Use NSubstitute for `IMessageContext` and `IMessageContextData<SqLiteConnectionState>` mocks. `TransportConfigurationReceive` constructor takes 3 sub-deps to mock (same as Phase 3/4).

LGPL header byte-copy.

**Acceptance Criteria:**
- 6 tests, all pass.
- MSTest 3.x assertions (`Assert.IsNull`, `Assert.AreSame`, `Assert.IsTrue/IsFalse`, `Assert.IsInstanceOfType<T>`).
- No `Tx` token.

### Task 2: Option-driven SimpleInjector smoke tests
**Files:** `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqLiteRelationalWorkerNotificationRegistrationTests.cs`
**Action:** create
**Description:**

Two `[TestMethod]` smoke tests proving the factory-delegate registration branches correctly on `EnableHoldTransactionUntilMessageCommitted`:

1. `Resolves_Relational_When_HoldTransaction_Enabled` — option=true → resolved `IWorkerNotification` `is IRelationalWorkerNotification`.
2. `Resolves_NonRelational_When_HoldTransaction_Disabled` — option=false → resolved `IWorkerNotification` is NOT `IRelationalWorkerNotification`.

Test seam: `QueueContainer<SqLiteMessageQueueInit>(registerService, setOptions)` where `registerService` overrides `ITransportOptionsFactory` with an NSubstitute-mocked factory returning a stub `SqLiteMessageQueueTransportOptions` with the desired option value; `setOptions` resolves `IWorkerNotification`.

Use a fake SQLite connection string constant: `"Data Source=:memory:"` or similar.

**Acceptance Criteria:**
- Both tests pass.
- Directly satisfies PROJECT.md §SC #2 for SQLite.

## Verification

```bash
# Gate 1: contract tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SqLiteRelationalWorkerNotificationTests" --nologo 2>&1 | tail -3
# expect 6/6 pass.

# Gate 2: smoke tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SqLiteRelationalWorkerNotificationRegistrationTests" --nologo 2>&1 | tail -3
# expect 2/2 pass.

# Gate 3: full SQLite test suite.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures.

# Gate 4: Tx-token guard.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqLiteRelationalWorkerNotificationTests.cs Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqLiteRelationalWorkerNotificationRegistrationTests.cs
# expect exit 1.
```

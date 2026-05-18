# Plan 2.2: PostgreSqlRelationalWorkerNotification Contract Tests + Option-Driven SimpleInjector Smoke Tests

## Context

PG counterpart of Phase 3 PLAN-2.2. Six contract/behavior tests against the new class shape (NSubstitute-mocked `IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>`), plus two option-driven SimpleInjector smoke tests directly satisfying PROJECT.md §Success Criteria #2 (cast succeeds when option=true; cast fails when option=false).

Phase 3 lessons baked in:
- Lesson 4: `NpgsqlTransaction` is sealed; NSubstitute can't proxy it. Contract test for the non-null-return path is named for what it actually proves (`ConnectionHolder_PropertySet_Does_Not_Throw`-style); full reference-equality coverage deferred to Phase 7.
- Lesson 5: Test seam = `QueueContainer<PostgreSQLMessageQueueInit>(registerService, setOptions)` with mocked `ITransportOptionsFactory` returning a stub `PostgreSqlMessageQueueTransportOptions`.

## Dependencies
PLAN-1.1 (the new class must exist).

## Tasks

### Task 1: Contract/behavior unit tests
**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationTests.cs`
**Action:** create
**Description:**

Create a new test file with the 18-line LGPL-2.1 header byte-copied from `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/ConnectionHolder.cs:1-18`.

Namespace: `DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic`.

Class: `[TestClass] public class PostgreSqlRelationalWorkerNotificationTests`.

Use a private static `CreateSubject(...)` helper that constructs the new class with NSubstitute-mocked six ctor deps (`IHeaders`, `IQueueCancelWork`, `TransportConfigurationReceive` with its three sub-deps `IConnectionInformation`/`IQueueDelayFactory`/`IRetryDelayFactory`, `ILogger`, `IMetrics`, and a real `ActivitySource("test")`).

Six `[TestMethod]` tests:

1. **`Constructor_Passes_Args_To_Base`** — assert the resolved instance's `WorkerStopping`, `HeaderNames`, `Log`, `Metrics`, `Tracer` reference the mocked instances; assert `TransportSupportsRollback` matches the configuration value.
2. **`Transaction_Returns_Null_When_ConnectionHolder_Not_Set`** — fresh instance; assert `Transaction` is `null`.
3. **`Transaction_Returns_Null_When_ConnectionHolder_Transaction_Is_Null`** — mock `IConnectionHolder.Transaction` returns `null`; assign holder; assert `Transaction` is `null`.
4. **`ConnectionHolder_PropertySet_Does_Not_Throw`** — NSubstitute cannot proxy sealed `NpgsqlTransaction`, so this test asserts that assigning a non-null `IConnectionHolder` round-trips on the property without exception. Inline comment notes the sealed-type limitation and defers non-null-return coverage to Phase 7 integration. (Name per Phase 3 SIMPLIFICATION L1 lesson — don't ship `Transaction_Returns_Underlying_Transaction_When_Set` because the assertion would mislead.)
5. **`Cast_To_IRelationalWorkerNotification_Succeeds`** — `Assert.IsTrue(subject is IRelationalWorkerNotification)`; `Assert.IsInstanceOfType<WorkerNotification>(subject)`.
6. **`Plain_WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification`** — sanity tripwire: build a plain `WorkerNotification`; `Assert.IsFalse(plain is IRelationalWorkerNotification)`.

MSTest 3.x assertions only (`Assert.IsNull`, `Assert.AreSame`, `Assert.IsTrue/IsFalse`, `Assert.IsInstanceOfType<T>`, `Assert.IsNotNull`). No FluentAssertions, no AutoFixture. NSubstitute for interface mocks. No `Tx` token.

**Acceptance Criteria:**
- File exists with 18-line LGPL header.
- 6 `[TestMethod]` methods present with the names above.
- All 6 tests pass.
- Grep guard: `grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationTests.cs` → exit 1.

### Task 2: Option-driven SimpleInjector smoke tests
**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationRegistrationTests.cs`
**Action:** create
**Description:**

Create a separate test file with the 18-line LGPL-2.1 header.

Namespace: `DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic`.

Class: `[TestClass] public class PostgreSqlRelationalWorkerNotificationRegistrationTests`.

Two `[TestMethod]` tests using the seam pattern proven in Phase 3 (`Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationRegistrationTests.cs`):

1. **`Resolves_Relational_When_HoldTransaction_Enabled`** — build a `QueueContainer<PostgreSQLMessageQueueInit>(registerService, setOptions)` where:
   - `registerService` stubs `ITransportOptionsFactory` to return a stub `PostgreSqlMessageQueueTransportOptions` with `EnableHoldTransactionUntilMessageCommitted = true`.
   - `setOptions` resolves `IWorkerNotification` from the container and captures it to a local variable.
   - After `using` block, assert the captured notification `is IRelationalWorkerNotification`.
   - Wrap the `qc.CreateConsumer(new QueueConnection("ADMIN", FakeConnection))` call in `try/catch` to swallow downstream resolution exceptions on the fake connection (the smoke only needs the cast from `setOptions`).

2. **`Resolves_NonRelational_When_HoldTransaction_Disabled`** — same pattern with `EnableHoldTransactionUntilMessageCommitted = false`; assert `IsFalse(... is IRelationalWorkerNotification)`.

Use `const string FakeConnection = "Server=localhost;...";` matching the existing PG test convention (`PostgreSqlConnectionInformationTests.cs` or similar).

**Acceptance Criteria:**
- File exists with 18-line LGPL header.
- 2 `[TestMethod]` methods present.
- Both tests pass.
- Grep guard: zero `Tx`/`TX` matches.
- Test 1 directly satisfies PROJECT.md §Success Criteria #2 first half ("cast succeeds when option true").
- Test 2 directly satisfies PROJECT.md §Success Criteria #2 second half ("cast fails when option false").

## Verification

Run from worktree root:

```bash
# Gate 1: contract tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~PostgreSqlRelationalWorkerNotificationTests" --nologo 2>&1 | tail -3
# expect 6/6 pass.

# Gate 2: smoke tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~PostgreSqlRelationalWorkerNotificationRegistrationTests" --nologo 2>&1 | tail -3
# expect 2/2 pass.

# Gate 3: full PG test suite.
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures; test count = baseline + 8.

# Gate 4: Release build of the test project (XML-doc cleanliness on the test files).
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -5
# expect 0 errors.

# Gate 5: Tx-token guards on both test files.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationTests.cs Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalWorkerNotificationRegistrationTests.cs
# expect exit code 1 (no matches).
```

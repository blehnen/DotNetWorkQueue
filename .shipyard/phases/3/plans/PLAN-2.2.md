---
phase: phase-3-sqlserver-inbox
plan: 2.2
wave: 2
dependencies: [1.1]
must_haves:
  - Unit tests cover SqlServerRelationalWorkerNotification ctor forwarding, Transaction null-shape, Transaction delegation, and positive/negative capability-cast cases
  - Container smoke test proves option=true resolves an IRelationalWorkerNotification (cast succeeds)
  - Container smoke test proves option=false resolves an IWorkerNotification that is NOT an IRelationalWorkerNotification (cast fails)
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs
tdd: false
risk: medium
---

# Plan 2.2: Unit tests + option-driven container smoke tests for `SqlServerRelationalWorkerNotification`

## Context

PLAN-1.1 ships the new internal notification class and the option-driven DI
registration. This plan adds the test coverage required by CONTEXT-3 §2 and
ROADMAP Phase 3 success criteria #2 and #3:

- NSubstitute unit tests covering the `Transaction` getter's shape (no holder
  set → null) and delegation (calling `Transaction` reads the holder).
- Capability-cast sanity checks: positive on the relational class, negative on
  the plain base `WorkerNotification`.
- **Two SimpleInjector container smoke tests** validating the option-driven
  registration branch:
  - `Resolves_Relational_When_HoldTransaction_Enabled` — option=true → resolved
    `IWorkerNotification` IS `IRelationalWorkerNotification`.
  - `Resolves_NonRelational_When_HoldTransaction_Disabled` — option=false →
    resolved `IWorkerNotification` is NOT `IRelationalWorkerNotification`.

This pair satisfies PROJECT.md §Success Criteria #2 ("With the option false,
the cast fails on the same transport") at the unit-test layer.

### Test seam for the two smoke tests

The two smoke tests need a way to flip
`SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted`
BEFORE the factory delegate fires. Three viable seams (builder picks whichever
compiles cleanly against the current API):

**Seam A — `QueueContainer<>` register-lambda overload:** `QueueContainer<TInit>`
exposes (or can expose) a constructor overload accepting an
`Action<IContainer> register` callback that runs during container build, before
first resolution. The test mutates the options factory inside the callback:

```csharp
using (var test = new QueueContainer<SqlServerMessageQueueInit>(register: container =>
{
    var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
    var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
    options.EnableHoldTransactionUntilMessageCommitted = true;
}))
{
    // resolve IWorkerNotification through whatever public surface QueueContainer exposes
    // and assert the capability cast
}
```

**Seam B — direct SimpleInjector container build (preferred if Seam A's exact
signature is fuzzy):** construct the container directly, mirroring what
`QueueContainer` does internally:

```csharp
var container = new SimpleInjector.Container();
new ComponentRegistration().SuppressDiagnosticWarnings(container);
ComponentRegistration.RegisterDefaults(container, RegistrationTypes.OneOrMoreQueues);
new SqlServerMessageQueueInit().RegisterImplementations(
    container,
    RegistrationTypes.OneOrMoreQueues,
    new QueueConnection("TestQueue", "Server=fake;Database=db;User ID=sa;Password=password"));

var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
options.EnableHoldTransactionUntilMessageCommitted = true;

var notification = container.GetInstance<IWorkerNotification>();
Assert.IsTrue(notification is IRelationalWorkerNotification);
```

The exact `ComponentRegistration` / `RegistrationTypes` / `ITransportInit` API
surface for Seam B may differ slightly. The BUILDER agent resolves the
specifics at implementation time by inspecting the current `QueueContainer<>`
source and `ComponentRegistration` source — both live in
`Source/DotNetWorkQueue/`.

**Seam C — fallback (`SQLServerMessageQueueInit.SetOptions` style):** if the
init class exposes a public method to pre-seed options before
`RegisterImplementations`, use that. (RESEARCH §7 did not mention such a method,
so this is the least likely path.)

**The plan does NOT prescribe a specific seam.** The builder chooses A, B, or C
based on what compiles cleanly. The plan WILL specify:

- WHAT to assert (two cases: option=true → cast succeeds; option=false → cast fails).
- WHICH test seam family to use (`QueueContainer` register lambda OR direct
  SimpleInjector `Container` build OR an `SqlServerMessageQueueInit` options seam).
- ROUGH shape of the test code (see Task 2 below).

### Why not the existing `Assert.ThrowsExactly<SqlException>` pattern

The pre-existing `QueueCreatorTests.Create_CreateConsumer` pattern proves the
container resolves the full receive pipeline by triggering SQL I/O. It does NOT
let the test inspect the resolved `IWorkerNotification` type directly. For
Phase 3's success criteria the smoke test MUST actually resolve
`IWorkerNotification` and assert the capability-cast outcome — Seams A/B/C are
required because the `SqlException` smoke test cannot prove the cast outcome.

## Dependencies

PLAN-1.1 — requires `SqlServerRelationalWorkerNotification` class and the
option-driven factory-delegate registration.

PLAN-2.1 is NOT required — these tests construct/resolve the notification
directly; they do not exercise the receive path.

## Tasks

### Task 1: Author `SqlServerRelationalWorkerNotificationTests.cs` with shape + capability-cast tests

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs`
**Action:** create
**Description:**

Create a new `[TestClass]` file using MSTest 3.x conventions (`Assert.ThrowsExactly`
NOT `Assert.ThrowsException` per CLAUDE.md lesson). Match the LGPL-2.1 header
convention used by sibling files
(`Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs`
— include the header if siblings have it, omit if they do not).

Namespace: `DotNetWorkQueue.Transport.SqlServer.Tests.Basic`.

Required usings:

- `System.Data.Common` (for `DbTransaction`)
- `System.Diagnostics` (for `ActivitySource`)
- `Microsoft.Data.SqlClient` (for `SqlConnection`, `SqlTransaction`, `SqlCommand`
  generic args)
- `Microsoft.Extensions.Logging` (for `ILogger`)
- `Microsoft.Extensions.Logging.Abstractions` (for `NullLogger`)
- `Microsoft.VisualStudio.TestTools.UnitTesting` (MSTest)
- `NSubstitute`
- `DotNetWorkQueue.Configuration` (for `TransportConfigurationReceive`)
- `DotNetWorkQueue.Queue` (for `WorkerNotification`)
- `DotNetWorkQueue.Transport.RelationalDatabase` (for `IRelationalWorkerNotification`
  and `IConnectionHolder<,,>`)
- `DotNetWorkQueue.Transport.SqlServer.Basic` (for `SqlServerRelationalWorkerNotification`)

Add a private static factory helper to centralize construction:

```csharp
private static SqlServerRelationalWorkerNotification CreateSubject()
{
    var headers = Substitute.For<IHeaders>();
    var cancelWork = Substitute.For<IQueueCancelWork>();
    var configuration = Substitute.For<TransportConfigurationReceive>();
    var metrics = Substitute.For<IMetrics>();
    var tracer = new ActivitySource("DotNetWorkQueue.Tests.SqlServerInbox");
    return new SqlServerRelationalWorkerNotification(
        headers, cancelWork, configuration, NullLogger.Instance, metrics, tracer);
}
```

Test methods (five required, all `[TestMethod]`):

1. **`Constructor_ForwardsArgumentsToBase`** — build the subject and assert
   inherited members reflect the injected dependencies (`HeaderNames`,
   `WorkerStopping`, `Log`, `Metrics`, `Tracer` — confirm exact property names
   against `WorkerNotification.cs` source at implementation time). Proves
   `: base(...)` forwarding is intact.

2. **`Transaction_ReturnsNull_WhenConnectionHolderIsNull`** — build the subject
   without setting `ConnectionHolder`; assert `subject.Transaction` is `null`.

3. **`Transaction_DelegatesToConnectionHolder`** — build the subject; create
   an NSubstitute mock of `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>`;
   assign it; read `subject.Transaction` once; assert
   `holder.Received(1).Transaction` was read. Proves the `=> ConnectionHolder?.Transaction`
   expression dereferences the holder. **Do not try to mock the non-null
   `SqlTransaction` return** — `SqlTransaction` is `sealed` and NSubstitute
   cannot mock it (CLAUDE.md lesson). The "holder.Transaction is non-null"
   branch is exercised by the Phase 7 SqlServer integration tests against a
   live database.

4. **`Subject_ImplementsIRelationalWorkerNotification`** — build the subject;
   assert `subject is IRelationalWorkerNotification` is `true`.

5. **`PlainWorkerNotification_DoesNotImplementIRelationalWorkerNotification`** —
   build a plain `new WorkerNotification(...)` instance using the same 6-arg
   factory shape; assert `plain is IRelationalWorkerNotification` is `false`.
   Sanity check that the marker interface is NOT accidentally on the base.

**Forbidden:**

- `Assert.ThrowsException<T>` (MSTest 2.x API — use `Assert.ThrowsExactly<T>`).
- `(SqlTransaction)` or `(SqlConnection)` casts anywhere.
- `Tx`/`TX` token.
- `Microsoft.Extensions.Configuration` (namespace shadow risk).
- `FluentAssertions` (match existing test style — plain MSTest `Assert.*`).
- `[AssemblyInitialize]` or class fixtures (sibling tests do not use them).

**Acceptance Criteria:**

- File exists at the exact path.
- Five `[TestMethod]` methods with the names above.
- All five pass under `dotnet test`.
- `grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs`
  exits non-zero.
- `grep -n "Assert.ThrowsException<" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs`
  exits non-zero.
- `grep -nE "\(SqlConnection\)|\(SqlTransaction\)" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs`
  exits non-zero.

### Task 2: Add two option-driven container smoke tests (positive + negative)

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs`
**Action:** modify (extends Task 1's file with two additional test methods)
**Description:**

Add two `[TestMethod]` methods to the same file proving the option-driven
factory-delegate registration from PLAN-1.1 branches correctly:

**Test A: `Resolves_Relational_When_HoldTransaction_Enabled`**

Build the SimpleInjector / `QueueContainer` setup (Seam A, B, or C — builder's
choice per the Context section), mutate options so
`EnableHoldTransactionUntilMessageCommitted = true`, resolve
`IWorkerNotification` from the container, and assert:

```csharp
Assert.IsTrue(notification is IRelationalWorkerNotification);
```

**Test B: `Resolves_NonRelational_When_HoldTransaction_Disabled`**

Same setup but with `EnableHoldTransactionUntilMessageCommitted = false` (or
left at its default of false). Resolve `IWorkerNotification` and assert:

```csharp
Assert.IsFalse(notification is IRelationalWorkerNotification);
```

Rough Seam B code shape (the builder is free to switch to Seam A if cleaner):

```csharp
[TestMethod]
public void Resolves_Relational_When_HoldTransaction_Enabled()
{
    var container = new SimpleInjector.Container();
    // ... wire ComponentRegistration / SqlServerMessageQueueInit per Seam B ...
    var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
    var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
    options.EnableHoldTransactionUntilMessageCommitted = true;

    var notification = container.GetInstance<IWorkerNotification>();

    Assert.IsTrue(notification is IRelationalWorkerNotification);
}

[TestMethod]
public void Resolves_NonRelational_When_HoldTransaction_Disabled()
{
    var container = new SimpleInjector.Container();
    // ... same wiring ...
    var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
    var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
    options.EnableHoldTransactionUntilMessageCommitted = false;

    var notification = container.GetInstance<IWorkerNotification>();

    Assert.IsFalse(notification is IRelationalWorkerNotification);
}
```

**The builder agent resolves the exact API surface for Seam A vs B vs C at
implementation time.** What the plan locks in:

- Two named tests with the names above.
- The two `Assert.IsTrue` / `Assert.IsFalse` assertions on
  `notification is IRelationalWorkerNotification`.
- The options factory mutation pattern (resolve factory, call `Create()`,
  flip the bool, then resolve `IWorkerNotification` — the order matters
  because the factory delegate from PLAN-1.1 reads options at resolution
  time).

If neither Seam A nor B is workable (e.g., `SqlServerMessageQueueTransportOptions`
is cached on first `Create()` call so the second mutation doesn't take effect),
the builder may instead build two separate containers — one for each test —
and pre-seed options before the first `IWorkerNotification` resolution.

Additional required usings (add to the file from Task 1):

- `SimpleInjector` (if Seam B is chosen).
- `DotNetWorkQueue.Configuration` (for `QueueConnection`).
- `DotNetWorkQueue.IoC` (for `IContainer` / `RegistrationTypes` if surfaced).
- `DotNetWorkQueue.Transport.SqlServer` (root `SqlServerMessageQueueInit`).

**Forbidden:**

- `Assert.ThrowsException<T>` (MSTest 2.x).
- `(SqlTransaction)` / `(SqlConnection)` casts.
- `Tx`/`TX` token.
- Reaching for a live database connection. The container resolution path must
  not require SQL I/O — the factory delegate reads options before the receive
  pipeline starts, so no SQL is needed.

**Acceptance Criteria:**

- Two new `[TestMethod]` methods named exactly
  `Resolves_Relational_When_HoldTransaction_Enabled` and
  `Resolves_NonRelational_When_HoldTransaction_Disabled` exist in the file.
- The positive test asserts
  `Assert.IsTrue(notification is IRelationalWorkerNotification)`.
- The negative test asserts
  `Assert.IsFalse(notification is IRelationalWorkerNotification)`.
- Both tests pass under `dotnet test`.
- Neither test requires a live SQL connection (no `SqlException` thrown,
  no `Assert.ThrowsExactly<SqlException>` wrapper).
- `grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs`
  exits non-zero.

## Verification

Run from the worktree root `/mnt/f/git/dotnetworkqueue/.worktrees/phase-2-inbox-foundation`:

```bash
# 1. Strict release build of the SqlServer project — must remain clean.
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release -p:CI=true

# 2. All SqlServer unit tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj"

# 3. Filter to just the new tests — proves the seven new tests are green.
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" --filter "FullyQualifiedName~SqlServerRelationalWorkerNotificationTests"

# 4. No Tx token.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs

# 5. No MSTest 2.x API.
grep -n "Assert.ThrowsException<" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs

# 6. No sealed-type casts.
grep -nE "\(SqlConnection\)|\(SqlTransaction\)" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs

# 7. Both option-driven smoke tests exist by exact name.
grep -n "Resolves_Relational_When_HoldTransaction_Enabled\|Resolves_NonRelational_When_HoldTransaction_Disabled" Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs
```

Expected results:

- Command 1: `Build succeeded.` 0 errors; pre-existing NU1902 warnings only.
- Command 2: `Passed! Failed: 0`; total count = baseline + 7 new tests.
- Command 3: `Passed! Failed: 0, Passed: 7` (5 from Task 1 + 2 from Task 2).
- Command 4: exit code 1.
- Command 5: exit code 1.
- Command 6: exit code 1.
- Command 7: exit code 0, exactly two matching lines (one per test name).

# Research: Phase 3 — SqlServer Inbox `IWorkerNotification` Implementation

Scope: SqlServer-transport implementation of `IRelationalWorkerNotification` (defined in Phase 2),
wiring into `SqlServerMessageQueueInit`, and matching unit tests.

---

## §1 `IWorkerNotification` Registration Today

**File:** `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` lines 217–218

```csharp
container.Register<IWorkerNotification, WorkerNotification>(LifeStyles.Transient);
container.Register<IWorkerNotificationFactory, WorkerNotificationFactory>(LifeStyles.Singleton);
```

Lifecycle: **Transient** — a new `WorkerNotification` instance is resolved per message-handler
invocation. This is intentional: `HeartBeat` and `MessageCancellation` are set by the framework
per message after construction (property injection pattern, not constructor injection).

The registration lives in the core `ComponentRegistration.cs`, not in any transport init.
Transport-specific overrides of `IWorkerNotification` must therefore be registered inside the
transport's `RegisterImplementations()` *after* `base.RegisterImplementations(...)` is called,
so the transport binding wins.

---

## §2 `WorkerNotification` Class — Subclassing Surface

**File:** `Source/DotNetWorkQueue/Queue/WorkerNotifications.cs` (class `WorkerNotification`)

- **Visibility:** `public class WorkerNotification : IWorkerNotification`
- **Sealed:** No — the class is not sealed. Safe to subclass.

**Constructor** (lines 41–61):
```csharp
public WorkerNotification(
    IHeaders headerNames,
    IQueueCancelWork cancelWork,
    TransportConfigurationReceive configuration,
    ILogger log,
    IMetrics metrics,
    ActivitySource tracer)
```
All parameters must be non-null (Guard.NotNull on each). SimpleInjector will resolve all of them
from the container.

**Public mutable properties** (set after construction by framework code):
- `WorkerStopping` — `ICancelWork` (get; set;)
- `HeartBeat` — `IWorkerHeartBeatNotification` (get; set;)
- `HeaderNames` — `IHeaders` (get; set;)
- `MessageCancellation` — `IMessageCancellation` (get; set;) — defaults to `MessageCancellationNoOp.Instance`

**Read-only properties** (set from constructor arguments):
- `TransportSupportsRollback` — `bool`
- `Log` — `ILogger`
- `Metrics` — `IMetrics`
- `Tracer` — `System.Diagnostics.ActivitySource`

**Subclassing approach for Phase 3:**
The new `SqlServerWorkerNotification` class should:
1. Extend `WorkerNotification` (calls `base(...)` ctor passing through the same 6 params).
2. Add `IRelationalWorkerNotification` to the interface list.
3. Add a `DbTransaction? Transaction { get; }` property that delegates to an injected
   `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>`.

The `IRelationalWorkerNotification` interface (Phase 2 deliverable) must define `Transaction` as
`System.Data.Common.DbTransaction?` (abstract base class) — not `IDbTransaction` and not the
concrete `SqlTransaction` — to keep the interface provider-agnostic.

---

## §3 SqlServer `MessageQueueInit` Registration Seam

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`
**Method:** `RegisterImplementations(IContainer container, RegistrationTypes registrationType, QueueConnection queueConnection)`

The override call order (lines 55–80):
```
base.RegisterImplementations(container, registrationType, queueConnection);  // line 58
init.RegisterStandardImplementations(...);                                     // line 61
// Phase 3 outbox registrations — lines 64–73
// all other SqlServer-specific bindings
```

**Where to insert the inbox registration:**
After line 73 (end of outbox block) and before the `//override` comment at line 75.
The new line:
```csharp
container.Register<IWorkerNotification, SqlServerWorkerNotification>(LifeStyles.Transient);
```
This overrides the core `WorkerNotification` binding from `ComponentRegistration.cs:217` because
`RegisterImplementations` runs after the base registrations. SimpleInjector resolves the last
matching `Register` for a non-conditional binding — the transport's binding wins.

No `RegisterConditional` is needed here because there is only one `IWorkerNotification` binding per
container instance (one transport per container).

---

## §4 `ConnectionHolder` Access — Transaction Seam and Scoping

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/ConnectionHolder.cs`

Key facts:
- Class is `internal` — not directly injectable into the new notification class.
- Implements `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>` — this IS the injectable type.
- `Transaction` property (lines 89–101): `public SqlTransaction Transaction { get; set; }` — fully accessible via the interface as `TTransaction Transaction`.
- `Transaction` is null when `EnableHoldTransactionUntilMessageCommitted` is false (no transaction held).
- `IConnectionHolder` is registered as **Transient** (line 124 of Init):
  ```csharp
  container.Register<IConnectionHolderFactory<...>, ConnectionHolderFactory>(LifeStyles.Singleton);
  ```
  The factory is a Singleton; `ConnectionHolderFactory.Create()` constructs a new `ConnectionHolder`
  per call (opens a real `SqlConnection` in the ctor — line 50).

**Scoping resolution — CRITICAL:**

`IWorkerNotification` is resolved **Transient**. `IConnectionHolder` is NOT directly registered with
the container; it is produced by `IConnectionHolderFactory.Create()`. The receive path
(`SQLServerMessageQueueReceive`) calls `_connectionHolderFactory.Create()` itself and passes the
resulting `IConnectionHolder` instance through to `ReceiveMessage` and message-context delegates.

Therefore `ConnectionHolder` is **not a container-resolved type** — it cannot be constructor-injected
into `SqlServerWorkerNotification` by the container.

The correct seam is one of:
- **Option A (recommended):** Expose a mutable `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>? ConnectionHolder` property on `SqlServerWorkerNotification`. The receive path (`SQLServerMessageQueueReceive`) sets it after resolving the notification from the container, the same way it already sets `HeartBeat` and `MessageCancellation` post-construction. The `Transaction` property on the notification class then delegates to `ConnectionHolder?.Transaction`.
- **Option B:** Pass `IConnectionHolderFactory` as a ctor parameter and call `Create()` inside the notification. This is wrong: it would open a second database connection that is not associated with the in-flight message.

The `Transaction` getter must handle null gracefully:
```csharp
public DbTransaction? Transaction => _connectionHolder?.Transaction;
```
This returns null when no `ConnectionHolder` has been set, or when `EnableHoldTransactionUntilMessageCommitted` is false (transaction is null on the holder itself).

**Lifecycle summary:** `SqlServerWorkerNotification` is Transient (one per message). The `ConnectionHolder` reference is injected post-construction by `SQLServerMessageQueueReceive` before the user handler is invoked. This matches the existing pattern for `HeartBeat`.

---

## §5 Existing SimpleInjector `Verify()` Smoke Tests — Style Reference

**File:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs`

Test class: `QueueCreatorTests` (lines 10–60)
Pattern used throughout:

```csharp
[TestMethod]
public void Create_CreateProducer()
{
    var queue = "TestQueue";
    using (var test = new QueueContainer<SqlServerMessageQueueInit>())
    {
        Assert.ThrowsExactly<Microsoft.Data.SqlClient.SqlException>(
            delegate
            {
                test.CreateProducer<FakeMessage>(new QueueConnection(queue, GoodConnection));
            });
    }
}
```

Notes for the architect:
- Uses `Assert.ThrowsExactly<T>` (MSTest 3.x API — not the MSTest 2.x `ThrowsException<T>`).
- `QueueContainer<SqlServerMessageQueueInit>()` is the container-under-test.
- The test does NOT call `container.Verify()` directly; instead it exercises the container by
  resolving a real producer/consumer, which triggers SimpleInjector's internal verification.
- A smoke test for the new `IWorkerNotification` override should follow the same shape: wrap in a
  `QueueContainer<SqlServerMessageQueueInit>()` and resolve enough of the container to trigger
  verification.
- `FakeMessage` class is defined in `Helpers.cs` in the same test project.

Test file naming: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/<ClassName>Tests.cs`
(e.g., `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerWorkerNotificationTests.cs`).
Decorator tests go in `Decorator/<DecoratorClassName>Tests.cs`.

---

## §6 Transport-Option Access at Registration Time — Pattern A vs Pattern B

**Context:** `ConnectionHolder` opens a transaction only when
`SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted == true`.
The notification class must expose `null` for `Transaction` when the option is false.
This is handled naturally by `ConnectionHolder` itself (it leaves `_sqlTransaction = null`).
No DI branch based on the option value is needed.

**However**, if a future phase needs to branch DI registrations on an option value at registration
time, both patterns are used in the codebase:

**Pattern A — Late-binding factory lambda (used in `SQLServerMessageQueueInit.cs` lines 110–114):**
```csharp
container.Register<IBaseTransportOptions>(() =>
{
    try { return (IBaseTransportOptions)container.GetInstance<ITransportOptionsFactory>().Create(); }
    catch { return new SqlServerMessageQueueTransportOptions(); }
}, LifeStyles.Singleton);
```
The lambda captures `container` and resolves the options at first-use time, not at registration time.
This avoids ordering issues where options aren't loaded yet during `RegisterImplementations()`.

**Pattern B — Init-time branch via `SetDefaultsIfNeeded` (lines 219–225):**
Options-driven defaults are applied in `SetDefaultsIfNeeded()`, which runs after
`RegisterImplementations()` and after options have been loaded from the queue. This is the correct
hook for options-driven DI conditional logic that cannot use late-binding.

**Recommendation for Phase 3:** Neither pattern is needed. The `Transaction` null-or-not behavior
is fully controlled by `ConnectionHolder`'s constructor (lines 53–56 of `ConnectionHolder.cs`).
The notification class simply reads what is already there.

---

## §7 Outbox Milestone — Option-Driven DI Branch Reference

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`

The outbox milestone used `RegisterConditional` (lines 66–73) to preempt open-generic fallbacks
from `ComponentRegistration.RegisterFallbacks`. The comment at lines 66–70 explains the rationale:
`RegisterConditional` preserves lazy-verification semantics; plain `Register` on open generics
triggers eager verification that surfaces pre-existing diagnostic warnings.

For inbox Phase 3, plain `Register<IWorkerNotification, SqlServerWorkerNotification>` is correct
(closed generic, not open-generic) — `RegisterConditional` is not needed.

The other outbox-milestone DI precedent in this file: `ExternalTransactionValidator` registered as
Singleton at line 65. This confirms the pattern for new Singleton dependencies added by inbox work.

---

## §8 Test File Naming and Location Convention

**Transport.SqlServer.Tests directory structure:**
```
Source/DotNetWorkQueue.Transport.SqlServer.Tests/
  Basic/
    Command/          -- command-object tests
    CommandHandler/   -- command handler tests
    Factory/          -- factory tests
    <ClassName>Tests.cs  -- other Basic-namespace types
  Decorator/          -- decorator tests
  Schema/             -- schema tests
  Helpers.cs          -- shared FakeMessage, test helpers
  QueueCreatorTests.cs
  SqlConnectionInformationTests.cs
```

**New test file location:**
`Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerWorkerNotificationTests.cs`

**Naming convention:** `{ClassName}Tests.cs` in the subdirectory matching the source file's
namespace segment. `SqlServerWorkerNotification` lives in `Basic/`, so tests go in `Basic/`.

**Test class conventions observed:**
- `[TestClass]` attribute; no base class.
- `Assert.ThrowsExactly<T>` for exception assertions (MSTest 3.x).
- NSubstitute for mocking. AutoFixture for data generation where used.
- FluentAssertions 6.12.2 for assertion style (pinned per MEMORY.md — plan to migrate to MSTest assertions eventually).
- No `[DataRow]` / parameterized tests in most SqlServer unit tests; simple `[TestMethod]` methods.

---

## §9 Risks and Pitfalls for Phase 3 Architect

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Sealed-type cast: writing `(SqlTransaction)connectionHolder.Transaction` or casting `IDbConnection` to `SqlConnection` inside the notification class | Medium | High — breaks NSubstitute mocking, `TypeLoadException` at test time | Keep all handler/notification code on `IConnectionHolder<,,>` interface; expose `Transaction` as `DbTransaction?` (abstract base), never as `SqlTransaction` |
| Async mock split: if any test mocks `IDbConnection` for an async path | Low | Medium — async methods silently no-op via NSubstitute on interfaces | Use abstract `DbConnection`/`DbCommand` base classes for async handler mocks; keep notification tests sync-only |
| MSTest API: using `Assert.ThrowsException<T>` (MSTest 2.x) instead of `Assert.ThrowsExactly<T>` (MSTest 3.x) | Low | Low — compile error, caught immediately | Follow `QueueCreatorTests.cs` pattern exactly |
| `Tx` abbreviation drift: plan or code using `Tx` for "transaction" | Low | Low — style violation caught in review | Identifier must be `Transaction` (full word); repo convention and MEMORY.md both require this |
| `DotNetWorkQueue.IConfiguration` shadow: new files in `DotNetWorkQueue.Transport.SqlServer.*` namespace that import `Microsoft.Extensions.Configuration` | Low | Medium — silent resolution to wrong type, CS0246 or runtime errors | Use `global::Microsoft.Extensions.Configuration.IConfiguration` if ever referenced; Phase 3 inbox code does not need MS config at all |
| `ConnectionHolder` double-open: injecting `IConnectionHolderFactory` into notification class and calling `Create()` | Low | High — opens a second SQL connection per message, leaks connections | Set `ConnectionHolder` via property post-construction from `SQLServerMessageQueueReceive`, same as `HeartBeat` pattern |
| SimpleInjector diagnostic warning on Transient `IDisposable`: `SqlServerWorkerNotification` will inherit `WorkerNotification` which does not implement `IDisposable` — no risk here | N/A | N/A | No action needed; `WorkerNotification` is not disposable |

---

## Phase 3 Architect Handoff Summary

1. **Registration location:** Add `container.Register<IWorkerNotification, SqlServerWorkerNotification>(LifeStyles.Transient)` inside `SQLServerMessageQueueInit.RegisterImplementations()` after the outbox block (~line 73). This overrides the core binding. No `RegisterConditional` needed.

2. **Transaction access pattern:** `ConnectionHolder` is not container-resolved — it is created by `IConnectionHolderFactory.Create()` in the receive path. The notification class must expose a settable `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>?` property; `SQLServerMessageQueueReceive` sets it post-construction before invoking the user handler, mirroring how `HeartBeat` is set.

3. **`Transaction` property type:** `System.Data.Common.DbTransaction?` (abstract base class). Never `SqlTransaction` (sealed) or `IDbTransaction` (interface). The `ConnectionHolder.Transaction` is `SqlTransaction` but it is assignable to `DbTransaction` — cast is safe and tested.

4. **Test smoke test style:** Match `QueueCreatorTests.cs` — `QueueContainer<SqlServerMessageQueueInit>()` + `Assert.ThrowsExactly<SqlException>` to trigger container verification without a live SQL server. `Assert.ThrowsExactly` is the MSTest 3.x API (not `ThrowsException`).

5. **Unit test location:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerWorkerNotificationTests.cs`. Mock `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>` with NSubstitute (it is an interface — safe to mock). Test cases: null holder returns null transaction; non-null holder with null transaction returns null; non-null holder with transaction returns the `DbTransaction`.

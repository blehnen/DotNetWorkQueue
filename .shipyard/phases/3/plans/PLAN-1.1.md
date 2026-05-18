---
phase: phase-3-sqlserver-inbox
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Author internal class SqlServerRelationalWorkerNotification subclassing WorkerNotification and implementing IRelationalWorkerNotification
  - Register both concrete notification classes (relational + plain WorkerNotification) so the factory delegate can resolve either
  - Register IWorkerNotification via a factory delegate that inspects SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted and returns the relational impl when true, the plain impl when false
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
tdd: false
risk: medium
---

# Plan 1.1: Author `SqlServerRelationalWorkerNotification` + option-driven DI registration

## Context

Phase 3 ships the SqlServer half of the inbox feature. This plan creates the new
internal notification class that exposes the active dequeue `DbTransaction` to user
handlers and wires an **option-driven** DI registration in `SqlServerMessageQueueInit`.

The class subclasses `DotNetWorkQueue.Queue.WorkerNotification` (per CONTEXT-3 §3
inheritance lock) and additionally implements
`DotNetWorkQueue.Transport.RelationalDatabase.IRelationalWorkerNotification`
(Phase 2 deliverable). The `Transaction` member delegates to a settable
`IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>` property; the
receive path (PLAN-2.1) sets that property post-construction, mirroring how
`HeartBeat` is set today (`HeartBeatWorker.cs:89`). No constructor injection of
`IConnectionHolder` — that holder is NOT container-resolved and is only available
on the receive path (`SqlServerMessageQueueReceive.GetConnectionAndSetOnContext`).

### Registration: option-driven branch (corrects CRITIQUE finding)

Per `PROJECT.md` §Functional New Public API lines 35-38 and §Success Criteria #2,
the `IWorkerNotification` binding MUST branch on
`SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted`:

- option `true` → resolved instance implements `IRelationalWorkerNotification`
  (cast succeeds; `Transaction` is the live dequeue transaction).
- option `false` → resolved instance is the plain `WorkerNotification` (cast
  fails; capability-cast pattern protects user handlers).

The factory-delegate registration pattern in SimpleInjector encodes this
branch. Both concrete classes are pre-registered so the delegate can resolve
either at message-receive time:

```csharp
container.Register<SqlServerRelationalWorkerNotification>(LifeStyles.Transient);
container.Register<WorkerNotification>(LifeStyles.Transient);
container.Register<IWorkerNotification>(() =>
{
    var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
    var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
    return options.EnableHoldTransactionUntilMessageCommitted
        ? (IWorkerNotification)container.GetInstance<SqlServerRelationalWorkerNotification>()
        : container.GetInstance<WorkerNotification>();
}, LifeStyles.Transient);
```

The factory delegate is resolved per `IWorkerNotification` request (transient),
so option flips between scope creations are respected. The same pattern is
already used at `SQLServerMessageQueueInit.cs:110-114` for `IBaseTransportOptions`.

**Why this design satisfies the Phase 2 interface contract:** Phase 2's XML doc
on `IRelationalWorkerNotification.Transaction` says "Never null when the
containing interface is implemented". With option-driven registration, the
interface is only implemented when option=true, and option=true means the
receive path will set `ConnectionHolder` to a holder whose `Transaction` is
non-null (per `SQLServerMessageQueueReceive.GetConnectionAndSetOnContext`
behavior — see PLAN-2.1). The cast and the non-null guarantee are coupled at
the registration boundary.

## Dependencies

None — this plan is the foundation. PLAN-2.1 and PLAN-2.2 depend on this plan.

## Tasks

### Task 1: Create `SqlServerRelationalWorkerNotification.cs`

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs`
**Action:** create
**Description:**

Create a new internal class file with the exact LGPL-2.1 18-line header used
elsewhere in the project (byte-identical to the header in
`Source/DotNetWorkQueue.Transport.SqlServer/Basic/ConnectionHolder.cs`).

Namespace: `DotNetWorkQueue.Transport.SqlServer.Basic`.

Required usings:

- `System.Data.Common` (for `DbTransaction`)
- `System.Diagnostics` (for `ActivitySource` ctor parameter)
- `Microsoft.Data.SqlClient` (for `SqlConnection`/`SqlTransaction`/`SqlCommand`
  generic args)
- `Microsoft.Extensions.Logging` (for `ILogger`)
- `DotNetWorkQueue.Configuration` (for `TransportConfigurationReceive`)
- `DotNetWorkQueue.Queue` (for the `WorkerNotification` base class)
- `DotNetWorkQueue.Transport.RelationalDatabase` (for `IRelationalWorkerNotification`
  and `IConnectionHolder<,,>`)

Class declaration (exact spelling — CONTEXT-3 §1 user lock):

```csharp
internal class SqlServerRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
```

Constructor MUST match `WorkerNotification`'s 6-parameter signature and forward
all parameters to `base(...)` unchanged. SimpleInjector resolves all six from the
container with no additional plumbing:

```csharp
public SqlServerRelationalWorkerNotification(
    IHeaders headerNames,
    IQueueCancelWork cancelWork,
    TransportConfigurationReceive configuration,
    ILogger log,
    IMetrics metrics,
    ActivitySource tracer)
    : base(headerNames, cancelWork, configuration, log, metrics, tracer)
{
}
```

Add a public mutable property `ConnectionHolder` for post-construction
injection by `SqlServerMessageQueueReceive`. Type is the interface (the
concrete `ConnectionHolder` class is internal):

```csharp
public IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> ConnectionHolder { get; set; }
```

Implement the `Transaction` member from `IRelationalWorkerNotification` as a
null-safe delegation. The underlying `IConnectionHolder<,,>.Transaction` is
`SqlTransaction` which IS assignable to `DbTransaction` via implicit upcast:

```csharp
public DbTransaction Transaction => ConnectionHolder?.Transaction;
```

The `?.` operator returns null when no holder is set yet (between construction
and `SqlServerMessageQueueReceive.GetConnectionAndSetOnContext`). In the
option=true path that this class is registered for, the receive path always
sets `ConnectionHolder` before user-handler invocation, and the holder's
`Transaction` is non-null per `ConnectionHolder.cs:53-56` behavior. The null
fallback is defensive only.

XML doc requirements (Release builds enable `GenerateDocumentationFile`):

- `<summary>` on the class — describes it as the SqlServer inbox-pattern
  implementation of `IRelationalWorkerNotification`. Cross-reference
  `IRelationalWorkerNotification`, `WorkerNotification`, and the
  `EnableHoldTransactionUntilMessageCommitted` option.
- `<remarks>` on the class — explain the property-injection pattern: the
  `ConnectionHolder` is set post-construction by `SqlServerMessageQueueReceive`
  before the user handler is invoked, mirroring how `HeartBeat` is set today.
- `<inheritdoc/>` on the `Transaction` property.
- `<summary>` + `<value>` on the `ConnectionHolder` property.
- XML doc on the constructor with `<param>` entries matching the base class doc.

**Forbidden:**

- No sealed-type casts. Never write `(SqlTransaction)` anywhere in this file.
- No `Tx`/`TX` token. Full word `Transaction` (or `ConnectionHolder`) only.
- No `Microsoft.Extensions.Configuration` reference.
- Do NOT re-implement any base member.

**Acceptance Criteria:**

- File exists at `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs`.
- First 18 lines are the LGPL-2.1 header byte-identical to `ConnectionHolder.cs`'s
  header.
- Class is declared `internal class SqlServerRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification`
  exactly.
- Constructor forwards all 6 parameters to `base(...)`.
- Public property `ConnectionHolder` is `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>`
  with `get` + `set`.
- `Transaction` getter is `=> ConnectionHolder?.Transaction;` returning
  `System.Data.Common.DbTransaction`.
- `grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs`
  exits non-zero.
- `grep -nE "\(SqlConnection\)|\(SqlTransaction\)" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs`
  exits non-zero.
- XML doc present on class, ctor, and `ConnectionHolder` property; `<inheritdoc/>`
  on `Transaction`.

### Task 2: Register option-driven factory delegate in `SqlServerMessageQueueInit`

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`
**Action:** modify
**Description:**

Insert THREE new `Register` calls in `RegisterImplementations(...)` after the
outbox-pattern block (current lines 63–73) and before the
`//override so that we can use schema as needed` comment at line 75.

The new block is:

```csharp
// Phase 3: inbox-pattern receive wiring (SqlServer side).
// Pre-register both concrete notification classes so the factory delegate
// below can resolve either. The IWorkerNotification binding then branches on
// EnableHoldTransactionUntilMessageCommitted: option=true returns the
// relational variant (which implements IRelationalWorkerNotification), option=false
// returns the plain WorkerNotification (capability-cast fails on the user side).
container.Register<SqlServerRelationalWorkerNotification>(LifeStyles.Transient);
container.Register<WorkerNotification>(LifeStyles.Transient);
container.Register<IWorkerNotification>(() =>
{
    var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
    var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
    return options.EnableHoldTransactionUntilMessageCommitted
        ? (IWorkerNotification)container.GetInstance<SqlServerRelationalWorkerNotification>()
        : container.GetInstance<WorkerNotification>();
}, LifeStyles.Transient);
```

Lifestyle MUST be `LifeStyles.Transient` for all three registrations — matches
the core registration in `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs:217`.
`HeartBeat` and `MessageCancellation` are set per-message after construction, so
a new instance per resolve is required.

Notes on type names that may need fully-qualified references:

- `ITransportOptionsFactory` — the SqlServer-specific factory interface in
  `DotNetWorkQueue.Transport.SqlServer.Basic` (or `.Configuration` — confirm via
  inspection of `SQLServerMessageQueueInit.cs:110-114` which already uses the
  same factory).
- `SqlServerMessageQueueTransportOptions` — concrete options class, namespace
  `DotNetWorkQueue.Transport.SqlServer.Basic`.
- `WorkerNotification` — `DotNetWorkQueue.Queue`. Add a `using` if not already
  present in the file.

Use `RegisterConditional` ONLY for the existing outbox open-generic block;
`IWorkerNotification` is a closed type so plain `Register` is correct.

Do NOT touch any other line in `SQLServerMessageQueueInit.cs`.

**Forbidden:**

- Do NOT add an unconditional `Register<IWorkerNotification, SqlServerRelationalWorkerNotification>`
  line. The CRITIQUE confirmed that violates PROJECT.md §Functional New Public
  API and §Success Criteria #2 (option=false → cast must fail).
- Do NOT use `Tx`/`TX` token in the new comment.

**Acceptance Criteria:**

- Three new `container.Register` calls are present between the existing line
  73 and the existing comment `//override so that we can use schema as needed`.
- One registers `SqlServerRelationalWorkerNotification` as transient.
- One registers `WorkerNotification` as transient.
- One registers `IWorkerNotification` via factory delegate that inspects
  `SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted`
  and returns the relational impl when true, the plain impl when false.
- NO `container.Register<IWorkerNotification, SqlServerRelationalWorkerNotification>`
  unconditional line exists anywhere in the file.
- `grep -n "container.Register<IWorkerNotification>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`
  returns exactly one match (the factory delegate line).
- `grep -nE "container\.Register<IWorkerNotification,\s*SqlServerRelationalWorkerNotification>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`
  exits non-zero (no unconditional binding).
- `grep -n "EnableHoldTransactionUntilMessageCommitted" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`
  returns at least one new match inside the factory delegate body.

## Verification

Run from the worktree root `/mnt/f/git/dotnetworkqueue/.worktrees/phase-2-inbox-foundation`:

```bash
# 1. Strict release build — proves CS1591 clean, no analyzer/warning errors,
#    no missing usings, no nullable mismatches.
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release -p:CI=true

# 2. Token discipline — no Tx abbreviation in the new file.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs

# 3. No sealed-type casts in the new file.
grep -nE "\(SqlConnection\)|\(SqlTransaction\)" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs

# 4. Factory delegate registration is present (exactly one).
grep -n "container.Register<IWorkerNotification>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs

# 5. No unconditional override registration was added by accident.
grep -nE "container\.Register<IWorkerNotification,\s*SqlServerRelationalWorkerNotification>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs

# 6. The factory delegate body references the option.
grep -n "EnableHoldTransactionUntilMessageCommitted" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
```

Expected results:

- Command 1: `Build succeeded.` with 0 errors. Pre-existing NU1902 warnings are
  acceptable; zero new CS1591 / CS8XXX / IDE warnings introduced.
- Command 2: exit code 1 (no matches).
- Command 3: exit code 1 (no matches).
- Command 4: exit code 0, exactly one matching line.
- Command 5: exit code 1 (no matches).
- Command 6: exit code 0, at least one new match inside the factory delegate.

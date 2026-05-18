# Plan 1.1: Author `PostgreSqlRelationalWorkerNotification` + Factory-Delegate DI Registration

## Context

Phase 4 PostgreSQL counterpart of Phase 3's SqlServer wave 1. New `internal` class subclasses `WorkerNotification` and implements Phase 2's `IRelationalWorkerNotification`. DI registration in `PostgreSQLMessageQueueInit` uses a factory delegate that inspects `PostgreSqlMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted` and selects the relational impl (option=true) or the plain `WorkerNotification` (option=false, capability cast cleanly fails per PROJECT.md §Functional New Public API).

**Phase 3 lessons baked into this plan:**
- Try/catch around options resolution (lesson 1) — Phase 3 broke 6 tests by initially omitting this; Phase 4 ships it on the first commit.
- No `Register<WorkerNotification>(LifeStyles.Transient)` self-registration (lesson 2) — core already binds it.

## Dependencies
None — this plan is the foundation. PLAN-2.1 and PLAN-2.2 depend on this plan.

## Tasks

### Task 1: Create `PostgreSqlRelationalWorkerNotification.cs`
**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs`
**Action:** create
**Description:**

Create the new internal class with the exact 18-line LGPL-2.1 header byte-copied from `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/ConnectionHolder.cs` lines 1-18.

Namespace: `DotNetWorkQueue.Transport.PostgreSQL.Basic`.

Required usings:
- `System.Data.Common` (for `DbTransaction`)
- `System.Diagnostics` (for `ActivitySource` in base ctor)
- `Npgsql` (for `NpgsqlConnection`/`NpgsqlTransaction`/`NpgsqlCommand` generic args)
- `Microsoft.Extensions.Logging` (for `ILogger`)
- `DotNetWorkQueue.Configuration` (for `TransportConfigurationReceive`)
- `DotNetWorkQueue.Queue` (for `WorkerNotification` base)
- `DotNetWorkQueue.Transport.RelationalDatabase` (for `IRelationalWorkerNotification` + `IConnectionHolder<,,>`)

Class declaration:
```csharp
internal class PostgreSqlRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
```

Constructor forwards all six args to `: base(headerNames, cancelWork, configuration, log, metrics, tracer)` — same six params as `WorkerNotification` per `Source/DotNetWorkQueue/Queue/WorkerNotifications.cs:41-46`.

Public settable property for receive-path post-construction injection:
```csharp
public IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> ConnectionHolder { get; set; }
```

Implements `IRelationalWorkerNotification.Transaction`:
```csharp
/// <inheritdoc/>
public DbTransaction Transaction => ConnectionHolder?.Transaction;
```

Full XML doc on the class (capability-cast pattern, property-injection lifecycle, cross-reference to `WorkerNotification.HeartBeat` precedent), on the ctor (`<summary>` + 6 `<param>` entries), and on the `ConnectionHolder` property (`<summary>` + `<value>` explaining the receive-path injection).

**Acceptance Criteria:**
- File exists at the specified path with the 18-line LGPL header.
- Class is `internal` (per ROADMAP and CONTEXT-4).
- Inheritance list is exactly `: WorkerNotification, IRelationalWorkerNotification`.
- `Transaction` property type is `DbTransaction` (abstract base from `System.Data.Common`), NOT `NpgsqlTransaction` or `IDbTransaction`.
- `ConnectionHolder` property is `public` with `get; set;`.
- All public members carry XML doc (`<summary>` minimum; `<remarks>` where the design decision is non-obvious).
- Grep guard 1: `grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs` → exit 1 (no matches).
- Grep guard 2: `grep -nE "\(NpgsqlConnection\)\|\(NpgsqlTransaction\)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs` → exit 1 (no sealed-type casts).

### Task 2: Register factory delegate in `PostgreSQLMessageQueueInit.cs`
**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs`
**Action:** modify
**Description:**

Insert the inbox factory-delegate registration block AFTER the outbox `RegisterConditional` block (ending at line 74) and BEFORE the `//**all` general-registrations comment (line 76). Required `using` directives at the top of the file: confirm `DotNetWorkQueue.Queue` is present (for `WorkerNotification`); add if missing.

Insert this block (with the leading blank line for separation):

```csharp

// Phase 4: inbox-pattern receive wiring (PostgreSQL side).
// Pre-register the relational concrete so the factory delegate below can resolve it.
// WorkerNotification is already registered by the core (ComponentRegistration line 217)
// and is auto-resolvable as a concrete type without a separate self-registration.
// The IWorkerNotification binding branches on EnableHoldTransactionUntilMessageCommitted:
// option=true returns the relational variant (implements IRelationalWorkerNotification),
// option=false returns the plain WorkerNotification (capability-cast fails on the user side).
// The try/catch around options resolution mirrors the IBaseTransportOptions pattern
// below (line ~99) — at container.Verify() / early-resolution time options may not
// be loadable yet, so fall back to the default option value (false) which is the
// safe non-relational path.
container.Register<PostgreSqlRelationalWorkerNotification>(LifeStyles.Transient);
container.Register<IWorkerNotification>(() =>
{
    bool holdTransaction;
    try
    {
        var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
        var options = (PostgreSqlMessageQueueTransportOptions)optionsFactory.Create();
        holdTransaction = options.EnableHoldTransactionUntilMessageCommitted;
    }
    catch
    {
        holdTransaction = false;
    }
    return holdTransaction
        ? (IWorkerNotification)container.GetInstance<PostgreSqlRelationalWorkerNotification>()
        : container.GetInstance<WorkerNotification>();
}, LifeStyles.Transient);
```

DO NOT use `RegisterConditional` here (closed generic, single binding per container).
DO NOT add an unconditional `container.Register<IWorkerNotification, PostgreSqlRelationalWorkerNotification>` — the option-driven branch is mandated by PROJECT.md §Functional New Public API.

**Acceptance Criteria:**
- The factory-delegate block exists between line 74 and the `//**all` comment.
- Both concrete pre-registration (`PostgreSqlRelationalWorkerNotification`) and the factory delegate (`IWorkerNotification`) are present.
- The factory delegate inspects `PostgreSqlMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted` and includes the try/catch fallback to `false`.
- Grep anti-pattern: `grep -nE "container\.Register<IWorkerNotification,\s*PostgreSqlRelationalWorkerNotification>" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` → exit 1 (no unconditional override).
- Grep factory-delegate present: `grep -n "container.Register<IWorkerNotification>(" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` → exit 0, one or more matches.
- All existing `Transport.PostgreSQL.Tests` continue to pass (no regression in container.Verify at test time, thanks to the try/catch).

## Verification

Run from worktree root:

```bash
# Gate 1: Release build clean (both TFMs).
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -8
# expect "Build succeeded." and "0 Error(s)"; NU1902 warnings tolerated; NO CS1591.

# Gate 2: Tx-token grep guard on the new file.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs
# expect exit code 1 (no matches).

# Gate 3: sealed-type cast grep guard on the new file.
grep -nE "\(NpgsqlConnection\)|\(NpgsqlTransaction\)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs
# expect exit code 1 (no matches).

# Gate 4: factory-delegate present + unconditional-override absent.
grep -n "container.Register<IWorkerNotification>(" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
# expect one match.
grep -nE "container\.Register<IWorkerNotification,\s*PostgreSqlRelationalWorkerNotification>" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
# expect exit code 1 (no matches).

# Gate 5: existing tests still pass (no new tests in PLAN-1.1; tests come from PLAN-2.2).
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures, test count unchanged from baseline.
```

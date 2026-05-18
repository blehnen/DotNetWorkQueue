# Plan 2.1: SQLite Inbox Notification + Factory-Delegate DI + Receive-Path Wire-Up

## Context

SQLite counterpart of Phase 3/4 PLAN-1.1 + PLAN-2.1 merged. Builds on PLAN-1.1's new hold-tx infrastructure (`SqLiteConnectionState` on `IMessageContext`). All five Phase 3 lessons + Phase 4 carry-overs baked in from the outset.

## Dependencies
PLAN-1.1 (hold-tx state model + `SqLiteConnectionState` on context must exist).

## Tasks

### Task 1: Create `SqLiteRelationalWorkerNotification` class
**Files:** `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteRelationalWorkerNotification.cs`
**Action:** create
**Description:**

New internal class subclassing `WorkerNotification` + implementing Phase 2's `IRelationalWorkerNotification`.

Constructor signature: 6 args matching `WorkerNotification` base PLUS `IMessageContext context` (or `IMessageContextData<SqLiteConnectionState>` typed header — pick whichever resolution works with SimpleInjector lifecycle).

`Transaction` property delegates to the context-stored state:
```csharp
public DbTransaction Transaction
{
    get
    {
        var state = _context.Get(_headers.ConnectionState);
        return (DbTransaction)state?.Transaction;
    }
}
```

(If `IMessageContext` is not directly resolvable as a per-message scoped dep — likely Transient unaware — store a delegate or use the `IConnectionInformation` accessor pattern. Builder to confirm at execution time and pick whichever resolution works without breaking the `IWorkerNotification` Transient lifecycle.)

Full XML doc on the class and the new behavior (mirrors Phase 3/4 docs with the SQLite-specific note about reading from `IMessageContext` instead of a settable `ConnectionHolder` property).

LGPL-2.1 18-line header byte-copy from `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueInit.cs:1-18`.

**Acceptance Criteria:**
- File exists, internal, subclasses `WorkerNotification` + implements `IRelationalWorkerNotification`.
- `Transaction` property returns `DbTransaction` from context-state or null when state absent.
- No `Tx` abbreviation.
- No sealed-type casts.
- Release build clean.

### Task 2: Factory-delegate DI registration in `SqLiteMessageQueueSharedInit`
**Files:** `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs`
**Action:** modify
**Description:**

Add the inbox factory-delegate registration block (mirrors Phase 3/4 PLAN-1.1 Task 2 exactly with SQLite types substituted):

```csharp
// Phase 5: inbox-pattern receive wiring (SQLite side).
// Pre-register the relational concrete; WorkerNotification is core-bound.
// Factory delegate inspects EnableHoldTransactionUntilMessageCommitted with try/catch
// fallback to false (mirrors IBaseTransportOptions pattern; Phase 3 lesson 1).
container.Register<SqLiteRelationalWorkerNotification>(LifeStyles.Transient);
container.Register<IWorkerNotification>(() =>
{
    bool holdTransaction;
    try
    {
        var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
        var options = (SqLiteMessageQueueTransportOptions)optionsFactory.Create();
        holdTransaction = options.EnableHoldTransactionUntilMessageCommitted;
    }
    catch { holdTransaction = false; }
    return holdTransaction
        ? (IWorkerNotification)container.GetInstance<SqLiteRelationalWorkerNotification>()
        : container.GetInstance<WorkerNotification>();
}, LifeStyles.Transient);
```

Add `using DotNetWorkQueue.Queue;` if missing (Phase 4 carry-over — RESEARCH.md §7 noted gap; PLAN-1.1 Task 1 should have already added it for `SqLiteConnectionState` use, but verify).

**Acceptance Criteria:**
- Factory-delegate block present, try/catch wired correctly.
- No unconditional `Register<IWorkerNotification, SqLiteRelationalWorkerNotification>`.
- Existing test count unchanged (no new tests in this plan).

### Task 3: Receive-path tx-state visibility
**Files:** `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueReceive.cs` (modify)
**Action:** modify
**Description:**

PLAN-1.1 Task 3 already stores `SqLiteConnectionState` on `IMessageContext` when option=true. PLAN-2.1's Task 3 only needs to verify the new notification can READ that state — no additional wiring needed if PLAN-1.1's state model is correct.

If PLAN-1.1 stored the state on context, this task is a no-op (just a verification step). If PLAN-1.1 chose to keep the state internal to the receive class, this task MUST surface it on context via `context.Set(...)` so the notification can read it. Builder to confirm during execution.

**Acceptance Criteria:**
- If state is on context after PLAN-1.1: this task is a verification check (no code change). Document the no-op.
- If state needs surfacing: add a single `context.Set(_sqLiteHeaders.ConnectionState, state)` call in the appropriate receive-path location.

## Verification

```bash
# Gate 1: Release build clean.
dotnet build "Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -5
# expect 0 errors.

# Gate 2: tests still pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures (test count unchanged — tests come from PLAN-3.1).

# Gate 3: factory-delegate present + unconditional absent.
grep -n "container.Register<IWorkerNotification>(" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs
# expect one match.
grep -nE "container\.Register<IWorkerNotification,\s*SqLiteRelationalWorkerNotification>" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs
# expect exit 1.

# Gate 4: Tx-token + sealed-cast grep guards.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteRelationalWorkerNotification.cs
grep -nE "\(SqliteConnection\)|\(SqliteTransaction\)" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteRelationalWorkerNotification.cs
# both expect exit 1.
```

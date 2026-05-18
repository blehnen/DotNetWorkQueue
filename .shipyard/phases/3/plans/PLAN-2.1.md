---
phase: phase-3-sqlserver-inbox
plan: 2.1
wave: 2
dependencies: [1.1]
must_haves:
  - GetConnectionAndSetOnContext sets ConnectionHolder on the relational notification when (and only when) the resolved IWorkerNotification is the relational variant
  - Pattern-match cast (is SqlServerRelationalWorkerNotification) so non-relational registrations (e.g., JobScheduler no-op factory, tests) are untouched
  - No regression in pre-existing SqlServerMessageQueueReceive tests
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs
tdd: false
risk: low
---

# Plan 2.1: Wire `ConnectionHolder` onto the relational notification in the receive path

## Context

PLAN-1.1 created `SqlServerRelationalWorkerNotification` with a settable
`ConnectionHolder` property and registered an option-driven factory delegate for
`IWorkerNotification`. When `EnableHoldTransactionUntilMessageCommitted = true`,
the factory delegate resolves to the relational variant; when false, it resolves
to the plain `WorkerNotification`. For the `Transaction` getter to surface a
non-null `DbTransaction` to the user handler, the receive path must set
`ConnectionHolder` on the resolved notification **when** the resolved type is
the relational variant.

The existing receive path
(`Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`,
class `SqlServerMessageQueueReceive`) already constructs the `IConnectionHolder`
via `_connectionFactory.Create()` and stashes it on the
`IMessageContext` via `context.Set(_sqlHeaders.Connection, connection)` (lines
164–165). The `IMessageContext` also exposes the resolved `IWorkerNotification`
via its `WorkerNotification` property (`Source/DotNetWorkQueue/Messages/MessageContext.cs:140`).

The seam mirrors how `HeartBeatWorker` sets the heartbeat:

```csharp
context.WorkerNotification.HeartBeat = heartBeatNotificationFactory.Create(_cancel.Token);
```

For the inbox feature the equivalent assignment is:

```csharp
if (context.WorkerNotification is SqlServerRelationalWorkerNotification relationalNotification)
{
    relationalNotification.ConnectionHolder = connection;
}
```

The `is` pattern is REQUIRED. It keeps the receive path tolerant of:

- **Option=false case:** factory delegate resolved the plain `WorkerNotification`
  (no `ConnectionHolder` property exists on it; cast fails; assignment skipped).
- **JobScheduler no-op path:** uses `WorkerNotificationFactoryNoOp` which returns
  a different `IWorkerNotification` impl; cast fails; assignment skipped.
- **Test doubles:** unit tests can swap the DI registration; cast fails; no-op.

This wire-up is invisible at the public API surface — `IReceiveMessages`,
`IMessageContext`, and `IConnectionHolder` are untouched. PROJECT.md §Functional
Internal Implementation is satisfied.

## Dependencies

PLAN-1.1 — depends on `SqlServerRelationalWorkerNotification` existing as a
compile-time reference target.

## Tasks

### Task 1: Set `ConnectionHolder` on the relational notification after creating the connection

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`
**Action:** modify
**Description:**

In the private method `GetConnectionAndSetOnContext(IMessageContext context)`
(currently lines 162–180), insert a pattern-matched assignment immediately AFTER
the existing line:

```csharp
var connection = _connectionFactory.Create();
```

and BEFORE the existing line:

```csharp
context.Set(_sqlHeaders.Connection, connection);
```

The new lines are:

```csharp
// Phase 3: inbox-pattern wiring. When the option-driven DI registration
// (SqlServerMessageQueueInit) resolves IWorkerNotification to the relational
// variant, plumb the active ConnectionHolder onto it so user handlers can
// read notification.Transaction as the in-flight dequeue DbTransaction.
// The pattern-match cast keeps the receive path tolerant of the option=false
// case (plain WorkerNotification) and of JobScheduler / test-double impls.
if (context.WorkerNotification is SqlServerRelationalWorkerNotification relationalNotification)
{
    relationalNotification.ConnectionHolder = connection;
}
```

The `is` pattern is mandatory — a blind cast to
`SqlServerRelationalWorkerNotification` would throw `InvalidCastException`
whenever the registered impl is the base `WorkerNotification` (option=false
case, JobScheduler's `WorkerNotificationFactoryNoOp` path, or test doubles).

No other lines in `SQLServerMessageQueueReceive.cs` are modified. The existing
`_configuration.Options().EnableHoldTransactionUntilMessageCommitted` branches
(lines 110, 130, 168, 251) are untouched — they manage commit / rollback
delegate wiring, which is independent of the notification's holder reference.

Add `using DotNetWorkQueue.Transport.SqlServer.Basic;` only if the file's
top-of-file `using` block does not already cover it. Since
`SqlServerRelationalWorkerNotification` lives in the same namespace as the
receive class, no explicit using is needed — the type binds via the file's own
namespace.

**Forbidden:**

- Do NOT cast to `SqlServerRelationalWorkerNotification` directly without an `is`
  pattern.
- Do NOT introduce a sealed-type cast on `connection` (already typed as
  `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>`).
- Do NOT use `Tx` token in the new comment or variable name. Use
  `relationalNotification`.
- Do NOT add a second branch on
  `_configuration.Options().EnableHoldTransactionUntilMessageCommitted`. The
  branch lives at the DI layer (PLAN-1.1) — the receive path's job is just to
  plumb the holder onto whatever type the DI resolved.

**Acceptance Criteria:**

- The new `if (context.WorkerNotification is SqlServerRelationalWorkerNotification relationalNotification)`
  block sits between `var connection = _connectionFactory.Create();` and
  `context.Set(_sqlHeaders.Connection, connection);` in
  `GetConnectionAndSetOnContext`.
- `git diff Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`
  shows only additions.
- `grep -n "is SqlServerRelationalWorkerNotification" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`
  returns exactly one match.
- `grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`
  exits non-zero.
- The strict release build (verification command 1 below) passes clean.
- **Pre-existing `SqlServerMessageQueueReceive`-related tests still pass** (no
  regression in the SqlServer unit test project — see verification command 4).

## Verification

Run from the worktree root `/mnt/f/git/dotnetworkqueue/.worktrees/phase-2-inbox-foundation`:

```bash
# 1. Strict release build — must remain clean after the receive-path edit.
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release -p:CI=true

# 2. Exactly one is-pattern setter for the relational notification.
grep -n "is SqlServerRelationalWorkerNotification" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs

# 3. No Tx token added.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs

# 4. Existing SqlServer unit tests still pass — the change is non-breaking.
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj"
```

Expected results:

- Command 1: `Build succeeded.` with 0 errors. NU1902 warnings carry forward;
  zero new warnings introduced.
- Command 2: exit code 0, exactly one matching line.
- Command 3: exit code 1 (no matches).
- Command 4: `Passed! Failed: 0`, total ≥ existing baseline (no regression in
  any pre-existing `SqlServerMessageQueueReceive`-touching test).

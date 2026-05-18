# Plan 2.1: Wire `ConnectionHolder` into `PostgreSqlRelationalWorkerNotification` from Receive Path

## Context

PG counterpart of Phase 3 PLAN-2.1. The receive path's `GetConnectionAndSetOnContext` creates the `ConnectionHolder` per message; this plan adds a single pattern-match block to inject that holder into the resolved `IWorkerNotification` if (and only if) it's the `PostgreSqlRelationalWorkerNotification` variant. The setter is a no-op on the option-false path because the factory delegate from PLAN-1.1 returns a plain `WorkerNotification` in that case, which fails the pattern-match cleanly.

Phase 3 lesson 3 baked in: pattern-match (not cast) handles both option paths in a single statement.

## Dependencies
PLAN-1.1 (the new `PostgreSqlRelationalWorkerNotification` class must exist and be the factory-delegate's option=true target).

## Tasks

### Task 1: Add pattern-match setter in `PostgreSQLMessageQueueReceive.GetConnectionAndSetOnContext`
**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs`
**Action:** modify
**Description:**

Find `GetConnectionAndSetOnContext(IMessageContext context)` at line 153. Just before `return connection;` (line ~170), add:

```csharp
// Phase 4 inbox: if the resolved IWorkerNotification is the relational variant (selected
// by PostgreSQLMessageQueueInit's factory delegate when EnableHoldTransactionUntilMessageCommitted
// is true), inject the per-message ConnectionHolder so the user handler can read
// the active dequeue transaction via the IRelationalWorkerNotification capability cast.
// When the option is false, context.WorkerNotification is a plain WorkerNotification and the
// pattern-match fails — no-op, no harm.
if (context.WorkerNotification is PostgreSqlRelationalWorkerNotification relationalNotification)
{
    relationalNotification.ConnectionHolder = connection;
}
```

No new `using` directives required — `PostgreSqlRelationalWorkerNotification` lives in the same `DotNetWorkQueue.Transport.PostgreSQL.Basic` namespace.

**Acceptance Criteria:**
- The 8-line block (comment + `if` + setter) is inserted just before the existing `return connection;` line in `GetConnectionAndSetOnContext`.
- Pattern-match uses `is X variable` form (not unconditional cast).
- All existing `PostgreSQLMessageQueueReceive`-related tests in `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/` still pass.
- Grep guard: `grep -nE "\b(Tx|TX)\b"` on the added lines only — zero matches.

## Verification

Run from worktree root:

```bash
# Gate 1: Release build clean (both TFMs).
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -8
# expect "Build succeeded." and "0 Error(s)".

# Gate 2: existing PG tests still pass (no new tests in PLAN-2.1).
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures; test count unchanged from baseline.

# Gate 3: confirm the new pattern-match is present.
grep -n "is PostgreSqlRelationalWorkerNotification relationalNotification" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs
# expect one match inside GetConnectionAndSetOnContext.
```

# Build Summary: Plan 2.1

## Status: complete

## Tasks Completed

- **Task 1** — Wired `ConnectionHolder` into the relational notification from the PostgreSQL receive path. Modified `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs`, edit inside `GetConnectionAndSetOnContext(IMessageContext context)` after `context.Cleanup += Context_Cleanup;` and just before `return connection;`. Added the standard 12-line pattern-match block:

  ```csharp
  if (context.WorkerNotification is PostgreSqlRelationalWorkerNotification relationalNotification)
  {
      relationalNotification.ConnectionHolder = connection;
  }
  ```

  Commit `1b4dddbe`.

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs` (modified — +12 lines; no new using directives needed since `PostgreSqlRelationalWorkerNotification` shares the same `DotNetWorkQueue.Transport.PostgreSQL.Basic` namespace).

## Decisions Made

- **Pattern-match (not cast)** per Phase 3 lesson 3. Cast would throw `InvalidCastException` when option=false because the factory delegate returns a plain `WorkerNotification` in that path; pattern-match makes the setter a clean no-op on the non-relational path.
- **Edit placement** after the existing Commit/Rollback/Cleanup wiring and just before `return connection;` — matches Phase 3 SqlServer exactly.

## Issues Encountered

None. Phase 3 pattern applied verbatim; first-try success.

## Verification Results

| Gate | Command | Result |
|---|---|---|
| 1 | Release build of `Transport.PostgreSQL` | **PASS.** 0 errors. |
| 2 | `Transport.PostgreSQL.Tests` full run | **PASS.** 143/143 tests still pass (no new tests in PLAN-2.1; tests come from PLAN-2.2). Zero regressions in existing `PostgreSQLMessageQueueReceive`-related tests. |
| 3 | Pattern-match block present at the expected location | **PASS.** `grep -n "is PostgreSqlRelationalWorkerNotification relationalNotification" Source/...Receive.cs` finds one match inside `GetConnectionAndSetOnContext`. |

## Commits Created

- `1b4dddbe` — wire ConnectionHolder into IRelationalWorkerNotification from receive path (PG)

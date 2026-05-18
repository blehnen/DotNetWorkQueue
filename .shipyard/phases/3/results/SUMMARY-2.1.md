# Build Summary: Plan 2.1

## Status: complete

## Tasks Completed

- **Task 1** — Wired `ConnectionHolder` into the relational notification from the SqlServer receive path. Modified `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs`, edit inside `GetConnectionAndSetOnContext(IMessageContext context)` just before `return connection;`. The new ~12-line pattern-match block:

  ```csharp
  if (context.WorkerNotification is SqlServerRelationalWorkerNotification relationalNotification)
  {
      relationalNotification.ConnectionHolder = connection;
  }
  ```

  When `EnableHoldTransactionUntilMessageCommitted = true`, the factory delegate from Wave 1 returns a `SqlServerRelationalWorkerNotification`; the pattern-match succeeds and the receive path injects the per-message `ConnectionHolder`. When option is false, `context.WorkerNotification` is a plain `WorkerNotification`; the pattern-match fails cleanly (no-op).

  Commit `c146a554`.

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs` (modified — +~12 lines inside `GetConnectionAndSetOnContext`; no new `using` directives needed since `SqlServerRelationalWorkerNotification` shares the same `DotNetWorkQueue.Transport.SqlServer.Basic` namespace).

## Decisions Made

- **Edit placement = just before `return connection;`** in `GetConnectionAndSetOnContext`. This is after the existing `context.Set(_sqlHeaders.Connection, connection)` and after the `Commit`/`Rollback`/`Cleanup` delegate wiring, so the receive context is fully constructed when the relational notification's `ConnectionHolder` is set. Earlier placement (before the `context.Set`) would invert the ordering and risk handler invocation seeing a partially-wired context.
- **Pattern-match (`is X`) instead of unconditional cast.** Cast would throw `InvalidCastException` when option=false because `context.WorkerNotification` is a plain `WorkerNotification` in that path. Pattern-match makes the setter a clean no-op on the non-relational path, matching the capability-cast design (PROJECT.md §Functional New Public API).
- **No new using directives.** `SqlServerRelationalWorkerNotification` and `SQLServerMessageQueueReceive` both live in `DotNetWorkQueue.Transport.SqlServer.Basic`, so the type resolves implicitly.

## Issues Encountered

None. The Wave 1 lessons (factory-delegate try/catch + class-internal placement) carried over cleanly to the receive-path wiring without further surprises.

## Verification Results

| Gate | Command | Result |
|---|---|---|
| 1 | `dotnet build Source/DotNetWorkQueue.Transport.SqlServer/...csproj -c Release -p:CI=true --nologo` | **PASS.** 0 errors, 14 NU1902 pre-existing advisory warnings (accepted carry-forward). Both net10.0 and net8.0 targets. |
| 2 | `dotnet test Source/DotNetWorkQueue.Transport.SqlServer.Tests/...csproj --nologo` | **PASS.** 156/156 tests pass. Zero regressions in existing `SQLServerMessageQueueReceive`-related tests. |
| 3 | `grep -nE "\b(Tx|TX)\b"` on the modified file (additions only) | **PASS.** Zero matches in the new lines. Existing file's word-boundary tokens unaffected. |

## Commits Created

- `c146a554` — `shipyard(phase-3): wire ConnectionHolder into IRelationalWorkerNotification from receive path`

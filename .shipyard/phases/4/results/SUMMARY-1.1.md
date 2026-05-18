# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed

- **Task 1** — Authored `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs`. Internal class subclassing `WorkerNotification` and implementing `IRelationalWorkerNotification`. Settable `IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> ConnectionHolder { get; set; }`. `DbTransaction Transaction => ConnectionHolder?.Transaction;`. Full XML doc (mirrors Phase 3's SqlServer counterpart). 18-line LGPL header byte-identical to `ConnectionHolder.cs`. Commit `a38aebac`.

- **Task 2** — Added factory-delegate registration in `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` between the outbox `RegisterConditional` block (line 74) and the `//**all` general registrations (line 76). Added `using DotNetWorkQueue.Queue;` directive (was missing from PG init; SqlServer init had it). Try/catch fallback to `EnableHoldTransactionUntilMessageCommitted = false` on options-load failure baked in from the outset (Phase 3 lesson 1 applied — mirrors `IBaseTransportOptions` precedent at the same file line ~99). Commit `61ff56c1`.

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs` (created, 91 lines)
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` (modified — +31 lines: factory-delegate block + 1 new `using`)

## Decisions Made

- **Added `using DotNetWorkQueue.Queue;`** to PG init (SqlServer init already had it; PG was missing it). Required for `WorkerNotification` resolution inside the factory delegate.
- **Followed Phase 3 lesson 1 (try/catch in factory delegate) from the outset** — Phase 3 hit 6 broken tests by initially omitting this and self-fixed mid-build. Phase 4 shipped it on the first commit. No regression in 143 existing PG tests.
- **Followed Phase 3 lesson 2 (no `Register<WorkerNotification>` self-registration)** — PG init pre-registers only `PostgreSqlRelationalWorkerNotification`; `WorkerNotification` is auto-resolved via core registration.

## Issues Encountered

- **None.** All five Phase 3 lessons applied verbatim; first-try success on both commits.

## Verification Results

| Gate | Command | Result |
|---|---|---|
| 1 | `dotnet build Source/DotNetWorkQueue.Transport.PostgreSQL/...csproj -c Release -p:CI=true` | **PASS.** 0 errors, 14 NU1902 pre-existing carry-forward warnings. Both net10.0 and net8.0 targets. |
| 2 | `grep -nE "\b(Tx\|TX)\b" Source/...PostgreSqlRelationalWorkerNotification.cs` | **PASS.** Exit 1, zero matches. |
| 3 | `grep -nE "\(NpgsqlConnection\)\|\(NpgsqlTransaction\)" Source/...PostgreSqlRelationalWorkerNotification.cs` | **PASS.** Exit 1, zero matches. |
| 4 | Factory-delegate present + unconditional-override absent in init | **PASS.** Exactly one `container.Register<IWorkerNotification>(()=>...)` line; zero unconditional `<IWorkerNotification, PostgreSqlRelationalWorkerNotification>` forms. |
| 5 | `dotnet test Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/...csproj` | **PASS.** 143/143 baseline tests pass; zero regressions from DI change. |

## Commits Created

- `a38aebac` — add PostgreSqlRelationalWorkerNotification class
- `61ff56c1` — register IWorkerNotification via factory delegate on hold-transaction option (PG)

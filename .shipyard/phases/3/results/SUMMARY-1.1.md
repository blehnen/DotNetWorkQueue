# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed

- **Task 1** — Authored `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs`. Internal class subclassing `WorkerNotification` and implementing `IRelationalWorkerNotification`. Settable `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> ConnectionHolder { get; set; }`; `DbTransaction Transaction => ConnectionHolder?.Transaction;`. Full XML doc. 18-line LGPL header byte-identical to `ConnectionHolder.cs`. Commit `d446f6fd`.

- **Task 2** — Added factory-delegate registration in `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` between the outbox block (~line 73) and the `//override` schema comment (~line 92). The `IWorkerNotification` resolution branches on `EnableHoldTransactionUntilMessageCommitted`:
  - Option `true` → returns `SqlServerRelationalWorkerNotification` (capability cast succeeds).
  - Option `false` → returns plain `WorkerNotification` (capability cast cleanly fails).
  Commit `a3e18c20` initial; commit `ce6c79c3` follow-up adding try/catch guard against options-load failures during container resolution (see Issues Encountered below).

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs` (created, 96 lines including XML doc + LGPL header)
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` (modified — +22 lines inserted at the inbox registration block)

## Decisions Made

- **Dropped redundant `container.Register<WorkerNotification>(LifeStyles.Transient)` self-registration.** `WorkerNotification` is registered by the core at `ComponentRegistration.cs:217` and is auto-resolvable as a concrete type via `container.GetInstance<WorkerNotification>()` without a separate self-registration in the SqlServer override block. Avoids a duplicate-registration footgun if `AllowOverridingRegistrations` ever changes. The plan's original two-pre-registration form was not strictly necessary; one pre-registration (the new relational class) suffices.

- **Added try/catch guard around options resolution** in the factory delegate. Mirrors the existing `IBaseTransportOptions` pattern at the same file line ~110-114. At container.Verify() / early-resolution time, `optionsFactory.Create()` can throw `SqlException` trying to read persisted options before a real DB is reachable; without the guard, an `ActivationException` would wrap the `SqlException` and trigger an `InvalidOperationException` from SimpleInjector, breaking 6 pre-existing `QueueCreatorTests` that expected the bare `SqlException` at create-time. The catch falls back to `EnableHoldTransactionUntilMessageCommitted = false` — the safe non-relational path. Real production callers will have options loaded by the time the notification resolves, so the relational impl is still selected when the user enables hold-tx.

- **`Transaction` property declaration uses `?.` against `ConnectionHolder`.** `IRelationalWorkerNotification.Transaction` is typed as non-nullable `DbTransaction` per Phase 2's interface contract ("Never null when the containing interface is implemented"). The `?.` returns null when `ConnectionHolder` is unset (e.g., before the receive path sets it post-construction in PLAN-2.1). This is acceptable because the receive path always sets `ConnectionHolder` before invoking the user handler when the relational impl is in scope, and the SqlServer project has NRT disabled so `CS8603` (possible null reference return) is not raised.

## Issues Encountered

- **6 existing `QueueCreatorTests` failed after initial PLAN-1.1 commits (`d446f6fd`, `a3e18c20`).** Tests expected `Assert.ThrowsExactly<SqlException>` at consumer/producer creation; got `InvalidOperationException` wrapping a `SimpleInjector.ActivationException` wrapping the `SqlException`. Root cause: the factory delegate's `optionsFactory.Create()` call eagerly tried to load persisted options from the (fake-connection-string) database during container resolution, before the test's resolution path could surface the SqlException directly.

  **Fix (commit `ce6c79c3`):** Added try/catch fallback to default `EnableHoldTransactionUntilMessageCommitted = false` on options-load failure. Mirrors the precedent in the same file at line ~110 for `IBaseTransportOptions`. Existing tests now 156/156 green.

  **Lesson for plan authors / future researchers:** When a factory delegate in `SQLServerMessageQueueInit` does ANY container resolution that touches the database (or anything else that might fail at container.Verify time), it MUST include a try/catch fallback. The codebase precedent (the `IBaseTransportOptions` block) already documented this, and the researcher noted Pattern A — the plan should have specified the try/catch from the outset.

## Verification Results

| Gate | Command | Result |
|---|---|---|
| 1 | `dotnet build Source/DotNetWorkQueue.Transport.SqlServer/...csproj -c Release -p:CI=true` | **PASS.** `Build succeeded. 14 Warning(s) [all NU1902 pre-existing] 0 Error(s)`. Both net10.0 and net8.0 targets. No CS1591. |
| 2 | `grep -nE "\b(Tx|TX)\b" Source/...SqlServerRelationalWorkerNotification.cs` | **PASS.** Exit 1, zero matches. |
| 3 | `grep -nE "\(SqlConnection\)|\(SqlTransaction\)" Source/...SqlServerRelationalWorkerNotification.cs` | **PASS.** Exit 1, zero matches. No sealed-type casts. |
| 4 | `grep -n "container.Register<IWorkerNotification," Source/...SQLServerMessageQueueInit.cs` | **PASS.** Exit 1, no unconditional `<IWorkerNotification, SqlServerRelationalWorkerNotification>` form. Factory-delegate `container.Register<IWorkerNotification>(()=>...)` form IS present. |
| 5 | `dotnet test Source/DotNetWorkQueue.Transport.SqlServer.Tests/...csproj` | **PASS.** `Failed: 0, Passed: 156, Skipped: 0, Total: 156, Duration: 15 s`. No regressions; no new tests added in PLAN-1.1 (tests come from PLAN-2.2). |

## Commits Created

- `d446f6fd` — add SqlServerRelationalWorkerNotification class
- `a3e18c20` — register IWorkerNotification via factory delegate on hold-transaction option
- `ce6c79c3` — guard inbox notification factory delegate against options load failure (lesson-driven follow-up; see Issues Encountered)

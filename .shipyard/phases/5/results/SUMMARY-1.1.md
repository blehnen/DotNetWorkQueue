# Build Summary: Plan 1.1 — SQLite Hold-Transaction Implementation

## Status: complete

## Tasks Completed

- **Task 1** — Authored `SqLiteConnectionState` + `SqLiteHeaders` (Approach B per CONTEXT-5 §3a). Registered `SqLiteHeaders` singleton in `SqLiteMessageQueueSharedInit`. Added `using DotNetWorkQueue.Queue;` (Phase 4 PG carry-over gap fixed in advance). Commit `fbc6f037`.

- **Task 2** — Restructured `ReceiveMessageQueryHandler.Handle` to branch on caller-supplied connection/transaction (hold-tx path) vs self-create (existing behavior). Caller-supplied path does NOT dispose; self-create path uses `try/finally`. Commit `6b701632`.

- **Task 3** — Wired hold-tx state through receive-path lifecycle:
  - `ReceiveMessage.GetMessage` creates connection+tx when option=true, passes to `Handle`, stores `SqLiteConnectionState` on context, includes leak guards.
  - `SqLiteMessageQueueReceive.ContextOn{Commit,Rollback,Cleanup}` read state from context and commit/rollback/dispose the held resources.
  - New ctor deps wired into both classes (DI resolves automatically).
  
  Commit `31879446`.

## Files Modified

- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteConnectionState.cs` (new, 90 lines)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteHeaders.cs` (new, 49 lines)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs` (+10 lines: `using DotNetWorkQueue.Queue;` + `SqLiteHeaders` registration)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/QueryHandler/ReceiveMessageQueryHandler.cs` (refactor — `Handle` method body restructured for caller-supplied vs self-create branching; +24 lines net)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/Message/ReceiveMessage.cs` (substantive — +60 lines: new deps + hold-tx connection creation + state-on-context + leak guards)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueReceive.cs` (+~30 lines: new dep + commit/rollback/cleanup hold-tx delegate logic)

## Decisions Made

- **Approach B (context-state-based) over Approach A (typed `ConnectionHolder<,,>`):** confirmed at execution time. SQLite has an existing `IConnectionHeader<IDbConnection, IDbTransaction, IDbCommand>` registration in `SqLiteMessageQueueSharedInit` but it requires an `IConnectionHolder<,,>` implementation as the value type — neither SQLite nor `Transport.RelationalDatabase` provides a generic `ConnectionHolder<IDbConnection, ...>` concrete. Building one would have added new abstractions for one transport. The custom `SqLiteHeaders.ConnectionState` keyed `IMessageContextData<SqLiteConnectionState>` is lighter and self-contained.

- **`ReceiveMessage` got 4 new ctor deps** (`IDbFactory`, `IConnectionInformation`, `ISqLiteMessageQueueTransportOptionsFactory`, `SqLiteHeaders`). All resolvable via existing DI registrations in `SqLiteMessageQueueSharedInit`. No new DI plumbing needed.

- **`SqLiteMessageQueueReceive` got 1 new ctor dep** (`SqLiteHeaders`) for the commit/rollback/cleanup delegate logic.

- **Order of ops in ContextOnCommit/ContextOnRollback:** existing `_handleMessage.CommitMessage.Commit(context)` runs FIRST (preserves existing message-bookkeeping order), THEN the held tx commits/rolls back. Critical because `CommitMessage.Commit` writes to the message-status table; if the tx commits first, the bookkeeping write happens outside the held tx and could be visible before the user's business writes are durable.

- **`SqLiteConnectionState.Completed` flag** guards against double-commit/double-rollback in cleanup. Defensive — won't fire in normal flow but protects against edge cases (e.g., a custom user delegate firing both commit and rollback in error scenarios).

- **Leak guards in `ReceiveMessage.GetMessage`:** `try/catch` around `Handle` disposes held resources on throw; `if (receivedTransportMessage == null)` disposes when no message dequeued (held resources never reach context).

## Issues Encountered

- **One build artifact issue (transient):** during one test run, file-copy contention from a leftover background test process caused `Aq.ExpressionJsonSerializer.dll` to fail to copy. Recovered by rebuilding cleanly. Not a code defect.

- **No code issues.** Approach B confirmed viable on first attempt. All test runs after each task came back green (142/142 baseline preserved on every iteration).

## Verification Results

| Gate | Result |
|---|---|
| Task 1 — Release build (Transport.SQLite, both TFMs, `-p:CI=true`) | **PASS.** 0 errors, 14 NU1902 pre-existing. |
| Task 2 — full SQLite test suite | **PASS.** 142/142 (option-false path unchanged; new caller-supplied path covered by PLAN-3.1 smoke tests later). |
| Task 3 — full SQLite test suite (final) | **PASS.** 142/142. |
| Grep guards: no `Tx`/`TX` token in new files | **PASS.** Verified across `SqLiteConnectionState.cs`, `SqLiteHeaders.cs`, plus Task 2/3 additions. |
| Grep guards: no `(SqliteConnection)` / `(SqliteTransaction)` sealed casts | **PASS.** All hold-tx code uses `IDbConnection`/`IDbTransaction` interface-level access. |
| `EnableHoldTransactionUntilMessageCommitted` now read in production code | **PASS.** Added 1 read in `Message/ReceiveMessage.cs` (the gate that creates the connection+tx). Previously: 0 reads in the entire transport. |

## Commits Created

- `fbc6f037` — add SqLiteConnectionState + SqLiteHeaders for hold-tx state
- `6b701632` — branch ReceiveMessageQueryHandler on caller-supplied connection/tx
- `31879446` — wire SQLite hold-tx state through receive path lifecycle

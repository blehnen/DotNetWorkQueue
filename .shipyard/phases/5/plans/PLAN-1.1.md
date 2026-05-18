# Plan 1.1: SQLite Hold-Transaction Implementation (Foundational)

## Context

SQLite has `EnableHoldTransactionUntilMessageCommitted` declared at `SQLiteMessageQueueTransportOptions.cs:278` but the option is **never read anywhere in the transport** (RESEARCH.md §2 verdict). The dequeue transaction is created inside `using` blocks in `ReceiveMessageQueryHandler.Handle()` and disposed before the user handler runs. This plan adds hold-transaction semantics to SQLite for the first time — the prerequisite for Phase 5's inbox notification wiring (PLAN-2.1).

**Architectural decision: Approach B — context-state-based.**

Rather than introducing a new typed `ConnectionHolder<IDbConnection, IDbTransaction, IDbCommand>` wrapper (Approach A), this plan stores the active connection and transaction directly on `IMessageContext` via the existing `context.Set(IMessageContextData<T>, T)` API. Rationale:
- SQLite's existing receive code is interface-based (`IDbFactory`, `IDbConnection`, `IDbTransaction`) — no typed-holder pattern exists today.
- Approach A would introduce a NEW abstraction surface specifically for SQLite (one transport) and require new factory plumbing.
- Approach B extends the existing `IMessageContext.Set/Get` mechanism that SqlServer already uses (`context.Set(_sqlHeaders.Connection, connection)` at `SQLServerMessageQueueReceive.cs:165`).
- Smaller file-edit surface. Lower risk of mid-build architectural surprise.
- PLAN-2.1's notification class can read directly from `IMessageContext` — minimal coupling.

The new `IConnectionHeader` typed-key for SQLite carries `(IDbConnection, IDbTransaction)` as a tuple-like state object stored on context.

## Dependencies
None — foundational.

## Tasks

### Task 1: Add hold-tx connection-and-transaction state on `IMessageContext` + new typed header
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteConnectionState.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteHeaders.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs` (modify — register the new header singleton)

**Action:** create + modify

**Description:**

Create a `SqLiteConnectionState` class that holds the active dequeue connection and transaction for the in-flight message. Internal class. Fields: `IDbConnection Connection { get; }`, `IDbTransaction Transaction { get; }`. Constructor takes both. The state object also tracks whether the tx has been committed/rolled-back (for safety against double-commit/double-rollback).

```csharp
internal sealed class SqLiteConnectionState
{
    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }
    public bool Completed { get; private set; }
    public SqLiteConnectionState(IDbConnection connection, IDbTransaction transaction) { ... }
    public void MarkCompleted() => Completed = true;
}
```

Create `SqLiteHeaders` with a singleton `IMessageContextData<SqLiteConnectionState>` keyed header property (mirrors how `_sqlHeaders.Connection` works on SqlServer — see `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerMessageQueueHeaders.cs` or similar for the pattern; if no helper class exists, just construct `new MessageContextData<SqLiteConnectionState>("SqLite.ConnectionState", null)` inline).

Register the headers as a Singleton in `SqLiteMessageQueueSharedInit.RegisterImplementations` near where other transport headers are registered (analogous to SqlServer's `IConnectionHeader<,,>` registration). Also confirm `using DotNetWorkQueue.Queue;` is present (Phase 4 PG carry-over note — RESEARCH.md §7 noted this gap).

**Acceptance Criteria:**
- `SqLiteConnectionState.cs` exists, internal, sealed, with the two readonly properties + `MarkCompleted` flag.
- `SqLiteHeaders.cs` exposes a public/internal typed `IMessageContextData<SqLiteConnectionState>` (called `Connection` or `State` — match SqlServer naming for the equivalent).
- `SqLiteMessageQueueSharedInit` registers the headers and exposes the connection-state singleton via DI.
- LGPL-2.1 18-line header on both new files (byte-copy from `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueInit.cs:1-18`).
- No `Tx` abbreviation.
- Release build clean (both TFMs).

### Task 2: Modify `ReceiveMessageQueryHandler.Handle` to honor the hold-tx option
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/QueryHandler/ReceiveMessageQueryHandler.cs` (modify)

**Action:** modify

**Description:**

Today (`ReceiveMessageQueryHandler.cs:94-113`) wraps the connection + transaction in `using` blocks, disposing both at the end of `Handle()`. New behavior:
- If `_options.Value.EnableHoldTransactionUntilMessageCommitted == true`: do NOT dispose the connection or transaction at the end of `Handle()`. Hand ownership out via the existing `ReceiveMessageQuery<IDbConnection, IDbTransaction>` mechanism — the caller (`Message/ReceiveMessage.GetMessage`) will get the connection + tx back through the query parameters being populated (mutable refs aren't possible with current shape — see below).
- If option false: existing behavior (create + dispose internally).

**API change required:** the `ReceiveMessageQuery<IDbConnection, IDbTransaction>` is currently called with `(null, null, ...)` (per `Source/DotNetWorkQueue.Transport.SQLite/Basic/Message/ReceiveMessage.cs:73`). The query handler creates its own connection + tx. To support hold-tx, EITHER:
- (a) Change the query handler to ACCEPT a connection + tx from the caller (use the existing nullable params), where the caller (modified `ReceiveMessage.GetMessage` in Task 3) creates them and passes them in. The query handler skips its own creation if both are non-null.
- (b) Add a new query type `ReceiveMessageWithHeldTransactionQuery` and a separate handler for the hold-tx path.

Choose option (a) — minimal new abstraction surface; reuses the existing nullable params for their intended purpose.

Pseudo-code shape after the change:
```csharp
public IReceivedMessageInternal Handle(ReceiveMessageQuery<IDbConnection, IDbTransaction> query)
{
    if (!_databaseExists.Exists(_connectionInformation.ConnectionString)) return null;

    IDbConnection connection;
    IDbTransaction transaction;
    bool weCreatedThem;

    if (query.Connection != null && query.Transaction != null)
    {
        connection = query.Connection;
        transaction = query.Transaction;
        weCreatedThem = false;  // caller owns lifecycle
    }
    else
    {
        connection = _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false);
        connection.Open();
        transaction = _dbFactory.CreateTransaction(connection).BeginTransaction();
        weCreatedThem = true;
    }

    try
    {
        using (var selectCommand = connection.CreateCommand())
        {
            selectCommand.Transaction = transaction;
            // ... existing dequeue logic ...
            return _messageDeQueue.HandleMessage(connection, transaction, reader, commandString);
        }
    }
    finally
    {
        if (weCreatedThem)
        {
            transaction.Dispose();
            connection.Dispose();
        }
    }
}
```

**Acceptance Criteria:**
- `ReceiveMessageQueryHandler.Handle` branches on whether the caller supplied `Connection` and `Transaction` (both non-null) vs not.
- When caller-supplied: query handler does NOT dispose them.
- When self-created (existing behavior): query handler still uses `using`/`finally` for proper disposal.
- Release build clean (both TFMs).
- No `Tx` token in additions.
- No `(SqliteConnection)` / `(SqliteTransaction)` sealed-type casts.

### Task 3: Wire up hold-tx in `SqLiteMessageQueueReceive` + `Message/ReceiveMessage`
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueReceive.cs` (modify)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/Message/ReceiveMessage.cs` (modify)

**Action:** modify

**Description:**

**`ReceiveMessage.GetMessage(IMessageContext context)` (`Message/ReceiveMessage.cs`):**

When `_configuration.Options().EnableHoldTransactionUntilMessageCommitted == true`:
1. Create the connection + tx HERE (lift from query handler):
   ```csharp
   var connection = _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false);
   connection.Open();
   var transaction = _dbFactory.CreateTransaction(connection).BeginTransaction();
   ```
2. Pass them to the query: `new ReceiveMessageQuery<IDbConnection, IDbTransaction>(connection, transaction, ...)`.
3. After `Handle` returns, store the `SqLiteConnectionState(connection, transaction)` on `context.Set(_sqLiteHeaders.ConnectionState, state)`.
4. Wire `context.Commit` / `context.Rollback` / `context.Cleanup` to commit/rollback/dispose the held connection + tx.

When option false: existing behavior (`Handle` self-manages, no state on context).

**Constructor signature update for `ReceiveMessage`:** add dependencies for `IDbFactory`, `IConnectionInformation`, and the SqLite headers singleton. DI will resolve them through the existing `SqLiteMessageQueueSharedInit` registrations.

**`SqLiteMessageQueueReceive.SetActionsOnContext(IMessageContext context)`:** unchanged structure but the `ContextOnCommit`/`ContextOnRollback`/`Context_Cleanup` handlers must now READ the `SqLiteConnectionState` from context (if present) and:
- `ContextOnCommit`: `state.Transaction.Commit()` THEN `state.MarkCompleted()`.
- `ContextOnRollback`: `if (!state.Completed) state.Transaction.Rollback()` THEN `state.MarkCompleted()`.
- `Context_Cleanup`: `state.Transaction.Dispose()`; `state.Connection.Dispose()`.

The existing delegation to `_handleMessage.CommitMessage.Commit(context)` etc. continues to fire FIRST (so existing message-status updates happen); only after that does the tx commit/rollback.

**Acceptance Criteria:**
- `ReceiveMessage.GetMessage` branches on the option and creates the connection + tx in the hold-tx path.
- `SqLiteMessageQueueReceive` ContextOn* handlers commit/rollback/dispose the held connection + tx in the correct order.
- Existing test suite (`Source/DotNetWorkQueue.Transport.SQLite.Tests/`) still green — the option-false path is unchanged.
- Release build clean.
- No `Tx` token in additions.

## Verification

```bash
# Gate 1: Release build clean.
dotnet build "Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -8
# expect 0 errors. NU1902 warnings tolerated.

# Gate 2: existing SQLite tests still pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures. The option-false path is unchanged so all existing tests should pass.

# Gate 3: no new sealed-type casts.
grep -rnE "\(SqliteConnection\)|\(SqliteTransaction\)" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteConnectionState.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteHeaders.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/QueryHandler/ReceiveMessageQueryHandler.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueReceive.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/Message/ReceiveMessage.cs
# expect exit 1, zero matches.

# Gate 4: Tx-token guard.
grep -rnE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteConnectionState.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteHeaders.cs
# expect exit 1, zero matches.

# Gate 5: EnableHoldTransactionUntilMessageCommitted is now read in at least 2 places.
grep -rn "EnableHoldTransactionUntilMessageCommitted" Source/DotNetWorkQueue.Transport.SQLite --include="*.cs"
# expect: SQLiteMessageQueueTransportOptions.cs:278 (declaration) PLUS at least 2 reads (in ReceiveMessage.cs and somewhere on the commit/rollback path).
```

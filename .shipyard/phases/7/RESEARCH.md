# Research: Phase 7 — Integration Tests

## §1 Integration test project layout (confirmed)

| Transport | Project name | Path |
|---|---|---|
| SqlServer | `DotNetWorkQueue.Transport.SqlServer.IntegrationTests` (no dot) | `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/` |
| PostgreSQL | `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests` (with dot) | `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/` |
| SQLite | `DotNetWorkQueue.Transport.SQLite.Integration.Tests` (with dot) | `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/` |

Naming inconsistency between SqlServer (no dot) and PG/SQLite (with dot) is pre-existing; do NOT normalize.

Each project has:
- `ConnectionString.cs` — connection-string loader, reads from `connectionstring.txt` at test runtime.
- `GenerateQueueName.cs` — helper for queue name generation.
- `AssemblyInit.cs` — assembly-level test fixture.
- `Outbox/` subdirectory (SqlServer + PG) — outbox-milestone test template.

## §2 Outbox-milestone test template (reference for Phase 7 patterns)

**Test base class pattern** (`SqlServerOutboxIntegrationTestBase.cs`):
- `protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");` (CLAUDE.md DNQ-hyphen lesson)
- `protected static string NewBusinessTableName() => "OutboxBusiness_" + Guid.NewGuid().ToString("N");`
- `protected sealed class QueueScope : IDisposable` — owns `QueueCreationContainer<T>` + `*MessageQueueCreation`; disposes both in reverse order, swallows teardown errors.
- `protected QueueScope CreateQueue(QueueConnection queueConnection, bool enablePriority = false)` — instantiates the queue scope.
- Business-table helpers: `CreateBusinessTable(IDbConnection conn, string tableName)` + assertion helper `AssertBothRowsVisible(...)` / `AssertNeitherRowVisible(...)`.

**Test method pattern** (`SqlServerOutboxSendTests.cs`):
```csharp
[TestMethod]
public void Send_Commit_BothRowsVisible()
{
    var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
    var businessTable = NewBusinessTableName();

    using var queue = CreateQueue(qc);
    using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
    conn.Open();
    try
    {
        CreateBusinessTable(conn, businessTable);
        using var producer = CreateRelationalProducer(qc);
        using (var transaction = conn.BeginTransaction())
        {
            // produce + business INSERT inside the same transaction
            var msg = GenerateMessage.Create<FakeMessage>();
            var result = producer.RelationalProducer.Send(msg, transaction);
            // ... business INSERT on transaction.Connection with transaction ...
            transaction.Commit();
        }
        // verify from separate connection: BOTH rows visible
    }
    finally
    {
        // drop business table
    }
}
```

## §3 Per-transport hold-tx + ActivityListener integration shape

For Phase 7 Inbox tests, the test base class needs:
- `IConsumerQueue` resolution from `QueueContainer<...>` configured with `EnableHoldTransactionUntilMessageCommitted = true`.
- ActivityListener registration in `[ClassInitialize]`:
  ```csharp
  private static ActivityListener _listener;
  [ClassInitialize] public static void Init(TestContext _) {
      _listener = new ActivityListener {
          ShouldListenTo = source => source.Name.StartsWith("DotNetWorkQueue", StringComparison.Ordinal),
          Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
      };
      ActivitySource.AddActivityListener(_listener);
  }
  [ClassCleanup] public static void Cleanup() { _listener?.Dispose(); }
  ```
- Per-test helper `RunHandler(...)` that:
  1. Configures consumer with `EnableHoldTransactionUntilMessageCommitted = true`.
  2. Produces a single message.
  3. Consumer handler casts `notification is IRelationalWorkerNotification r`, writes to business table on `r.Transaction.Connection`, then returns (commit) OR throws (rollback).
  4. Polls metrics until message processed (CLAUDE.md polling lesson).
  5. Returns control for assertion.

## §4 Option-false negative path

Per ROADMAP: "with `EnableHoldTransactionUntilMessageCommitted = false`, the cast fails and handler code paths that depend on it must surface a discoverable error (not a NullReferenceException)."

Test shape: Configure consumer with option=false. Handler attempts `notification is IRelationalWorkerNotification r` — gets `false`. Handler then SHOULD throw a discoverable error (e.g., `InvalidOperationException` with a clear message), NOT dereference `r.Transaction` causing NRE.

This is actually testing USER CODE behavior (the test's handler code), so the test pattern is: handler with explicit guard like `if (!(notification is IRelationalWorkerNotification r)) throw new InvalidOperationException("Inbox capability requires EnableHoldTransactionUntilMessageCommitted = true");` — and the test asserts the queue records the handler exception and the queue message rolls back appropriately.

## §5 SQLite outbox cross-database validation (spike §3 semantics)

The cross-database test (Validation test 1) needs TWO distinct SQLite databases:
1. Queue database (e.g., `queue.db`).
2. Caller transaction's database (e.g., `business.db`, different file).

`SqLiteExternalDbNameExtractor` returns the canonicalized path of the caller's `conn.DataSource`; `SqliteNormalizedConnectionInformation.Container` returns the queue's canonicalized path. The validator's `StringComparison.Ordinal` compare on the two upper-cased paths must FAIL (different files), throwing the validator's standard mismatch exception.

For the `:memory:` short-circuit test, use `"Data Source=:memory:"` on both sides — they should match (both return `":memory:"` verbatim from the extractor + wrapper).

## §6 Phase 7 architect handoff summary

1. **4 plan files, 1 wave** (all parallel-safe — different transport-specific directories):
   - PLAN-1.1: SqlServer Inbox (8 tests, 5 files including base)
   - PLAN-1.2: PostgreSQL Inbox (8 tests, 5 files)
   - PLAN-1.3: SQLite Inbox (8 tests, 5 files)
   - PLAN-1.4: SQLite Outbox (12 tests, 6 files)

2. Each plan splits its 5-6 files across **≤3 tasks**:
   - Task 1: Base class + 1 test class (~2-3 tests)
   - Task 2: 2 test classes (~4 tests)
   - Task 3: 1-2 test classes + verification gates (~2-4 tests)

3. Build is deferred — needs live SQL Server, PostgreSQL, SQLite for connection-string.txt files.

4. Each plan's verification gates: build clean + the new test suite runs against the corresponding live DB + Coverlet coverage shows new `HandleExternalTx` / receive-path branches hit.

5. **Phase 1 spike Risk #4 (SQLite single-writer)** surfaces here — observations go into Phase 8 docs, NOT blockers.

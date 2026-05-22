# Transactional Outbox Pattern

## Overview

The transactional outbox pattern guarantees that a queue message and a business database write
either both commit or both roll back. It solves the dual-write problem: your application needs to
write a row to its own tables and enqueue a message in one logical operation, and you cannot
afford to have one succeed while the other fails.

DotNetWorkQueue's default producer opens and manages its own connection and transaction. That is
the right default for fire-and-forget enqueue, but it precludes the outbox pattern because the
queue INSERT is not part of the caller's business transaction. `IRelationalProducerQueue<T>` is
the opt-in surface that fixes this: a derived interface that accepts a caller-supplied
`DbTransaction`, runs all queue INSERTs inside it, and never commits, rolls back, or disposes the
caller's resources.

## Quick Start

### Prerequisites

- Your queue transport must be **SqlServer, PostgreSQL, or SQLite** (SQLite with caveats; see
  [SQLite Concurrency Caveat](#sqlite-concurrency-caveat)). Memory, Redis, and LiteDb do not
  implement `IRelationalProducerQueue<T>`. See [Supported Transports](#supported-transports).
- Queue tables must exist before the first `Send` call. Run `CreateQueue()` once at deployment
  time. See [Schema Deployment Prerequisite](#schema-deployment-prerequisite).

### Example: SqlServer

The example below shows the full flow: set up the queue, open a connection and transaction,
write a business row, enqueue the event, then commit atomically.

`IRelationalProducerQueue<T>` has `SendAsync` overloads with matching signatures. The sync form
is shown here.

```csharp
using System.Data;
using Microsoft.Data.SqlClient;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer;

// --- Message type ---
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public string Status { get; set; }
}

// --- One-time setup (at application startup) ---
// Sample connection string, not for production use. Load credentials from a
// secrets manager and use a least-privilege account, not `sa`.
// `TrustServerCertificate=true` is for local dev against a self-signed cert
// only; remove it (or pin a trusted CA) anywhere else.
var conn = new QueueConnection(
    "MyEvents",
    "Server=localhost;Database=AppDb;User Id=sa;Password=...;TrustServerCertificate=true");

using var producerContainer = new QueueContainer<SqlServerMessageQueueInit>();
using var producer = producerContainer.CreateProducer<OrderCreatedEvent>(conn);

// Capability-cast: succeeds for SqlServer and PostgreSQL transports.
if (producer is not IRelationalProducerQueue<OrderCreatedEvent> relationalProducer)
    throw new InvalidOperationException("Transport does not support the outbox pattern.");

// --- Per-request business logic ---
using var sqlConn = new SqlConnection(
    "Server=localhost;Database=AppDb;User Id=sa;Password=...;TrustServerCertificate=true");
sqlConn.Open();

using var transaction = sqlConn.BeginTransaction();

using (var cmd = sqlConn.CreateCommand())
{
    cmd.Transaction = transaction;
    cmd.CommandText =
        "INSERT INTO Orders (OrderId, Status) VALUES (@id, @status)";
    cmd.Parameters.AddWithValue("@id", 42);
    cmd.Parameters.AddWithValue("@status", "Pending");
    cmd.ExecuteNonQuery();
}

// Outbox write: enqueue the event inside the same transaction.
var result = relationalProducer.Send(
    new OrderCreatedEvent { OrderId = 42, Status = "Pending" },
    transaction);

if (result.HasError)
    throw new InvalidOperationException($"Enqueue failed: {result.SendingException}");

transaction.Commit();
```

To retry the whole operation on a transient failure, wrap the block from `sqlConn.Open()` to
`transaction.Commit()` in your own retry policy (e.g., Polly). The producer does not retry on the
caller-transaction path. See [Retry Contract](#retry-contract).

#### PostgreSQL note

The same pattern works with `NpgsqlConnection`, `NpgsqlTransaction`, and
`PostgreSqlMessageQueueInit` as the transport initializer. The capability cast succeeds
identically. The only behavioral difference is in how the DB-name validator compares the
connection's reported database against the queue's configured catalog. That difference is covered
in [Database-Name Comparison Semantics](#database-name-comparison-semantics).

#### SQLite note

The same pattern works with `SqliteConnection`, `SqliteTransaction`, and
`SqLiteMessageQueueInit` as the transport initializer. The capability cast succeeds identically.
SQLite serializes all writers at the database-file level (`BEGIN EXCLUSIVE`-on-write semantics),
so concurrent producer transactions against the same queue file will contend. See
[SQLite Concurrency Caveat](#sqlite-concurrency-caveat) for caller patterns. Retry semantics
match SqlServer/PostgreSQL per [Retry Contract](#retry-contract).

### Async and batch variants

The async form replaces every blocking call with its `Async` counterpart:

```csharp
await using var sqlConn = new SqlConnection(connectionString);
await sqlConn.OpenAsync();

await using var transaction = await sqlConn.BeginTransactionAsync();

using (var cmd = sqlConn.CreateCommand())
{
    cmd.Transaction = (SqlTransaction)transaction;
    cmd.CommandText = "INSERT INTO Orders (OrderId, Status) VALUES (@id, @status)";
    cmd.Parameters.AddWithValue("@id", 42);
    cmd.Parameters.AddWithValue("@status", "Pending");
    await cmd.ExecuteNonQueryAsync();
}

var result = await relationalProducer.SendAsync(
    new OrderCreatedEvent { OrderId = 42, Status = "Pending" },
    transaction);

if (result.HasError)
    throw new InvalidOperationException($"Enqueue failed: {result.SendingException}");

await transaction.CommitAsync();
```

Two notes on the async form:

- `await using` on the transaction calls `DisposeAsync()` when the scope exits. If the
  transaction has not been committed by then, the runtime rolls it back — same semantics as
  the synchronous form, just async-friendly.
- `BeginTransactionAsync()` returns the abstract `DbTransaction`. SqlServer-specific code that
  needs `SqlTransaction` (like the `cmd.Transaction = ...` line above) requires the cast.
  `relationalProducer.SendAsync(...)` accepts the abstract `DbTransaction` directly — no cast needed.

To enqueue multiple messages atomically inside one transaction, the batch overloads accept an
`IEnumerable<T>`:

```csharp
var batch = new[]
{
    new OrderCreatedEvent { OrderId = 42, Status = "Pending" },
    new OrderCreatedEvent { OrderId = 43, Status = "Pending" }
};

var results = relationalProducer.Send(batch, transaction);

if (results.HasErrors)
    throw new InvalidOperationException("One or more batch enqueues failed.");

transaction.Commit();
```

The batch path runs each enqueue inside the supplied transaction. A rollback after a partially
successful batch rolls back every queue row written so far, including the successful ones — the
all-or-nothing guarantee holds for the whole batch, not per message. `SendAsync(batch, transaction)`
is the async equivalent.

## Reference

### Lifecycle Contract

The caller owns the connection and transaction. The producer does not manage them:

- The producer **never** calls `transaction.Commit()`, `transaction.Rollback()`,
  `transaction.Dispose()`, `conn.Close()`, or `conn.Dispose()`.
- All queue INSERTs (message body, metadata, status) run on the supplied `transaction` and its
  `transaction.Connection`. The producer opens no other connection.
- The internal `IConnectionHolder` machinery used on the standard send path is bypassed on the
  caller-transaction path.
- After `Send` returns, the transaction is still open. Commit or roll back when you are ready.
- A rollback after a successful `Send` rolls back the queue row too, so no message is delivered.
  That is the guarantee.
- ADO.NET transactions are not thread-safe. Do not call `Send(msg, transaction)` concurrently with
  writes on the same transaction from another thread. Same rule as the rest of ADO.NET.

See PROJECT.md §Ownership & Threading Contract for the authoritative contract language.

### Retry Contract

The producer's internal Polly retry decorator chain is **bypassed** on the caller-transaction
path. The transport resolves the bare command handler directly and sets
`IRetrySkippable.SkipRetry = true` on the send command so no retry wrapper re-executes the INSERT.

This is intentional: the caller owns the transaction, so the caller must also own retry semantics.
A retry inside the transport would re-execute the INSERT while the connection may be in an
inconsistent state after a transient failure mid-transaction.

To retry the whole business operation, wrap both your business writes and the `Send` call in your
own retry policy and re-call `Send` from scratch on each attempt:

```text
Polly retry policy
  └─ open connection + begin transaction
  └─ business INSERT
  └─ relationalProducer.Send(msg, transaction)
  └─ transaction.Commit()
```

See PROJECT.md §Functional Implementation and `IRetrySkippable.cs` for implementation details.

### Schema Deployment Prerequisite

Queue tables must exist in your database before the first `Send` on the caller-transaction path.
The outbox path does not auto-create tables. Running DDL inside the caller's transaction would
be the wrong contract for this pattern (see PROJECT.md §Non-Goals).

Deploy the schema once at application startup or as part of your database migration:

1. Create a `QueueCreationContainer<SqlServerMessageQueueInit>()`.
2. Call `GetQueueCreation<SqlServerMessageQueueCreation>(conn)` to get the creation handle.
3. Check `DoesQueueExist` and, if false, call `CreateQueue()`. Check the returned result for
   `Success`.

Run this once per deployment, not on every request. `DoesQueueExist` guards against re-creating
an existing queue. For PostgreSQL, substitute `PostgreSqlMessageQueueInit` and
`PostgreSqlMessageQueueCreation`.

### Database-Name Comparison Semantics

At the start of every caller-transaction `Send`, the validator checks that the connection's
reported database matches the queue's configured catalog. Each transport has its own extractor
that retrieves the name from the open connection:

| Transport  | Extractor                           | Comparison     |
|------------|-------------------------------------|----------------|
| SqlServer  | `SqlServerExternalDbNameExtractor`  | `Ordinal`      |
| PostgreSQL | `PostgreSqlExternalDbNameExtractor` | `Ordinal`      |
| SQLite     | `SqLiteExternalDbNameExtractor`     | `Ordinal`      |

Both extractors return `connection.Database` verbatim, with no `ToUpperInvariant` or other
normalization. The comparison is `StringComparison.Ordinal` (byte-for-byte). Configure the
`Database=` key in your connection string with the exact case as the catalog name on the server.

For SqlServer, `SqlConnection.Database` after open returns the canonical name from
`sys.databases`, so the case you used when creating the database is what the extractor sees.
For PostgreSQL, `NpgsqlConnection.Database` reflects the value from the connection string
(PostgreSQL identifiers are case-sensitive unless unquoted).

For SQLite, `SqLiteExternalDbNameExtractor` returns `Path.GetFileNameWithoutExtension(DataSource)`
— the bare file stem with no directory and no extension. `SqLiteExternalTransactionValidator`
applies the same `Path.GetFileNameWithoutExtension` to `IConnectionInformation.Container` before
the `Ordinal` compare, so both sides are normalized symmetrically. Configure the `Container` key
in your connection string with the file stem (e.g., `myqueue`) or the full path including the
`.db3` extension (e.g., `/data/myqueue.db3`) — the extractor strips both forms to the same stem.
For in-memory databases, `Container` should be `:memory:` and `GetFileNameWithoutExtension`
returns `:memory:` unchanged.

If the names do not match, `Send` throws `InvalidOperationException` before writing any data.
The exception message includes both the connection's reported name and the queue's expected name.

### SQLite Concurrency Caveat

SQLite serializes all writers at the database-file level via `BEGIN EXCLUSIVE` semantics. When
a caller-supplied transaction is open and `Send` is executing inside it, an exclusive write lock
is held on the queue's database file for the duration of that transaction. No other writer
— including any concurrent producer thread or consumer worker thread writing to the same queue
tables — can acquire a write lock until the caller commits or rolls back.

This is not a code defect; it is SQLite's single-writer architecture. The outbox pattern still
works correctly — atomicity is preserved — but callers must design around the serialization:

- **Serialize producers.** Avoid opening concurrent outbox transactions across threads or
  processes against the same SQLite database file. Concurrent transactions will contend on the
  write lock, producing `SQLITE_BUSY` errors on the losing side.
- **Keep `Send` short.** Open the caller's transaction immediately before the business write and
  the `Send` call, then commit as soon as both succeed. A long-running transaction prolongs the
  exclusive lock window.
- **Set `busy_timeout`.** Configure a pragmatic wait before SQLite returns `SQLITE_BUSY`. In
  `IntegrationConnectionInfo`, execute `PRAGMA busy_timeout = 5000;` (5 seconds) on the
  connection before opening the transaction. This converts most transient contention into a wait
  rather than an immediate failure.
- **Low-concurrency designs are the right fit.** SQLite's outbox support is best suited for
  single-process producers or strictly sequential enqueue pipelines. High-throughput,
  multi-thread producer workloads are better served by SqlServer or PostgreSQL.

**In-memory vs on-disk.** Both modes support the outbox pattern. In-memory shared-cache
(`?mode=memory&cache=shared`) follows the same serialization rules. On-disk databases benefit
from WAL mode (configure via `PRAGMA journal_mode=WAL;` on the connection before use), which
improves read concurrency but does not change the single-writer constraint.

**Inbox pattern.** `IRelationalWorkerNotification` (the inbox / hold-transaction-until-committed
pattern) is permanently out of scope for SQLite. The same `BEGIN EXCLUSIVE` semantics that
require care on the producer side make the inbox pattern structurally non-viable: holding the
dequeue transaction for the duration of a user handler blocks the consumer worker's own next
dequeue attempt, producing a deadlock. See [issue #149](https://github.com/blehnen/DotNetWorkQueue/issues/149).

### Supported Transports

- **Supported:** SqlServer, PostgreSQL. The `IProducerQueue<T>` instance returned by these
  transports implements `IRelationalProducerQueue<T>` and the `is` capability cast succeeds.

- **Supported with caveats:** SQLite. The capability cast succeeds. SQLite serializes all writers
  at the database-file level; see [SQLite Concurrency Caveat](#sqlite-concurrency-caveat) before
  using this transport in a concurrent producer scenario.

- **Not supported:** Memory, Redis, LiteDb. The producer instances returned by these transports
  do not implement `IRelationalProducerQueue<T>`. The `is` cast returns false; no
  `NotSupportedException` is thrown. The interface is simply absent, so a misconfigured caller
  fails at the cast rather than at the first `Send`.

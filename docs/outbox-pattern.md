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

- Your queue transport must be **SqlServer or PostgreSQL**. Memory, Redis, LiteDb, and SQLite do
  not implement `IRelationalProducerQueue<T>`. See [Supported Transports](#supported-transports).
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

try
{
    relationalProducer.Send(batch, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

The batch path performs a true multi-row **body** insert inside the supplied transaction for each
chunk; the per-message metadata (and status) rows are still written one per message afterward, all
on the same transaction. Because the caller owns the transaction, the path is fail-fast: on any
failure it **throws** so you can roll back — it does not report per-message errors. (Catch the
exception and call `transaction.Rollback()`, as above.) The all-or-nothing guarantee holds for the
whole batch. `SendAsync(batch, transaction)` is the async equivalent.

> The caller-supplied-transaction batch overload is implemented for **SQL Server** and
> **PostgreSQL**. Other transports throw `InvalidOperationException` (SQLite is single-writer, so
> holding a transaction open across a batch is non-viable — use the standalone `Send(List<T>)`
> outbox path there instead).
>
> Note: this throw-on-failure behavior is specific to the caller-transaction batch path
> (since 0.9.42). The standalone `Send(List<T>)` path still reports per-message results via
> `IQueueOutputMessages.HasErrors`.

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

Both extractors return `connection.Database` verbatim, with no `ToUpperInvariant` or other
normalization. The comparison is `StringComparison.Ordinal` (byte-for-byte). Configure the
`Database=` key in your connection string with the exact case as the catalog name on the server.

For SqlServer, `SqlConnection.Database` after open returns the canonical name from
`sys.databases`, so the case you used when creating the database is what the extractor sees.
For PostgreSQL, `NpgsqlConnection.Database` reflects the value from the connection string
(PostgreSQL identifiers are case-sensitive unless unquoted).

If the names do not match, `Send` throws `InvalidOperationException` before writing any data.
The exception message includes both the connection's reported name and the queue's expected name.

### Supported Transports

- **Supported:** SqlServer, PostgreSQL. The `IProducerQueue<T>` instance returned by these
  transports implements `IRelationalProducerQueue<T>` and the `is` capability cast succeeds.

- **Not supported:** Memory, Redis, LiteDb, SQLite. The producer instances returned by these
  transports do not implement `IRelationalProducerQueue<T>`. The `is` cast returns false; no
  `NotSupportedException` is thrown. The interface is simply absent, so a misconfigured caller
  fails at the cast rather than at the first `Send`.

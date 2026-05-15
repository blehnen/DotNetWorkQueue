# Transactional Outbox Pattern

## Overview

The transactional outbox pattern guarantees that a queue message and a business database write
either both commit or both roll back. It solves the dual-write problem that arises when your
application must write to a database table and enqueue a message as part of a single logical
operation.

DotNetWorkQueue's default producer opens and manages its own database connection and transaction
internally. That design is correct for fire-and-forget enqueue, but it precludes the outbox
pattern because the queue INSERT is not part of your business transaction. The opt-in surface for
the outbox pattern is `IRelationalProducerQueue<T>`, a derived interface that accepts a
caller-supplied `DbTransaction` and performs all queue INSERTs inside it without ever committing,
rolling back, or disposing the caller's resources.

## Quick Start

### Prerequisites

- Your queue transport must be **SqlServer or PostgreSQL**. Memory, Redis, LiteDb, and SQLite do
  not implement `IRelationalProducerQueue<T>` — see [Supported Transports](#supported-transports).
- Queue tables must exist before the first `Send` call. Run `CreateQueue()` once at deployment
  time — see [Schema Deployment Prerequisite](#schema-deployment-prerequisite).

### Example: SqlServer

The example below shows a complete vertical slice: set up the queue, open a connection and
transaction, perform a business INSERT, enqueue the event, and commit — atomically.

`SendAsync` overloads with the same signatures exist on `IRelationalProducerQueue<T>` for
async callers; the sync form is shown here for clarity.

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

// Business write: INSERT your domain row.
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

// Commit: both the business row and the queue row commit atomically.
transaction.Commit();
```

To retry the whole operation on a transient failure, wrap the block from `sqlConn.Open()` to
`transaction.Commit()` in your own retry policy (e.g., Polly). The producer does not retry on the
caller-transaction path — see [Retry Contract](#retry-contract).

#### PostgreSQL note

The same pattern works with `NpgsqlConnection`, `NpgsqlTransaction`, and
`PostgreSqlMessageQueueInit` as the transport initializer. The capability cast succeeds
identically. The only behavioral difference is how the DB-name validator compares the connection's
reported database name against the queue's configured catalog — covered in
[Database-Name Comparison Semantics](#database-name-comparison-semantics).

## Reference

### Lifecycle Contract

The caller owns the connection and transaction for their entire lifetime. The producer participates
as a guest:

- The producer **never** calls `transaction.Commit()`, `transaction.Rollback()`, `transaction.Dispose()`, `conn.Close()`,
  or `conn.Dispose()`.
- The producer performs its queue INSERTs (message body, metadata, status) using the connection
  and transaction you supply via `transaction.Connection` and the `transaction` reference directly. It uses no other
  connection.
- `IConnectionHolder` and `IConnectionHolderFactory` — the internal machinery that manages
  owned connections on the normal send path — are bypassed entirely on the caller-transaction path.
- After `Send` returns, the transaction is still open. You decide whether to commit or roll back.
- If you roll back after a successful `Send`, the queue row is rolled back too. No message is
  delivered. This is the guarantee the pattern provides.
- ADO.NET transactions are not thread-safe. Do not call `Send(msg, transaction)` concurrently with your
  own writes on the same transaction from another thread. This matches the standard ADO.NET
  threading contract.

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
The outbox path does not auto-create tables — DDL inside the caller's transaction is the wrong
contract for this pattern (see PROJECT.md §Non-Goals).

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

Both extractors return `connection.Database` verbatim — no `ToUpperInvariant`, no normalization.
The comparison is always `StringComparison.Ordinal` (byte-for-byte). Configure the `Database=`
key in your connection string with the exact case as the catalog name in your server.

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
  transports do not implement `IRelationalProducerQueue<T>`. The `is` cast returns false and
  no `NotSupportedException` is thrown — the interface is simply absent, so misconfigured
  callers fail at the cast rather than at the first `Send`. Phase 5 added negative-path
  integration tests that confirm the cast fails for all four transports.

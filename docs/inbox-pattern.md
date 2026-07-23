# Transactional Inbox Pattern

## Overview

The transactional inbox pattern guarantees that a message dequeue and the user handler's business
database write either both commit or both roll back. It solves the consumer-side dual-write
problem: your handler needs to write to its own tables (idempotency keys, business state, etc.)
and acknowledge the queue message in one logical operation, and you cannot afford to have one
succeed while the other fails.

DotNetWorkQueue's default consumer opens an internal transaction for the dequeue, runs your
handler, then commits or rolls back on its own. That is the right default for at-least-once
delivery, but it precludes the inbox pattern because the handler has no way to enlist its own
writes on the dequeue transaction. `IRelationalWorkerNotification` is the opt-in surface that
fixes this: a derived interface that exposes the active `DbTransaction` to the handler. The
library still owns the transaction's lifecycle: the handler enlists writes on it, the library
commits on successful return or rolls back on throw.

This is the **dual** of the outbox pattern. The outbox (producer-side) lets the caller own the
transaction and enlist the queue INSERT. The inbox (consumer-side) lets the library own the
transaction and the handler enlist business writes. See [`outbox-pattern.md`](outbox-pattern.md)
for the producer-side counterpart.

## Quick Start

### Prerequisites

- Your queue transport must be **SqlServer or PostgreSQL**. Memory, Redis, LiteDb, and SQLite
  do not implement `IRelationalWorkerNotification`. See [Supported Transports](#supported-transports).
- The queue must be created with `EnableHoldTransactionUntilMessageCommitted = true`. With the
  option off, the capability cast cleanly fails by design.
- Queue tables must exist before the consumer starts. Run `CreateQueue()` once at deployment
  time. See [Schema Deployment Prerequisite](#schema-deployment-prerequisite).

### Example: SqlServer

The example below shows the full flow: create the queue with the hold-transaction option,
start a consumer, and in the handler enlist a business INSERT on the library-owned transaction.

`IConsumerQueueAsync` works the same way with `CreateConsumerAsync` and an async handler.

```csharp
using System;
using System.Data;
using System.Threading;
using Microsoft.Data.SqlClient;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer;
using DotNetWorkQueue.Transport.SqlServer.Basic;

// --- Message type ---
public class OrderProcessedEvent
{
    public int OrderId { get; set; }
    public string Status { get; set; }
}

// --- One-time setup (at deployment / startup) ---
// Sample connection string, not for production use. Load credentials from a
// secrets manager and use a least-privilege account, not `sa`.
// `TrustServerCertificate=true` is for local dev against a self-signed cert
// only; remove it (or pin a trusted CA) anywhere else.
var conn = new QueueConnection(
    "MyEvents",
    "Server=localhost;Database=AppDb;User Id=sa;Password=...;TrustServerCertificate=true");

using (var creator = new QueueCreationContainer<SqlServerMessageQueueInit>())
using (var creation = creator.GetQueueCreation<SqlServerMessageQueueCreation>(conn))
{
    creation.Options.EnableHoldTransactionUntilMessageCommitted = true;
    // EnableStatus must be false when EnableHoldTransactionUntilMessageCommitted is true.
    creation.Options.EnableStatus = false;
    creation.Options.EnableStatusTable = false;
    if (!creation.QueueExists)
    {
        var result = creation.CreateQueue();
        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage);
    }
}

// --- Long-running consumer ---
using var consumerContainer = new QueueContainer<SqlServerMessageQueueInit>();
using var consumer = consumerContainer.CreateConsumer(conn);

consumer.Configuration.Worker.WorkerCount = 4;

consumer.Start<OrderProcessedEvent>((message, workerNotification) =>
{
    // Capability cast: succeeds when EnableHoldTransactionUntilMessageCommitted = true
    // on SqlServer or PostgreSQL. Fails (cleanly, no throw) otherwise.
    if (workerNotification is not IRelationalWorkerNotification relational)
        throw new InvalidOperationException(
            "Transport does not support the inbox pattern, or option is disabled.");

    // Enlist the business INSERT on the library-owned transaction. Do NOT call
    // Commit / Rollback / Dispose on relational.Transaction or its Connection.
    using var cmd = relational.Transaction.Connection.CreateCommand();
    cmd.Transaction = relational.Transaction;
    cmd.CommandText =
        "INSERT INTO ProcessedOrders (OrderId, Status, ProcessedAt) " +
        "VALUES (@id, @status, @ts)";
    cmd.Parameters.Add(new SqlParameter("@id", message.Body.OrderId));
    cmd.Parameters.Add(new SqlParameter("@status", message.Body.Status));
    cmd.Parameters.Add(new SqlParameter("@ts", DateTime.UtcNow));
    cmd.ExecuteNonQuery();

    // Returning normally signals success; the library commits both the dequeue and
    // the business INSERT atomically. Throw to signal failure; the library rolls
    // back both atomically.
}, null);

// Keep the process alive while the consumer is running.
Thread.Sleep(Timeout.Infinite);
```

If the handler throws for any reason (your assertion, a SQL exception, an unrelated bug), the
library rolls back the entire transaction. The queue row stays available for redelivery and your
business write never lands. If the handler returns normally, the library commits both. There is
no third state.

### Async handler

The async form replaces `CreateConsumer` with `CreateConsumerAsync` and the handler signature
returns `Task`:

```csharp
using var consumer = consumerContainer.CreateConsumerAsync(conn);

consumer.Start<OrderProcessedEvent>(async (message, workerNotification) =>
{
    if (workerNotification is not IRelationalWorkerNotification relational)
        throw new InvalidOperationException(
            "Transport does not support the inbox pattern, or option is disabled.");

    using var cmd = relational.Transaction.Connection.CreateCommand();
    cmd.Transaction = relational.Transaction;
    cmd.CommandText =
        "INSERT INTO ProcessedOrders (OrderId, Status, ProcessedAt) " +
        "VALUES (@id, @status, @ts)";
    cmd.Parameters.Add(new SqlParameter("@id", message.Body.OrderId));
    cmd.Parameters.Add(new SqlParameter("@status", message.Body.Status));
    cmd.Parameters.Add(new SqlParameter("@ts", DateTime.UtcNow));
    await cmd.ExecuteNonQueryAsync();
}, null);
```

`relational.Transaction` is the abstract `DbTransaction` so `await`-friendly APIs on the base
class work directly. Do not cast to `SqlTransaction` and call `CommitAsync` /
`RollbackAsync` / `DisposeAsync`; the library owns the lifecycle.

### PostgreSQL note

The same pattern works with `PostgreSqlMessageQueueInit` / `PostgreSqlMessageQueueCreation` and
`NpgsqlConnection` / `NpgsqlTransaction`. The capability cast succeeds identically and the
ownership contract is the same.

## Reference

### Lifecycle Contract

The library owns the connection and transaction exposed via `IRelationalWorkerNotification`:

- The handler **never** calls `transaction.Commit()`, `transaction.Rollback()`,
  `transaction.Dispose()`, `connection.Close()`, or `connection.Dispose()`.
- The handler MAY enlist `DbCommand`s on `relational.Transaction.Connection` with
  `cmd.Transaction = relational.Transaction`. All writes inside the same transaction commit
  atomically with the dequeue.
- Returning normally from the handler signals success: the library commits the dequeue and any
  business writes the handler made on the transaction.
- Throwing signals failure: the library rolls back the dequeue and any business writes.
- The handler MUST NOT stash the `DbTransaction` or `DbConnection` references past return; the
  library disposes them in the cleanup phase, after which the references are invalid.
- The handler MUST NOT pass `DbTransaction` to another thread. `DbTransaction` is not
  thread-safe, the same rule as the rest of ADO.NET.

### Commit / Rollback Semantics

The library commits or rolls back on these two events:

1. **Handler returns normally** → library calls `transaction.Commit()`, then disposes the
   connection. The dequeue row is removed and your business writes are durable.
2. **Handler throws** → library calls `transaction.Rollback()`, then disposes the connection. The
   dequeue row remains available for redelivery; your business writes never land.

If a transient SQL error occurs mid-handler (deadlock, connection drop), throwing the SQL
exception is the right response: the library rolls back, the message redelivers, and your retry
policy decides whether to drop the message into the error queue. Do not catch and swallow:
catching without rethrowing tells the library the handler succeeded, which commits whatever the
transaction is in at that moment.

### EnableHoldTransactionUntilMessageCommitted Requirement

The `IRelationalWorkerNotification` capability is only registered when
`EnableHoldTransactionUntilMessageCommitted = true` on the queue's transport options at
**queue-creation time**. The option is persisted in the queue's configuration table, so the
consumer reads the same value the creator wrote.

With the option off, the consumer's resolved `IWorkerNotification` is the plain
`WorkerNotification`. The `is IRelationalWorkerNotification` cast returns false, and the
inbox capability is not available. This is the discoverable signal: a single cast on the
handler side tells you whether the transaction is exposed.

Set the option once at queue creation, alongside the matching `EnableStatus = false` and
`EnableStatusTable = false` (the queue's status-tracking machinery is incompatible with
hold-transaction mode and queue validation will reject the combination). The option cannot be
changed after the queue exists without recreating it.

### When the cast fails

| Condition                                                            | Cast result | What it means                                       |
|----------------------------------------------------------------------|-------------|-----------------------------------------------------|
| SqlServer or PG, `EnableHoldTransactionUntilMessageCommitted = true` | `true`      | Use `relational.Transaction` for business writes    |
| SqlServer or PG, `EnableHoldTransactionUntilMessageCommitted = false`| `false`     | Inbox capability is disabled; option must be set    |
| SQLite (any option setting)                                          | `false`     | Inbox is structurally non-viable on SQLite          |
| Memory, Redis, LiteDb (any option setting)                           | `false`     | Non-relational transports never implement inbox     |

The cast never throws. A handler that requires inbox semantics should throw an
`InvalidOperationException` on cast failure (as in the Quick Start example) so misconfigurations
fail at handler-invocation time rather than producing silent at-least-once-only semantics.

### Schema Deployment Prerequisite

Queue tables must exist before the consumer starts. The consumer does not auto-create tables.
Deploy the schema once at application startup or as part of your database migration:

1. Create a `QueueCreationContainer<SqlServerMessageQueueInit>()`.
2. Call `GetQueueCreation<SqlServerMessageQueueCreation>(conn)` to get the creation handle.
3. Set `creation.Options.EnableHoldTransactionUntilMessageCommitted = true`.
4. Set `creation.Options.EnableStatus = false` and `creation.Options.EnableStatusTable = false`
   (queue validation rejects the combination otherwise).
5. Check `creation.QueueExists` and, if false, call `CreateQueue()`. Check `Success` on the
   returned result.

Run this once per deployment, not on every consumer start. `QueueExists` guards against
re-creating an existing queue. For PostgreSQL, substitute `PostgreSqlMessageQueueInit` and
`PostgreSqlMessageQueueCreation`.

### Supported Transports

- **Supported:** SqlServer, PostgreSQL. The `IWorkerNotification` passed to the handler
  implements `IRelationalWorkerNotification` when
  `EnableHoldTransactionUntilMessageCommitted = true`.

- **Not supported:** Memory, Redis, LiteDb. These transports do not have a relational
  `DbTransaction` to expose; the capability interface is never implemented.

- **Not supported, SQLite.** SQLite uses single-writer / `BEGIN EXCLUSIVE`-on-write
  concurrency. Holding the dequeue transaction for the duration of a user handler would block
  every other writer on the queue table, including the worker's own next dequeue attempt,
  producing a worker deadlock. The `EnableHoldTransactionUntilMessageCommitted` property is
  hidden from `SqLiteMessageQueueTransportOptions` (explicit interface implementation on
  `ITransportOptions` only); code that types the variable as the concrete SQLite options
  class will not see it. Code that holds an `ITransportOptions` reference still satisfies
  the interface but the getter is hard-wired to `false` and the setter is a no-op. See
  issue #149 for the SQLite outbox follow-up (the outbox direction is viable on SQLite
  because the caller chooses when to commit).

### Comparison to the Outbox Pattern

| Aspect                   | Inbox (this doc)                                 | [Outbox](outbox-pattern.md)                    |
|--------------------------|--------------------------------------------------|------------------------------------------------|
| Side                     | Consumer (handler reads queue + writes business) | Producer (caller writes business + enqueues)   |
| Transaction owner        | Library                                          | Caller                                         |
| Capability interface     | `IRelationalWorkerNotification`                  | `IRelationalProducerQueue<T>`                  |
| How transaction is set   | Library begins on dequeue                        | Caller passes to `Send`                        |
| Required option          | `EnableHoldTransactionUntilMessageCommitted=true`| None (always available on relational producers)|
| Supported transports     | SqlServer, PostgreSQL                            | SqlServer, PostgreSQL                          |
| SQLite                   | Permanently non-viable (single-writer lock)      | Tracked for follow-up in #149                  |
| Retry semantics          | Library retry policies apply                     | Caller-owned (library retry is bypassed)       |

Both patterns share `EnableHoldTransactionUntilMessageCommitted` semantics on the producer
side for non-outbox sends; the inbox option enables the consumer's hold-transaction window
specifically. See PROJECT.md §Functional for the authoritative contract language.

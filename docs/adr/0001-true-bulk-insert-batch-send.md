# ADR 0001: True bulk-insert batch send for relational transports

- **Status:** Accepted
- **Date:** 2026-06-26
- **Context issue:** [#162](https://github.com/blehnen/DotNetWorkQueue/issues/162)

## Context

`IProducerQueue.Send(List<...>)` historically provided no throughput benefit over calling
`Send(message)` in a loop. The batch overloads fanned the list out to the single-message
command handler: `SendMessages.Send` ran a `Parallel.ForEach` (sync) or `foreach` (async)
that invoked the per-message handler once per message. On the relational transports each of
those calls opened its own connection and transaction and issued two or three single-row
round trips. A batch of N messages therefore became N independent transactions, N commits,
and N connections. SQLite (single-writer, one fsync per commit) suffered most, but every
relational transport paid for the connection churn and per-message commits.

The Redis transport already had a true batch path (`SendMessageCommandBatch` plus a Lua
multi-enqueue). The goal of #162 is to bring the relational transports up to that model,
behind the unchanged public API, starting with SQL Server.

## Decision

Add an optional batch command handler dispatched from `SendMessages<T>`:

- A transport-independent `SendMessageCommandBatch` command (and a transaction-aware
  `RelationalSendMessageCommandBatch` subclass for the inbox held-transaction path).
- `SendMessages<T>` takes the batch handler as an optional dependency and decides whether to
  use it from a separate `ISendMessageBatchSupport` capability service (a plain `bool`).
  Transports with a bulk insert register it `true` and dispatch the whole list to their batch
  handler; transports without one (Memory, LiteDb, SQLite/PostgreSQL until their own phases)
  register it `false` plus a no-op handler that is never invoked, and fall back to the existing
  per-message loop. The capability is a dedicated service rather than a marker interface on the
  handler because the relational transports wrap every command handler in open-generic
  decorators (Polly retry, trace, metrics), so the injected handler reference is a decorator,
  not the concrete type, and a type or marker check on it silently reports the wrong answer.
- The SQL Server handler runs one connection and one transaction spanning all chunks. Per
  chunk it performs a multi-row body insert via
  `MERGE … OUTPUT INSERTED.QueueID, Source.Ordinal`, recovers the generated ids in caller
  input order via the ordinal, then inserts the dependent meta (and optional status) rows.
- Batches are whole-batch atomic (all-or-nothing): any failure rolls back every row and each
  returned `IQueueOutputMessage` reports the failure. This is a deliberate behavior change
  from the previous per-message isolation, where one bad message failed alone and the rest
  committed. Callers who need per-message isolation continue to call `Send` in a loop.
- Chunking is governed by a safe maximum derived from the SQL Server 2,100-parameter command
  limit. The batch size is user-configurable (`SqlServerMessageQueueTransportOptions.BatchSize`)
  but is clamped down to the safe maximum, so a configured value can never overflow the budget.
- `ProducerQueue.InternalSendPrepare` was changed from a `ConcurrentBag` plus `Parallel.ForEach`
  (which discarded caller order) to an order-preserving `Parallel.For` into a pre-sized array,
  so the batch handler can return ids in input order. This is a strict improvement that
  benefits all transports.

### Why MERGE rather than INSERT … OUTPUT

SQL Server does not guarantee that identity values are assigned in `VALUES` order, so the
generated ids must be re-associated with input positions explicitly. A plain
`INSERT … OUTPUT INSERTED.QueueID` cannot emit a source-only ordinal column; `MERGE … OUTPUT`
can reference source columns, so it pairs each generated `QueueID` with the caller-supplied
ordinal in the same output row. (`last_insert_rowid()` and `SCOPE_IDENTITY()` are per-statement
and meaningless for a multi-row insert.)

### Why only the body insert is multi-row

The body table has uniform columns (`Body`, `Headers`) for every message, so a single
multi-row statement is safe. The meta and status rows are inserted per message because their
columns can vary: user metadata (`AdditionalMetaData`) is set per message, so a multi-row meta
insert cannot assume a uniform column set across the batch. Keeping meta and status per-row
inside the single transaction still captures the dominant win, collapsing N connections, N
transactions, and N commits into one and batching id generation. A measured benchmark showed
about a 21× throughput improvement for a 500-message SQL Server batch (loop 6402 ms, batch
305 ms).

## Alternatives considered

- **Table-valued parameter (TVP).** One parameter regardless of batch size (no 2,100 limit, so
  larger chunks), but requires a predefined SQL table type created at install time, a schema or
  DDL dependency that complicates the transport's setup story. Deferred: the MERGE approach
  needs no new DDL. TVP remains a future option if very large single-statement batches are
  needed.
- **`SqlBulkCopy`.** Fastest for very large inserts, but returns no generated ids, which breaks
  the per-message id-return contract (we would need client-generated ids or a staging round
  trip). Rejected.
- **One transaction per chunk.** Bounds write-lock duration and memory, but reintroduces partial
  success at the chunk boundary, contradicting the chosen all-or-nothing semantics. Rejected.
- **Multi-row meta/status inserts.** A possible further optimization, but unsafe in general
  because user metadata columns can vary per message. Deferred as a future optimization for the
  uniform-metadata case.

## Consequences

- Bulk producers gain a large throughput improvement on SQL Server (and, in later phases,
  SQLite and PostgreSQL); the largest relative win is expected on SQLite.
- The batch path is all-or-nothing, a documented behavior change. Scheduled-job messages are
  not supported on the batch path (no batch equivalent of the per-message job-uniqueness query)
  and are rejected with a clear error; send them individually.
- A large batch holds the write transaction until it commits; on SQLite this serializes other
  writers for the duration. Acceptable because batch send is opt-in and the alternative breaks
  atomicity.
- No public API changes. Transports without a batch handler degrade gracefully to the loop.

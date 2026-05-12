# Project: Outbox Pattern Support for Relational Transports

## Description

Add producer-side support for caller-supplied transactions to the SqlServer and PostgreSQL transports, enabling the **transactional outbox pattern**: the caller writes business data and a queue message inside a single transaction so the two commit (or roll back) atomically.

DotNetWorkQueue's existing producer surface always opens and manages its own connection + transaction internally. This is the right default for fire-and-forget enqueue, but it precludes the outbox pattern because the queue INSERT is not part of the caller's business transaction. This project introduces a parallel, opt-in send surface on relational transports that accepts a `System.Data.Common.DbTransaction` from the caller, reuses its associated connection, performs the queue INSERTs inside that transaction, and never commits, rolls back, or disposes the caller-owned resources. Retry policies are skipped on this path — the caller owns the transaction lifecycle, and therefore also owns retry policy.

The feature is exposed via a derived interface `IRelationalProducerQueue<T> : IProducerQueue<T>` so non-relational transports (Redis, Memory, LiteDb) are unaffected and the existing public API is preserved.

## Goals

1. Enable the transactional outbox pattern on SqlServer and PostgreSQL transports.
2. Preserve `IProducerQueue<T>` exactly — no breaking changes for existing callers.
3. Keep transaction-lifecycle ownership boundaries explicit: caller owns the transaction, transport never commits/rolls back/disposes.
4. Validate at the API boundary (schema co-location, connection state, transaction state) so misconfiguration fails fast at the first `Send` call rather than as silent data drift in production.
5. Bypass internal retry policies on the caller-transaction path; caller drives retry semantics for the whole business operation.
6. Avoid landmines for non-relational transports: invisible API surface (capability-cast pattern), no `NotSupportedException` stubs.

## Non-Goals

- **SQLite outbox support.** Out of scope for this milestone. Design extends cleanly if requested later.
- **Consumer-side transaction enlistment** (inbox pattern). Existing `HoldTransactionUntilCommited` option on SqlServer/PostgreSQL covers the close-enough case for consumers.
- **Method producer / LINQ compiled-expression producer outbox support.** Outbox is a DTO-event pattern; deferred-execution producers are a different use case nobody has asked for.
- **`System.Transactions.TransactionScope` / ambient transaction enlistment.** Requires MSDTC for cross-connection promotion on SqlServer, is unsupported by Npgsql, and is a deprecated pattern.
- **Polly retries on the caller-transaction path.** The caller owns the transaction; retries are the caller's responsibility (e.g., wrap the entire business operation in their own retry policy and re-call `Send`).
- **Auto-creation of queue tables on first `Send`.** Caller deploys schema once via the existing `CreateQueue` API against their business database. DDL inside the caller's transaction is the wrong contract for outbox.
- **Memory, Redis, and LiteDb transports.** Outbox is inherently a single-database-transaction pattern; these transports have no equivalent.

## Requirements

### Functional — New Public API

- **New interface** `IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` in `DotNetWorkQueue.Transport.RelationalDatabase`, with six overloads:
  - `IQueueOutputMessage Send(TMessage, DbTransaction)`
  - `IQueueOutputMessage Send(TMessage, IAdditionalMessageData, DbTransaction)`
  - `Task<IQueueOutputMessage> SendAsync(TMessage, DbTransaction)`
  - `Task<IQueueOutputMessage> SendAsync(TMessage, IAdditionalMessageData, DbTransaction)`
  - `IQueueOutputMessages Send(IEnumerable<QueueMessage<TMessage, IAdditionalMessageData>>, DbTransaction)`
  - `Task<IQueueOutputMessages> SendAsync(IEnumerable<QueueMessage<TMessage, IAdditionalMessageData>>, DbTransaction)`
- **Capability cast usage pattern:**
  ```csharp
  var producer = container.CreateProducer<MyEvent, SqlServerMessageQueueInit>(conn);
  if (producer is IRelationalProducerQueue<MyEvent> rp)
  {
      using var tx = await myConn.BeginTransactionAsync();
      await businessRepo.SaveAsync(data, tx);
      await rp.SendAsync(new MyEvent(...), tx);
      await tx.CommitAsync();
  }
  ```
- The parameter type is `System.Data.Common.DbTransaction` (abstract base), not `IDbTransaction`, so a single overload covers both sync and async ADO.NET paths.
- The transaction's connection is reached via `tx.Connection`; the caller does not pass a separate connection parameter (ADO.NET binds transactions to the connection they were opened from).

### Functional — Internal Implementation

- `SendMessageCommand` (internal) gets an optional `DbTransaction ExternalTransaction { get; }` property. Default null preserves the existing self-managed path.
- `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` in **both** the SqlServer and PostgreSQL transports get a forked `HandleExternalTx(...)` path that:
  - Uses `command.ExternalTransaction.Connection` for all commands.
  - Sets `cmd.Connection = tx.Connection` and `cmd.Transaction = tx` on every `CreateCommand()` call (three INSERTs: message body, metadata, status — and any others enabled by configuration).
  - Never calls `tx.Commit`, `tx.Rollback`, `tx.Dispose`, `conn.Close`, or `conn.Dispose`.
  - Reuses the existing static SQL builders (`SendMessage.BuildMetaCommand`, `BuildStatusCommand`, etc.) — these have no connection-ownership coupling.
- `IConnectionHolder` / `IConnectionHolderFactory` is **bypassed entirely** on the caller-transaction path. No new "non-owning connection holder" variant is needed.
- Concrete `RelationalProducerQueue<TMessage>` lives in `Transport.RelationalDatabase` and inherits the existing producer, adding the tx-aware overloads. SqlServer and PostgreSQL DI registers the producer factory to return this concrete type; it still satisfies `IProducerQueue<T>` for non-outbox callers.
- The caller-transaction path bypasses the Polly retry decorator chain (`BeginTransactionRetryDecorator` and any related wrappers around the queue INSERT). The relational producer resolves the bare handler directly, or via a "no-retry" keyed registration.

### Functional — Validation

A single validator runs at the start of every tx-aware `Send`:

- `transaction != null` → else `ArgumentNullException`.
- `transaction.Connection != null` → else `InvalidOperationException` ("transaction disposed or completed").
- `transaction.Connection.State == ConnectionState.Open` → else `InvalidOperationException`.
- `transaction.Connection.Database` (canonical form) equals the queue's configured database → else `InvalidOperationException` with both database names in the message.
- Per-provider `IExternalDbNameExtractor`:
  - SqlServer: `conn.Database`, compared case-insensitively (`StringComparer.OrdinalIgnoreCase`).
  - PostgreSQL: `conn.Database`, compared case-sensitively (`StringComparer.Ordinal`).

### Ownership & Threading Contract (documented, not enforced)

- The producer never commits, rolls back, or disposes the caller's transaction or its connection.
- ADO.NET transactions are not thread-safe; the caller must not invoke `Send(msg, tx)` concurrently with their own writes on the same transaction from another thread. This matches the standard ADO.NET contract.
- The caller is responsible for retry policy. DNQ will not retry the queue INSERT on the caller-transaction path. To retry, the caller wraps the entire business operation (their writes + DNQ `Send`) in their own retry policy and re-calls `Send` from scratch.
- The caller must have run `CreateQueue` once at deployment time against their business database so the queue tables exist before the first `Send` call.

## Non-Functional Requirements

- **Backward compatibility:** Zero breaking changes to `IProducerQueue<T>`, `QueueContainer`, or any non-relational transport. Existing callers see no API surface differences.
- **Performance:** Caller-transaction path must be at least as fast as the owned-transaction path (it skips `BeginTransaction()`, `Commit()`, and the Polly decorator chain).
- **Diagnostics:** Validation exceptions must include both the offending database name and the queue's expected database name in their message.
- **Determinism:** Build must pass `-p:CI=true` for Source Link (existing convention).
- **Multi-targeting:** Compatible with net10.0 and net8.0 (existing TFMs).
- **Test coverage:** Both unit (handler logic, validation, ownership invariants) and integration (atomic commit/rollback verified against a real database) coverage on both transports. Integration tests must hit **every new public method** on `IRelationalProducerQueue<T>` for each transport — codecov coverage on this repo is driven primarily by integration tests, and `SendMessageCommandHandlerAsync` lives in a separate file from `SendMessageCommandHandler`, so async paths require their own integration coverage and cannot be inferred from sync coverage.

## Success Criteria

1. `IRelationalProducerQueue<T>` exists in `Transport.RelationalDatabase` and is implemented by the producer factories returned by SqlServer + PostgreSQL transports.
2. Memory, Redis, LiteDb, and SQLite producer instances do not implement `IRelationalProducerQueue<T>` — the `is` check fails for these.
3. Capability-cast pattern works: a caller resolving `IProducerQueue<T>` from a SqlServer or PostgreSQL transport can cast to `IRelationalProducerQueue<T>` and call the tx-aware overloads.
4. Atomic commit verified: an integration test enqueues a message + writes a business row + commits the caller's tx → both rows are present in the database.
5. Atomic rollback verified: an integration test enqueues a message + writes a business row + rolls back the caller's tx → neither row is present.
6. Cross-database validation: an integration test passing a transaction whose connection points to a different database than the queue → `InvalidOperationException` thrown before any DB write.
7. Caller-owned resources are not disposed: a unit test using a mocked `DbTransaction` + `DbConnection` asserts `Commit`, `Rollback`, `Dispose`, `Close` are never invoked by the transport.
8. Polly retry decorator bypass verified: under a forced transient failure, the caller-tx path throws to the caller after one attempt (not three).
9. All existing unit and integration tests still pass; no regressions on the owned-tx path.
10. Documentation page `docs/outbox-pattern.md` drafted in-repo, covering the caller-owned-transaction lifecycle contract, the caller-owned-retry contract, the capability-cast usage pattern, the schema-deployment prerequisite, per-provider DB-name comparison semantics, and the explicit "not supported on Memory/Redis/LiteDb/SQLite" callout.
11. Jenkins (full 14-stage matrix) passes on the feature branch via a draft PR before merge.

## Constraints

### Technical

- **API parameter type is `System.Data.Common.DbTransaction`** (abstract base), not `IDbTransaction`. Async ADO.NET methods are defined on the base class; using the interface would force a runtime downcast or doubled API surface.
- **Transaction–connection binding is hard-wired in ADO.NET.** The caller does not pass a separate connection; `tx.Connection` is the only valid connection for that transaction.
- **No DTC, no `System.Transactions`.** All commits are single-connection, single-database, single-process.
- **Existing DNQ conventions hold:**
  - Handlers operate on `IDbConnection`/`IDbCommand` abstractions (no sealed-type casts inside handlers) — except `tx.Connection` is `DbConnection`, which is fine because we never cast it further.
  - LGPL-2.1 license headers on all new source files.
  - Interface prefix `I`, factory suffix `Factory`, configuration suffix `Configuration`.
  - Thread-safe disposal via `Interlocked` where applicable.

### Scope

- **Transports:** SqlServer and PostgreSQL only. SQLite is explicitly deferred.
- **Producer surface:** POCO `Send` and `SendAsync` only (single + batch + with/without `IAdditionalMessageData`). Method-producer and LINQ-producer Sends are out of scope.
- **Consumer:** Untouched. Outbox is producer-side only.

### CI & Process

- Jenkins is PR-triggered. Feature-branch validation must open a draft PR to trigger the 14-stage matrix.
- Integration tests slot into the existing SqlServer and PostgreSQL Jenkins stages — no Jenkinsfile changes.
- Release publishing remains the tag-triggered `publish.yml` workflow. Local `dotnet nuget push` is not used.

### Risk Inventory

1. **Polly decorator bypass cleanness** (mid) — verify the bare handler is reachable without retry wrapping; investigate current decorator chain before committing to full plan.
2. **PostgreSQL batch `Send` tx binding** (mid) — if the batch path uses `NpgsqlBatch`, verify it correctly inherits the active transaction on the connection.
3. **PostgreSQL DB-name case semantics** (low) — quoted identifiers create edge cases; covered by focused unit test.
4. **Documentation discipline** (low, ship-blocking) — caller-owned-retry contract and lifecycle ownership must be published in `docs/outbox-pattern.md` with the release.

## Effort Estimate

**46–61 focused work hours** (~2–3 weeks elapsed time on this repo factoring CI cycles and PR review).

| Component | Hours |
|---|---:|
| `IRelationalProducerQueue<T>` + `RelationalProducerQueue<T>` | 2–3 |
| `SendMessageCommand.ExternalTransaction` property | 0.5 |
| SqlServer handlers (sync + async + batch) | 6–8 |
| PostgreSQL handlers (sync + async + batch) | 6–8 |
| Validation + per-provider `IExternalDbNameExtractor` | 2–3 |
| DI registration changes (RelationalDatabase + SqlServer + PostgreSQL inits) | 2–3 |
| Unit tests (~12–15) | 6–8 |
| Integration tests (~22 — 11 per provider, method-coverage matrix) | 14–18 |
| XML doc comments + `docs/outbox-pattern.md` draft | 3–4 |
| Review iterations | 4–6 |

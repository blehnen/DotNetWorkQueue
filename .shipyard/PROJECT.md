# Project: Inbox Pattern Support for Relational Transports (+ SQLite-Outbox Sweep)

## Description

Add consumer-side support for library-exposed transactions on the SqlServer, PostgreSQL, and SQLite transports, enabling the **transactional inbox pattern**: the user's message handler can write business data inside the same transaction the library uses to dequeue and commit the queue message, so the two commit (or roll back) atomically. This is the dual of the outbox milestone — where the caller owned the transaction and the library used it, here the library owns the transaction and the user joins it.

The mechanism is exposed via a derived interface `IRelationalWorkerNotification : IWorkerNotification` (in `DotNetWorkQueue.Transport.RelationalDatabase`) carrying the active `DbTransaction`. Non-relational transports (Memory, Redis, LiteDb) never implement it — capability-cast pattern, mirroring outbox. The feature is gated on the existing option `EnableHoldTransactionUntilMessageCommitted = true`: when the option is off, the relational notification impl simply does NOT implement `IRelationalWorkerNotification`, so the cast cleanly fails and the user gets a single discoverable signal that the feature is or isn't live.

This milestone also sweeps in **SQLite-outbox** as a co-feature, extending the producer-side `IRelationalProducerQueue<T>` interface from the outbox milestone to SQLite. The decision is symmetry-driven: shipping inbox on three relational transports while outbox lives on only two would leave a confusing asymmetric public surface ("you can do outbox on SqlServer/PG but only inbox on SQLite — why?"). Doing both directions on all three relational transports in one milestone keeps the relational surface coherent.

## Goals

1. Enable atomic "dequeue + business write" via a library-exposed `DbTransaction` on SqlServer, PostgreSQL, and SQLite consumers when `EnableHoldTransactionUntilMessageCommitted = true`.
2. Extend the existing outbox surface (`IRelationalProducerQueue<T>`) to SQLite producers, closing the asymmetry left by the prior milestone.
3. Preserve the existing public API for all six current transports — additive only; no breaking changes.
4. Preserve the existing handler programming model — user code remains `(IReceivedMessage<T>, IWorkerNotification) => ...`; the inbox capability is discovered via a single `is IRelationalWorkerNotification r` cast.
5. Keep ADO.NET types out of the transport-agnostic root assembly — all new interfaces live in `DotNetWorkQueue.Transport.RelationalDatabase`.
6. Match outbox-milestone test discipline — method-matrix-driven integration coverage on real DBs; negative-path tests confirming non-relational transports do NOT implement the capability.

## Non-Goals

- LiteDb inbox or outbox — LiteDb does not expose ADO.NET `DbTransaction` semantics; out of scope.
- Memory / Redis inbox or outbox — not relational; no transaction object to expose or accept.
- A `notification.AbortTransaction` flag for soft rollback — the contract is "throw to roll back, return to commit," matching DNQ's existing handler semantics. No new lifecycle state.
- Any change to the existing producer/consumer surface for non-relational transports.
- Auto-deduplication via the exposed transaction — user remains responsible for whatever inbox-dedupe table or strategy they need. The library exposes the seam; the pattern is user-implemented on top.
- Heartbeat refactoring — if heartbeats are already disabled in hold-tx mode (the held tx serves as the lease), the inbox milestone confirms this but does NOT redesign the heartbeat system.
- Any new opt-in flag in `ITransportOptions` — `EnableHoldTransactionUntilMessageCommitted` is already the gate; no second flag like `EnableInboxTransaction`.

## Requirements

### Functional — New Public API

- `IRelationalWorkerNotification : IWorkerNotification` in `DotNetWorkQueue.Transport.RelationalDatabase` with a single member `DbTransaction Transaction { get; }`. Contract: when the interface is implemented on a notification instance, `Transaction` is non-null. The presence of the interface IS the capability assertion.
- Per-transport `WorkerNotification` factories on SqlServer, PostgreSQL, and SQLite branch on `EnableHoldTransactionUntilMessageCommitted`:
  - Option `true` → construct a notification class that implements `IRelationalWorkerNotification` and surfaces the connection-holder's transaction.
  - Option `false` → construct the existing notification class that does NOT implement `IRelationalWorkerNotification`.
- The impl class declares only `: IRelationalWorkerNotification` (interface inheritance covers `IWorkerNotification`).
- For SQLite-outbox sweep:
  - `SqliteExternalDbNameExtractor : IExternalDbNameExtractor` — extracts `conn.DataSource` (file path). DB-name comparison semantics are a Phase 1 RESEARCH decision (see Risk Inventory §3).
  - `SqLiteMessageQueueInit` registers `IRelationalProducerQueue<T>` → `RelationalProducerQueue<T>` and the SQLite extractor.
  - `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` in `Transport.SQLite` gain the same `HandleExternalTx` fork as SqlServer + PostgreSQL — when `command.ExternalTransaction != null`, route through the caller's connection + tx, never `Commit`/`Rollback`/`Dispose`/`Close`. Batch path same fork.

### Functional — Internal Implementation

- The new relational notification impl on each transport pulls the `DbTransaction` from the per-message `ConnectionHolder` (the same holder that already owns the dequeue connection + transaction across `Receive → handler dispatch → RemoveMessage`).
- Wiring touches only the three relational transports' notification factories; `ConnectionHolder` itself does not change its public surface.
- Sync and async handler paths both receive `(IReceivedMessage<T>, IWorkerNotification)` and the cast works identically on both. No fork in the seam.

### Functional — Validation

- Inbox path needs no `ExternalTransactionValidator` analog — the library owns the connection and transaction by construction; no cross-DB or wrong-state risk.
- SQLite-outbox path uses the existing `ExternalTransactionValidator` plus the new `SqliteExternalDbNameExtractor` — identical to how SqlServer + PostgreSQL use their per-provider extractors.

### Ownership & Threading Contract (documented, not enforced)

**Inbox — library owns the transaction:**
- User MUST NOT call `Commit()`, `Rollback()`, `Dispose()`, or `Close()` on the exposed `DbTransaction` or its `Connection`.
- User MUST NOT stash the transaction reference past the handler's return — it is released by the library after `RemoveMessage`.
- User MUST NOT pass the reference to another thread — `DbTransaction` is not thread-safe.
- Library WILL commit on successful handler return (existing `RemoveMessage` path) and roll back on handler throw (existing rollback-on-throw path). User's business write rides along.
- User signals rollback by throwing. There is no soft-abort flag.

**Outbox — caller owns the transaction (unchanged from outbox milestone):**
- Library NEVER calls `Commit`/`Rollback`/`Dispose`/`Close` on the caller's transaction or connection.
- Caller owns retry policy on the caller-tx path (Polly retry decorator skipped, per outbox Phase 1 resolution).

## Non-Functional Requirements

- **Multi-targeting**: net10.0 + net8.0 across all changed projects (matches the rest of the codebase).
- **Build cleanliness**: `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln` produces zero errors, zero new XML-doc warnings (CS1591), zero new analyzer warnings.
- **No ADO.NET in the root assembly**: `DotNetWorkQueue` (root project) must not gain a reference to `System.Data.Common`, `Microsoft.Data.SqlClient`, `Npgsql`, or `Microsoft.Data.Sqlite` as a result of this work.
- **`IDbConnection` discipline preserved**: no new sealed-type casts to `NpgsqlConnection` / `SqlConnection` / `SqliteConnection` in handler code. Continue using `IDbConnectionFactory` for test seams.
- **Async handler mocking pattern**: any new async query/command handlers use `DbConnection` / `DbCommand` / `DbDataReader` abstract bases (not interfaces) for async-method mockability.
- **Deterministic builds**: Release builds pass `-p:CI=true` for Source Link determinism.
- **CI**: Jenkins 14-stage parallel matrix continues to pass; no new flake sources introduced. PRs trigger Jenkins via `gh pr create --draft`.

## Success Criteria

1. `IRelationalWorkerNotification : IWorkerNotification` is `public` in `DotNetWorkQueue.Transport.RelationalDatabase` and carries a single `DbTransaction Transaction { get; }` member with full XML doc.
2. Capability cast smoke test for each of the 3 relational transports: with `EnableHoldTransactionUntilMessageCommitted = true`, `container.GetInstance<IConsumerQueue<...>>()` yields a notification that `is IRelationalWorkerNotification` and exposes a non-null `Transaction`. With the option false, the cast fails on the same transport.
3. Negative-path coverage: Memory, Redis, LiteDb notifications never implement `IRelationalWorkerNotification` (unit tests confirm).
4. Atomic-commit integration test (per relational transport, both sync and async handlers): handler writes to a business table on `r.Transaction.Connection`; handler returns; verify both rows visible in a separate connection. ≥6 tests.
5. Atomic-rollback integration test (per relational transport, both sync and async handlers): handler writes to business table; handler throws; verify neither row visible. ≥6 tests.
6. SQLite-outbox vertical slice: 12 integration tests on SQLite mirroring SqlServer + PG outbox Phase 6 coverage (method × outcome × validation × retry-bypass).
7. SQLite extractor unit-test coverage: round-trip on path string, plus the DB-name comparison semantics chosen in RESEARCH §1.
8. Zero `Commit`/`Rollback`/`Dispose`/`Close` calls from the library on a caller-supplied tx in SQLite-outbox path (mocked unit test, matching the SqlServer + PG Phase 3/4 assertions).
9. `docs/inbox-pattern.md` published, including the lifecycle contract, the "heartbeats disabled in hold-tx mode" explanation, a worked example, and the per-provider DB-name comparison semantics for outbox (now three transports).
10. README updated with a one-paragraph pointer to the new docs alongside the existing outbox pointer.
11. Jenkins SqlServer + PostgreSQL + SQLite integration stages green on the milestone PR.
12. `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln` clean (0 errors, 0 new CS1591, NU1902 advisory carry-forward via `<WarningsNotAsErrors>` is acceptable per Phase 7 lessons).

## Constraints

### Technical

- The feature must reuse `ConnectionHolder` and its existing connection + transaction ownership — no new connection-lifetime abstraction.
- ADO.NET types stay out of `DotNetWorkQueue` root assembly. `IRelationalWorkerNotification` and friends live in `Transport.RelationalDatabase`.
- The relational notification impl class is `internal` per transport (only the interface is `public`).
- SQLite's `Microsoft.Data.Sqlite` is the supported driver — no support for `System.Data.SQLite`.
- No new `ITransportOptions` flags. `EnableHoldTransactionUntilMessageCommitted` is the existing gate.
- SQLite-outbox extractor's DB-name comparison must not be platform-conditional in user-facing behavior; the resolution (case-folding strategy + `Path.GetFullPath()` canonicalization) is decided in RESEARCH §1 and applied uniformly on all OSes.

### Scope

- Three relational transports only: SqlServer, PostgreSQL, SQLite.
- One milestone covers both directions: inbox (new) on all three, outbox (extending existing) on SQLite. No further deferrals.
- Heartbeat-disabled-in-hold-tx-mode is **confirmed**, not redesigned. If the audit surfaces a transport where heartbeats fire during a held tx and conflict, scope decision: file an ISSUE and document the limitation; do NOT bundle a heartbeat redesign into this milestone.

### CI & Process

- PRs must be draft-opened against master to trigger Jenkins (PR-trigger pattern, per repo CI lesson).
- Integration tests against SQLite, SqlServer, PostgreSQL run in the existing Jenkins 14-stage parallel matrix; no Jenkinsfile changes expected.
- Queue names in integration tests use `Guid.NewGuid().ToString("N")` to avoid DNQ's hyphen rejection.
- Phases follow the established Shipyard workflow: brainstorm → roadmap → plan → build → review → verify → simplify → audit → docs → ship.

### Risk Inventory

1. **Heartbeats during hold-tx (audit risk).** User stated heartbeats are likely disabled when `EnableHoldTransactionUntilMessageCommitted = true` because the held tx blocks them. Phase 1 spike CONFIRMS this on all three relational transports. If a transport unexpectedly issues heartbeats during a held tx, scope decision required (file ISSUE, document limitation, do not redesign heartbeat in this milestone).
2. **Library-issued command timeouts during a slow handler.** `RemoveMessage` and any other internal commands that run on the held connection after the handler returns must have timeouts compatible with the latency budget of slow handlers. Audit needed; document any sizing recommendations.
3. **SQLite DB-name comparison semantics.** "DB names" are file paths — case-insensitive on Windows, case-sensitive on Linux. Candidate resolutions: `Path.GetFullPath()` + `OrdinalIgnoreCase` (matches Windows precedent, somewhat permissive on Linux) vs `Path.GetFullPath()` + `Ordinal` (strict, matches Linux filesystem). Decision in RESEARCH §1.
4. **SQLite single-writer concurrency under hold-tx.** A held tx in SQLite blocks all other writers. Not a code risk; document in `docs/inbox-pattern.md` as a user-visible characteristic.
5. **`NpgsqlBatch` transaction binding — already resolved in outbox Phase 4.** Reuse existing learnings; no fresh investigation needed.
6. **Documentation completeness (ship-blocker).** `docs/inbox-pattern.md` plus the SQLite-outbox additions to `docs/outbox-pattern.md` must land in this milestone — same ship-blocking criterion as outbox Phase 7.

## Effort Estimate

Comparable to the outbox milestone with a SQLite-outbox sweep added: ~7 phases, ~36 integration tests (24 inbox + 12 SQLite-outbox), plus unit-test scaffolding and docs. Roughly 30–50% larger surface than outbox by integration-test count, but the inbox half is structurally lighter (no `ExternalTransactionValidator` analog, no Polly bypass design — both are passthrough). SQLite-outbox half is mechanically a duplicate of the SqlServer / PG outbox work, so plan-level risk is low.

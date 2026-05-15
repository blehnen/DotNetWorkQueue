# Phase 3 Context: SqlServer Implementation + Unit Tests

Source phase description: `.shipyard/ROADMAP.md` §Phase 3.
Source project: `.shipyard/PROJECT.md`.
Prior phases: `.shipyard/notes/phase-1-polly-bypass-spike.md` (Risk #1 closed); `.shipyard/phases/2/CONTEXT-2.md` + Phase 2 plans (foundation layer — planned, not yet built at the time of this planning session).

## Phase Scope (from ROADMAP.md + user decisions below)

Phase 3 ships the SqlServer half of the outbox feature end-to-end:

1. `SqlServerExternalDbNameExtractor : IExternalDbNameExtractor` — case-insensitive `conn.Database` comparison.
2. `SqlServerRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage>` — overrides the four `protected virtual SendWithExternalTransaction*` hooks introduced by Phase 2 PLAN-2.2 to invoke the SqlServer `SendMessageCommandHandler` (sync) and `SendMessageCommandHandlerAsync` (async) directly.
3. `HandleExternalTx` fork inside the existing `SendMessageCommandHandler.Handle()` and `SendMessageCommandHandlerAsync.HandleAsync()` — early-branch on `command.ExternalTransaction != null`, reuses `tx.Connection` and binds `cmd.Connection`/`cmd.Transaction` on every command, never calls Commit/Rollback/Dispose/Close.
4. `SQLServerMessageQueueInit` DI updates to register the extractor + producer subclass.
5. SqlServer unit tests (~7–8) covering: branch selection on null vs non-null `ExternalTransaction`, validator invocation, no-Commit/Rollback/Dispose/Close on mocked tx + conn, OrdinalIgnoreCase comparison correctness, retry-decorator bypass end-to-end-ish (the bypass branch itself is Phase 2 PLAN-3.1; Phase 3 confirms the integration through the SqlServer producer).

## User Decisions

### Decision 1: Batch send on caller-tx path — SEQUENTIAL LOOP OVER SINGLE-SEND OVERRIDE

`SendMessages<T>.Send(List<>)` in `Transport.Shared` currently uses `Parallel.ForEach`, which is **unsafe** for caller-supplied `DbTransaction` because ADO.NET transactions are not thread-safe (PROJECT.md §Ownership & Threading explicitly documents this).

`SqlServerRelationalProducerQueue<T>` overrides `SendWithExternalTransactionBatch` (sync) and `SendWithExternalTransactionBatchAsync` (async) to iterate the input list **sequentially** (`for` / `foreach`, not `Parallel.ForEach`) and call the single-send override per item. Each call validates the tx once and dispatches one `RelationalSendMessageCommand` to the handler fork.

- **Failure aggregation:** mirror the existing `SendMessages<T>` pattern — catch per-item exceptions and aggregate into `IQueueOutputMessages` with the failing message's exception attached. Caller decides whether to rethrow or continue (typical outbox usage: caller wraps the whole batch + business writes in their own tx and rolls back on any error).
- **Async batch:** sequential `await`s in a `foreach`. Order-preserving.

**Rejected:** A dedicated batch-aware handler that fans all INSERTs in one method. Duplicates SQL plumbing and forces PostgreSQL Phase 4 to make the `NpgsqlBatch` decision (Risk #2). The sequential-loop approach defers Risk #2 to the same scope it was already scoped under and reuses the validated single-send path.

**Rejected:** Deferring batch to a later phase. PROJECT.md §Functional New Public API lists all 6 overloads as in-scope for the feature; carving batch out would force a second roadmap revision and ship an incomplete public API in the first release.

### Decision 2: Handler-fork placement — EARLY BRANCH INSIDE EXISTING HANDLER

The `HandleExternalTx` fork lives inside the existing `SendMessageCommandHandler.cs` and `SendMessageCommandHandlerAsync.cs` files in `Transport.SqlServer`. The branch is added at the top of `Handle()`/`HandleAsync()`, immediately after any required null-guards:

```csharp
public long Handle(SendMessageCommand command)
{
    if (command.ExternalTransaction != null)
        return HandleExternalTx(command);
    // ... existing self-managed-tx path unchanged
}

private long HandleExternalTx(SendMessageCommand command)
{
    // tx.Connection -> cmd.Connection, cmd.Transaction on every command
    // Reuse SendMessage.BuildMetaCommand / BuildStatusCommand
    // Never call Commit/Rollback/Dispose/Close
}
```

- **Mirrors the Phase 1 retry-decorator bypass shape** — a small early-return branch at the top of an existing handler is the same surface area as the `IRetrySkippable` check in `RetryCommandHandlerOutputDecorator`.
- **Minimal diff** — one new private method per handler, no new public DI registration for the fork itself (the producer subclass is what's new in DI; the fork is just a code path inside the existing registered handler).
- **Co-located SQL** — the existing handler already knows how to build the three INSERTs (message body, metadata, status); the fork reuses `SendMessage.BuildMetaCommand`, `BuildStatusCommand`, etc., which are static and have no connection-ownership coupling (PROJECT.md §Functional Internal Implementation).

**Rejected:** A new sibling handler class (`SqlServerSendMessageExternalTxCommandHandler`). Cleaner separation on paper, but duplicates SQL plumbing, doubles the registered handler surface, and forces the producer subclass to take a dependency on a second handler type. The single-file fork is the standard DNQ pattern for opt-in execution variants.

### Decision 3: Validator invocation site — IN PRODUCER OVERRIDE BEFORE COMMAND CONSTRUCTION

`SqlServerRelationalProducerQueue<T>.SendWithExternalTransaction(...)` (and async + batch variants) calls `validator.Validate(tx)` as the first operation, **before** constructing the `RelationalSendMessageCommand`. The handler fork itself does not re-validate.

- **Fail-fast at the API boundary** — caller's `Send(msg, tx)` returns the validation failure immediately, before any handler resolution, command allocation, or DI traversal.
- **Matches CONTEXT-2 Decision 4** — the validator is a standalone class meant to run at the producer surface; embedding it in the handler would duplicate the "API-boundary check" role.
- **Validator is constructor-injected** into `SqlServerRelationalProducerQueue<T>` (resolved from DI alongside the existing producer dependencies). Lifetime per DNQ convention — architect decides scoped vs transient based on `IConnectionInformation` lifetime (which it depends on).

**Rejected:** First line of `HandleExternalTx`. Makes the handler responsible for boundary validation, which is not the handler's job; also means a future direct-handler caller path would have to opt in to validation.

**Rejected:** Both producer + handler. Redundant work on every call; no scenario justifies defense-in-depth here because the only way to reach the handler with an `ExternalTransaction` is through `RelationalSendMessageCommand`, which only the producer override constructs.

### Decision 4: Plan structure — 3 PLANS, 2 WAVES

Phase 3 is sliced into three plans across two waves:

- **Wave 1 (1 plan): PLAN-1.1 — Foundation.** SqlServer-specific extractor + DI wiring + producer subclass. Three tasks: (a) `SqlServerExternalDbNameExtractor`, (b) `SqlServerRelationalProducerQueue<T>` subclass with overrides routing to the sync + async handler interfaces (the overrides themselves are present but the handler fork they target is Wave 2 — until W2 lands, the overrides resolve handlers whose `Handle`/`HandleAsync` will branch back to the self-managed path because `ExternalTransaction` is non-null but the fork isn't there yet, which compiles cleanly and is caught by W2 tests), (c) `SQLServerMessageQueueInit` registrations.

  *Wave-1 ordering note:* the subclass + DI changes compile against Phase 2's `RelationalProducerQueue<T>` virtual hooks even before Phase 2 is built — they are additive only. Phase 2's `RelationalProducerQueue<T>` base class throws `InvalidOperationException` from each virtual; Phase 3 W1 overrides those to delegate to the SqlServer handler. The overridden methods compile fine; runtime behavior is exercised by W2 tests.

- **Wave 2 (2 parallel plans):**
  - **PLAN-2.1 — Sync handler fork + tests.** Modify `SendMessageCommandHandler.cs` to add the `HandleExternalTx` fork. Add 3–4 sync unit tests (branch selection, no-Commit/Rollback/Dispose/Close on mocked tx, OrdinalIgnoreCase via the extractor unit test, retry-decorator-not-invoked via the existing Phase 2 PLAN-3.1 sync test extended). Three tasks per the ≤3 rule.
  - **PLAN-2.2 — Async handler fork + tests.** Modify `SendMessageCommandHandlerAsync.cs` to add the `HandleExternalTxAsync` fork. Mirror sync tests with async mocking — per CLAUDE.md async-mocking lesson, mock `DbConnection`/`DbCommand`/`DbDataReader` (abstract base classes), not interfaces. Three tasks.

  *Parallelism justification:* W2 plans touch disjoint files (`SendMessageCommandHandler.cs` vs `SendMessageCommandHandlerAsync.cs`) and disjoint test files. Producer subclass and extractor are W1-completed by then.

**Rejected:** 4 plans / 3 waves — adds a wave gate without unblocking parallelism (producer subclass and handlers are independent enough to parallelize after extractor + DI lands).

**Rejected:** 2 plans / 1 wave — would violate the ≤3 tasks/plan rule (foundation + sync handler + async handler + tests = ≥10 tasks in one plan).

## Hard Rules / Cross-Cutting Constraints

- **No `Microsoft.Data.SqlClient.SqlConnection` casts inside the handler fork.** Operate on `IDbConnection` / `DbConnection` only. CLAUDE.md hard rule (the `SetJobLastKnownEventCommandHandler` PostgreSQL re-refactor at commit `9c77537d` is the precedent).
- **`IDbConnectionFactory` is bypassed on the caller-tx path.** The handler fork reads `command.ExternalTransaction.Connection` directly. The existing `IConnectionHolder` / `IConnectionHolderFactory` path is also bypassed (PROJECT.md §Functional Internal Implementation).
- **No `tx.Commit`/`Rollback`/`Dispose`/`conn.Close`/`Dispose` calls anywhere on the caller-tx path.** Asserted by a dedicated unit test per CLAUDE.md async-mocking lesson (mock the abstract base classes for async).
- **Sync vs async test seam split** (CLAUDE.md): sync handler tests use `IDbConnection`/`IDbCommand`/`IDataReader` interface mocks via NSubstitute; async handler tests MUST mock `DbConnection`/`DbCommand`/`DbDataReader` abstract base classes (otherwise `OpenAsync`/`ExecuteReaderAsync`/`ReadAsync` silently no-op).
- **Static SQL builders unchanged.** `SendMessage.BuildMetaCommand`, `SendMessage.BuildStatusCommand`, and their siblings are reused as-is by the handler fork. The fork's only divergence is connection + transaction binding.
- **LGPL-2.1 license headers on every new source file** (DNQ convention).
- **XML doc comments on every new public type/member** added in Phase 3 — `SqlServerExternalDbNameExtractor`, `SqlServerRelationalProducerQueue<T>` public surface, and any new public overloads. Missing-doc warnings under `TreatWarningsAsErrors` will break the build (Phase 7 will do the final docs pass but Phase 3 must not introduce warnings).
- **Build must pass net10.0 + net8.0** with `-p:CI=true` for Release inspection.
- **Producer subclass naming:** `SqlServerRelationalProducerQueue<TMessage>` (not just `SqlServerProducerQueue<T>` — the "Relational" disambiguator matches the base class name and signals that this subclass implements the relational outbox surface).
- **Extractor naming:** `SqlServerExternalDbNameExtractor` (matches PROJECT.md §Validation per-provider extractor naming).

## Exit Criteria for Phase 3

1. `Transport.SqlServer` builds clean on net10.0 + net8.0 with `TreatWarningsAsErrors` and zero new XML-doc warnings.
2. All new SqlServer unit tests pass; all existing SqlServer unit tests still pass.
3. SimpleInjector capability-cast smoke test passes: `container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` is `true` when the container is built from `SqlServerMessageQueueInit`.
4. Mock-based unit test confirms zero `Commit`/`Rollback`/`Dispose`/`Close` calls on the caller's `DbTransaction` or its `DbConnection` across both sync and async paths and across single + batch (4 mock-assertion tests minimum — PROJECT.md §Success Criteria #7).
5. Retry-bypass integration with `RelationalSendMessageCommand`: a unit test confirms that the SqlServer `SendMessageCommandHandler` dispatches into the `HandleExternalTx` fork when the producer routes a caller-tx command through the registered handler chain. The Polly bypass itself is Phase 2's PLAN-3.1 territory; Phase 3 verifies the integration end-to-end (PROJECT.md §Success Criteria #8 partial — full integration coverage lives in Phase 6).
6. `SqlServerExternalDbNameExtractor` uses `StringComparer.OrdinalIgnoreCase`; a unit test confirms `"MyDb"` and `"mydb"` compare equal and `"MyDb"` and `"MyOtherDb"` compare unequal.
7. No `SqlConnection` casts in any handler code (grep gate; same rule as the PostgreSQL re-refactor lesson in CLAUDE.md).
8. `SQLServerMessageQueueInit` registers both the extractor and the producer subclass; the producer factory returns the concrete `SqlServerRelationalProducerQueue<T>` so capability-cast works.

## Out of Scope (Phase 3)

- **PostgreSQL implementation** — Phase 4.
- **Negative-path tests on non-relational transports** — Phase 5.
- **Real-DB integration tests (atomic commit/rollback)** — Phase 6.
- **`docs/outbox-pattern.md` page** — Phase 7. XML doc comments on new public surface land in Phase 3 to keep `TreatWarningsAsErrors` happy.
- **`SendMessages<T>.Send(List<>)` `Parallel.ForEach` change** — only the relational batch *override* in `SqlServerRelationalProducerQueue<T>` is sequential. The base `SendMessages<T>.Send(List<>)` self-managed-tx path retains its existing parallelism unchanged.
- **Method producer / LINQ producer caller-tx** — out of scope per PROJECT.md §Non-Goals.

## Dependencies

- **Phase 2** must be planned (it is — `.shipyard/phases/2/plans/PLAN-*.md`). Phase 3 compiles against Phase 2's API surface (`IRelationalProducerQueue<T>`, `RelationalProducerQueue<T>` virtual hooks, `IRetrySkippable`, `RelationalSendMessageCommand`, `ExternalTransactionValidator`, `IExternalDbNameExtractor`). Phase 2 does NOT need to be **built** before Phase 3 plans are written, but Phase 2 MUST be built before Phase 3 is built. Builder for Phase 3 will encounter compile errors if launched while Phase 2 is unbuilt; that ordering is enforced at build time, not plan time.
- **Phase 1** spike outputs (`.shipyard/notes/phase-1-polly-bypass-spike.md`) inform the handler-fork shape decision but introduce no code dependency.

## Notes for Researcher / Architect

- The Phase 1 spike conclusively located `SendMessageCommandHandler.cs:39` and `SendMessageCommandHandlerAsync.cs:39` as the concrete handlers, and confirmed the decorator chain wrapping them is `TraceDecorator -> RetryDecorator -> Handler`. The trace decorator sits OUTSIDE the retry decorator, so the `HandleExternalTx` fork at the bottom of the chain still gets traced — observability is preserved on the caller-tx path (Phase 1 memo §Design justification).
- The existing `SendMessage.cs` in `Transport.SqlServer/Basic/CommandHandler/` contains the static SQL builders (`BuildMetaCommand`, `BuildStatusCommand`, etc.). The fork reuses these — researcher should confirm none of the builders take a `DbConnection`/`DbTransaction` parameter and that the connection/transaction is set on the `IDbCommand` returned by the builder.
- `SendMessages<T>` in `Transport.Shared/Basic/SendMessages.cs:32` is the cross-transport orchestrator that constructs `SendMessageCommand` instances. The `RelationalProducerQueue<T>` virtual overrides bypass this class entirely on the caller-tx path — they construct `RelationalSendMessageCommand` directly and call the handler. Architect should confirm the producer override has access to the registered `ICommandHandler<SendMessageCommand, long>` (sync) and `ICommandHandlerWithOutputAsync<SendMessageCommand, long>` (async) — likely via constructor injection from the SqlServer init.
- Phase 2 PLAN-2.2 establishes `RelationalProducerQueue<T>` constructor signature. Phase 3 producer subclass must call `base(...)` with the same parameters plus its own additions (extractor, validator, handler refs).

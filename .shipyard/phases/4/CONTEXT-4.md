# Phase 4 Context: PostgreSQL Implementation + Unit Tests

Source phase description: `.shipyard/ROADMAP.md` §Phase 4.
Source project: `.shipyard/PROJECT.md`.
Prior phase: `.shipyard/phases/3/results/SUMMARY-*.md` (SqlServer reference shipped end-to-end at the unit-test level).

## Phase Scope (from ROADMAP.md + user decisions below)

Phase 4 ships the PostgreSQL half of the outbox feature end-to-end, mirroring Phase 3's shape:

1. `PostgreSqlExternalDbNameExtractor : IExternalDbNameExtractor` — pass-through `connection.Database` (case-sensitive per PostgreSQL semantics — see Decision 2).
2. `PostgreSqlRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage>` — overrides the four `protected virtual SendWithExternalTransaction*` hooks from Phase 2. Each override: validator first, then `GuardNpgsqlTransaction` (cast guard for `NpgsqlTransaction`), then dispatch `RelationalSendMessageCommand` to the registered handler.
3. `HandleExternalTx` fork inside `Transport.PostgreSQL/.../SendMessageCommandHandler.cs` and `HandleExternalTxAsync` fork inside the matching `*Async.cs` handler — early-branch on `command.ExternalTransaction != null`, reuses `tx.Connection` as `NpgsqlConnection`, never calls Commit/Rollback/Dispose/Close.
4. `PostgreSQLMessageQueueInit` DI updates: 1 extractor singleton + 1 validator singleton + 3 open-generic producer mappings via `RegisterConditional` (Decision 5).
5. PostgreSQL unit tests (~9 mirroring Phase 3 Wave 1's count) + 6 structural smoke tests (3 sync fork + 3 async fork mirroring Phase 3 Wave 2). One additional **quoted-identifier edge-case test** (Risk #3 closure — see Decision 2).

## User Decisions

### Decision 1: Batch send on caller-tx path — SEQUENTIAL LOOP (mirror Phase 3)

`PostgreSqlRelationalProducerQueue<T>` overrides `SendWithExternalTransactionBatch` (sync) and `SendWithExternalTransactionBatchAsync` (async) to iterate the input list **sequentially** (`foreach`, NOT `Parallel.ForEach`) and call the single-send override per item. ADO.NET transactions are not thread-safe — PROJECT.md §Ownership & Threading documents this.

- **Failure aggregation:** mirror Phase 3 — catch per-item exceptions and aggregate into `QueueOutputMessage(sentMessage, error)` results.
- **Async batch:** sequential `await`s in a `foreach`. Order-preserving.

**Rejected: NpgsqlBatch experiment (Risk #2 spike).** Phase 3 chose sequential-loop for SqlServer; mirror that for cross-transport consistency. NpgsqlBatch transaction binding is a worthwhile optimization but defer it to a future performance phase if benchmarks demand it. **Risk #2 closed by deliberately deferring the spike** — the sequential-loop pattern is proven to work in Phase 3.

**Rejected: NpgsqlBatch optimistic adoption.** Late-discovery rework risk on the 14-stage Jenkins integration test cycle (Phase 6) is too high.

### Decision 2: PostgreSQL case-sensitive extractor — PASS-THROUGH VERBATIM (Risk #3 closure)

`PostgreSqlExternalDbNameExtractor.Extract(DbConnection connection)` returns `connection.Database` **verbatim** (no upper-casing, no lower-casing, no normalization). The Phase 2 validator uses `StringComparison.Ordinal` for the comparison; pass-through extraction matches PostgreSQL's case-sensitive catalog semantics exactly.

- **Why this is correct for PG:** PostgreSQL folds unquoted identifiers to lowercase and preserves case in quoted identifiers (`"MyDb"`). The `connection.Database` value as reported by Npgsql is the actual stored database name (already case-correct). Pass-through extraction means the validator compares `connection.Database == _connectionInfo.Container` byte-for-byte under `StringComparison.Ordinal`.
- **Implication for users:** A queue connection string `Database=MyDb` will only validate against an actual PG database named `MyDb` (mixed case) — not `mydb` (lowercase). Users with quoted-identifier database names must populate `Container` with the exact case-preserved name.
- **Risk #3 closure:** Add a unit test that verifies the case-sensitive comparison: queue configured for `"MyDb"`, extractor returns `"mydb"` (or vice versa) → validator throws `InvalidOperationException` with both names in the message.

**Rejected: Lowercase normalization** — silently changes the comparison semantics for users who legitimately use quoted-identifier mixed-case databases.

**Rejected: Configurable comparer** — Phase 2 deliberately delegated case semantics to per-provider extractors to avoid this complexity.

### Decision 3: Handler-fork placement — EARLY BRANCH INSIDE EXISTING HANDLER (mirror Phase 3)

The `HandleExternalTx` fork lives inside the existing `SendMessageCommandHandler.cs` and `SendMessageCommandHandlerAsync.cs` files in `Transport.PostgreSQL/Basic/CommandHandler/`. Same insertion-point pattern as Phase 3:

```csharp
public long Handle(SendMessageCommand command)
{
    Guard.NotNull(() => command, command);  // existing
    // ... existing lazy-init block ...
    if (commandSend.ExternalTransaction != null)
        return HandleExternalTx(commandSend);
    // ... existing self-managed-tx path unchanged
}

private long HandleExternalTx(SendMessageCommand commandSend)
{
    // (NpgsqlTransaction)commandSend.ExternalTransaction
    // (NpgsqlConnection)sqlTx.Connection
    // Reuses Phase 2 inherited static command-builders (SendMessage.BuildMetaCommand, etc.)
    // Never call Commit/Rollback/Dispose/Close
}
```

Async mirror uses `private async Task<long> HandleExternalTxAsync(...)`. Same pattern as Phase 3 PLAN-2.1 + PLAN-2.2.

**Note:** Both `SqlConnection` and `NpgsqlConnection` are sealed types. The handler fork's blind cast (e.g., `(NpgsqlTransaction)commandSend.ExternalTransaction`) is contractually safe because the producer subclass's `GuardNpgsqlTransaction` is the only public path that sets non-null `ExternalTransaction` on `RelationalSendMessageCommand`. Phase 3's auditor confirmed this design pattern.

### Decision 4: Validator invocation site — IN PRODUCER OVERRIDE BEFORE COMMAND CONSTRUCTION (mirror Phase 3)

`PostgreSqlRelationalProducerQueue<T>.SendWithExternalTransaction(...)` (and async + batch variants) calls `validator.Validate(tx)` as the first operation, **before** constructing the `RelationalSendMessageCommand`. The handler fork itself does not re-validate.

Same rationale as Phase 3 CONTEXT-3 Decision 3:
- Fail-fast at the API boundary.
- Matches Phase 2 CONTEXT-2 Decision 4 (standalone validator at producer surface).
- Single Validate call per batch (validator's checks are tx-level, not message-level).

### Decision 5: ENCODE PHASE 3 LESSONS AS HARD CONTEXT-4 RULES

These are not soft suggestions — they are hard rules the Phase 4 builder must follow without rediscovery:

**Rule A: DI registration must use `RegisterConditional`, NOT plain `Register`.**
- The 3 open-generic producer mappings (`IProducerQueue<>`, `IRelationalProducerQueue<>`, `RelationalProducerQueue<>` → `PostgreSqlRelationalProducerQueue<>`) MUST use `container.RegisterConditional(...)` not `container.Register(...)`.
- Reason: plain `Register` triggers SimpleInjector's `EnableAutoVerification` on first resolve, which surfaces pre-existing repo-wide diagnostic warnings (transient `IDisposable` types `IMessageContext`, `IWorker`, `IPrimaryWorker`) and breaks 6+ pre-existing tests (`PostgreSQL.Tests` likely has a similar `QueueCreatorTests`-style test suite that asserts `NpgsqlException` is thrown). `RegisterConditional` preserves lazy verification semantics matching the existing fallback pattern in `ComponentRegistration.RegisterFallbacks`.
- Reference: Phase 3 SUMMARY-1.1.md "Decisions Made" section + REVIEW-1.1.md "Deviation Audit".

**Rule B: Lifecycle-invariant source comment MUST be rephrased to avoid forbidden substrings.**
- The trailing comment in `HandleExternalTx[Async]` documenting the lifecycle-ownership contract MUST NOT contain `.Commit()`, `.Rollback()`, `.Close()`, or `.Dispose()` as substrings. The smoke test `HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose` greps the fork's source-text body for these substrings.
- **Use this exact wording:** `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
- Reason: matches Phase 3 PLAN-2.2 (async) builder's source-side approach. Phase 3 PLAN-2.1 (sync) builder used a different test-side workaround (line-comment stripping), but the cross-plan inconsistency was flagged in REVIEW-2.1.md as "cosmetic only" Minor. Phase 4 should converge on the simpler source-side rephrase for consistency.
- Reference: Phase 3 SUMMARY-2.2.md "Decisions Made", REVIEW-2.1.md, REVIEW-2.2.md.

**Rule C: Producer-subclass constructor signature MUST mirror Phase 3 SqlServer (11-param).**
- `PostgreSqlRelationalProducerQueue<T>` constructor takes 11 params: 6 base (`QueueProducerConfiguration, ISendMessages, IMessageFactory, ILogger, GenerateMessageHeaders, AddStandardMessageHeaders`) + 5 new (`sendHandler, sendHandlerAsync, validator, sentMessageFactory, ownMessageFactory`).
- The `ownMessageFactory` parameter is a second `IMessageFactory` injection: necessary because the Phase 2 base `RelationalProducerQueue<T>` does NOT expose `_messageFactory` as `protected` (a known limitation acknowledged in Phase 2 SUMMARY-2.2). SimpleInjector handles the double-injection by passing the same singleton instance to both slots.
- Reference: Phase 3 SUMMARY-1.1.md "Decisions Made" + plan body.

### Decision 6: Plan structure — MIRROR PHASE 3 (3 PLANS, 2 WAVES)

Phase 4 is sliced into three plans across two waves, exactly matching Phase 3:

- **Wave 1 (1 plan): PLAN-1.1 — Foundation.** PostgreSQL-specific extractor (pass-through; case-sensitive Risk #3 test) + `PostgreSqlRelationalProducerQueue<T>` subclass with overrides routing to the sync + async PG handler interfaces + `PostgreSQLMessageQueueInit` registrations. Three tasks (≤3 rule).

- **Wave 2 (2 parallel plans):**
  - **PLAN-2.1 — Sync handler fork + structural smoke tests.** Modify `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` (or whatever the PG equivalent is; researcher confirms exact path) to add the `HandleExternalTx` fork. Add 3 structural smoke tests (reflection + source-text grep). Two tasks per plan.
  - **PLAN-2.2 — Async handler fork + structural smoke tests.** Mirror sync for async handler. Two tasks.

Files are disjoint between PLAN-2.1 and PLAN-2.2 — parallel execution is safe.

## Hard Rules / Cross-Cutting Constraints

These come from PROJECT.md, CLAUDE.md, Phase 2/3 lessons, and Decision 5:

- **`IDbConnection` discipline (CLAUDE.md):** No `NpgsqlConnection` casts in handlers outside the `HandleExternalTx[Async]` fork. The fork's cast to `(NpgsqlConnection)tx.Connection` is the SAME boundary cast as Phase 3 SqlServer's. The producer subclass's `GuardNpgsqlTransaction` is the only public-API cast point.
- **No sealed-type casts in non-fork handler code:** `_jobExistsHandler.Handle(new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(...))` follows the existing PG pattern (analogous to Phase 3 SqlServer's typed query pattern). If the PG version uses different type parameters, mirror them.
- **Async handler mocking (CLAUDE.md):** Mock `DbConnection`/`DbCommand`/`DbDataReader` abstract bases, NOT `IDbConnection`/etc. interfaces. The PG async handler tests will need this if any execution-style tests are added; the Phase 4 structural smoke tests use reflection + source-text grep, so this rule doesn't directly apply.
- **MSTest 4.x assertions:** `Assert.ThrowsExactly<T>`, `Assert.ThrowsExactlyAsync<T>`, `Assert.AreEqual`, `StringAssert.Contains`. NEVER `Assert.ThrowsException<>`.
- **LGPL-2.1 license header** on every new `.cs` file.
- **Build cleanliness:** PG main + tests projects build clean on net10.0 + net8.0 with `TreatWarningsAsErrors`. XML doc on every new public type/member.
- **No regressions:** Existing PG tests must still pass. Phase 4 changes are additive plus 2-line early-branches in existing handlers.
- **RegisterConditional (Rule A above)** is mandatory — DO NOT use plain `Register` for the 3 open-generic producer mappings.
- **Lifecycle-comment rephrase (Rule B above)** is mandatory — use the exact comment text specified.

## Exit Criteria for Phase 4

1. **`Transport.PostgreSQL` builds clean** (net10.0 + net8.0) with `TreatWarningsAsErrors`.
2. **All new PG unit tests pass; existing tests still pass.**
3. **Capability cast works** for PostgreSQL: `container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` returns `true`. Same Wave-1-static + Phase-6-runtime split as Phase 3 (CONTEXT-3 §Exit Criteria #3 acceptance pattern).
4. **Mock-based lifecycle ownership** confirmed via source-text smoke test (sync + async fork): no `.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()` in fork bodies. Phase 6 covers the runtime side.
5. **Polly retry bypass** verified at the cross-transport level — Phase 2 PLAN-3.2 (PostgreSQL retry decorator bypass) is already in place and tested. Phase 4 confirms end-to-end via DI smoke test.
6. **Risk #2 closed by deferral:** sequential-loop batch send (Decision 1) mirrors Phase 3, no `NpgsqlBatch` spike performed. Wiki draft (Phase 7) will document the deferral.
7. **Risk #3 closed:** case-sensitive validator unit test green (Decision 2). At least one test asserts that a mismatched-case database name fails validation.
8. **No `NpgsqlConnection` casts in non-fork PostgreSQL handler code** (grep gate, excluding the fork itself).

## Out of Scope (Phase 4)

- `NpgsqlBatch` performance experiment (Risk #2 deferred per Decision 1).
- Integration tests against real PG (Phase 6).
- `docs/outbox-pattern.md` user-facing documentation (Phase 7).
- Memory/Redis/LiteDb/SQLite negative-path tests (Phase 5).
- CLAUDE.md "Lessons Learned" additions captured in Phase 3 (deferred to ship time — Phase 3 documenter's recommendation).

## Dependencies

- **Phase 2** (foundation) — built and shipped.
- **Phase 3** (SqlServer) — built and shipped. Phase 4 is structural mirror; SUMMARYs and REVIEWs serve as templates for cross-plan consistency.

Phase 4 does NOT depend on Phase 5 or 6.
